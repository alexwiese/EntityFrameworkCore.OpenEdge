// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using EFCore.OpenEdge.FunctionalTests.Shared;
// using EFCore.OpenEdge.FunctionalTests.Shared.Models;
// using FluentAssertions;
// using Microsoft.EntityFrameworkCore;
// using Xunit;
// using Xunit.Abstractions;

// namespace EFCore.OpenEdge.FunctionalTests.Update
// {
//     public class TransactionTests : ECommerceTestBase
//     {
//         private readonly ITestOutputHelper _output;

//         public TransactionTests(ITestOutputHelper output)
//         {
//             _output = output;
//         }

//         #region BASIC TRANSACTION TESTS

//         [Fact]
//         public void CanCommit_Simple_Transaction()
//         {
//             using var context = CreateContext();
//             using var transaction = context.Database.BeginTransaction();

//             var customer = new Customer
//             {
//                 Id = 1200,
//                 Name = "Transaction Customer",
//                 Email = "transaction@example.com",
//                 Age = 30,
//                 City = "Transaction City",
//                 IsActive = true
//             };

//             context.Customers.Add(customer);
//             context.SaveChanges();

//             // Verify customer exists within the transaction
//             var customerInTransaction = context.Customers.Find(1200);
//             customerInTransaction.Should().NotBeNull();

//             transaction.Commit();
//             _output.WriteLine("Successfully committed transaction");

//             // Verify customer exists after commit
//             using var verifyContext = CreateContext();
//             var committedCustomer = verifyContext.Customers.Find(1200);
//             committedCustomer.Should().NotBeNull();
//             committedCustomer.Name.Should().Be("Transaction Customer");
//         }

//         [Fact]
//         public void CanRollback_Simple_Transaction()
//         {
//             using var context = CreateContext();
//             using var transaction = context.Database.BeginTransaction();

//             var customer = new Customer
//             {
//                 Id = 1201,
//                 Name = "Rollback Customer",
//                 Email = "rollback@example.com",
//                 Age = 25,
//                 City = "Rollback City",
//                 IsActive = true
//             };

//             context.Customers.Add(customer);
//             context.SaveChanges();

//             // Verify customer exists within the transaction
//             var customerInTransaction = context.Customers.Find(1201);
//             customerInTransaction.Should().NotBeNull();

//             transaction.Rollback();
//             _output.WriteLine("Successfully rolled back transaction");

//             // Verify customer does not exist after rollback
//             using var verifyContext = CreateContext();
//             var rolledBackCustomer = verifyContext.Customers.Find(1201);
//             rolledBackCustomer.Should().BeNull();
//         }

//         [Fact]
//         public void CanCommit_Multiple_Operations_In_Transaction()
//         {
//             using var context = CreateContext();
//             using var transaction = context.Database.BeginTransaction();

//             // Insert a customer
//             var customer = new Customer
//             {
//                 Id = 1202,
//                 Name = "Multi-Op Customer",
//                 Email = "multiop@example.com",
//                 Age = 35,
//                 City = "Multi-Op City",
//                 IsActive = true
//             };

//             context.Customers.Add(customer);

//             // Insert a product
//             var product = new Product
//             {
//                 Id = 1200,
//                 Name = "Multi-Op Product",
//                 Price = 150.00m,
//                 CategoryId = 1,
//                 Description = "Multi-operation product",
//                 InStock = true
//             };

//             context.Products.Add(product);

//             // Update an existing customer
//             var existingCustomer = context.Customers.Find(1);
//             existingCustomer.Age = 45;

//             context.SaveChanges();
//             transaction.Commit();

//             _output.WriteLine("Successfully committed transaction with multiple operations");

//             // Verify all operations were committed
//             using var verifyContext = CreateContext();
//             var insertedCustomer = verifyContext.Customers.Find(1202);
//             var insertedProduct = verifyContext.Products.Find(1200);
//             var updatedCustomer = verifyContext.Customers.Find(1);

//             insertedCustomer.Should().NotBeNull();
//             insertedProduct.Should().NotBeNull();
//             updatedCustomer.Age.Should().Be(45);
//         }

//         #endregion

//         #region ROLLBACK SCENARIOS

//         [Fact]
//         public void CanRollback_Multiple_Operations_In_Transaction()
//         {
//             using var context = CreateContext();
//             using var transaction = context.Database.BeginTransaction();

//             // Get original state
//             var originalCustomer = context.Customers.Find(1);
//             var originalAge = originalCustomer.Age;

//             // Insert a customer
//             var customer = new Customer
//             {
//                 Id = 1203,
//                 Name = "Rollback Multi-Op Customer",
//                 Email = "rollbackmultiop@example.com",
//                 Age = 40,
//                 City = "Rollback Multi-Op City",
//                 IsActive = true
//             };

//             context.Customers.Add(customer);

//             // Update an existing customer
//             originalCustomer.Age = 99;

//             context.SaveChanges();
//             transaction.Rollback();

//             _output.WriteLine("Successfully rolled back transaction with multiple operations");

//             // Verify all operations were rolled back
//             using var verifyContext = CreateContext();
//             var notInsertedCustomer = verifyContext.Customers.Find(1203);
//             var notUpdatedCustomer = verifyContext.Customers.Find(1);

//             notInsertedCustomer.Should().BeNull();
//             notUpdatedCustomer.Age.Should().Be(originalAge);
//         }

//         [Fact]
//         public void Should_Rollback_On_Exception()
//         {
//             using var context = CreateContext();
//             using var transaction = context.Database.BeginTransaction();

//             try
//             {
//                 // Insert a valid customer
//                 var customer = new Customer
//                 {
//                     Id = 1204,
//                     Name = "Exception Customer",
//                     Email = "exception@example.com",
//                     Age = 30,
//                     City = "Exception City",
//                     IsActive = true
//                 };

//                 context.Customers.Add(customer);

//                 // Try to insert a customer with duplicate ID (should cause exception)
//                 var duplicateCustomer = new Customer
//                 {
//                     Id = 1, // This ID already exists
//                     Name = "Duplicate Customer",
//                     Email = "duplicate@example.com",
//                     Age = 25,
//                     City = "Duplicate City",
//                     IsActive = true
//                 };

//                 context.Customers.Add(duplicateCustomer);
//                 context.SaveChanges();

//                 // This should not be reached
//                 Assert.True(false, "Expected exception was not thrown");
//             }
//             catch (DbUpdateException ex)
//             {
//                 _output.WriteLine($"Expected exception caught: {ex.Message}");
//                 transaction.Rollback();
//             }

//             // Verify that the valid customer was not inserted due to rollback
//             using var verifyContext = CreateContext();
//             var notInsertedCustomer = verifyContext.Customers.Find(1204);
//             notInsertedCustomer.Should().BeNull();
//         }

//         #endregion

//         #region NESTED TRANSACTION TESTS

//         [Fact]
//         public void CanHandle_Nested_SaveChanges_In_Transaction()
//         {
//             using var context = CreateContext();
//             using var transaction = context.Database.BeginTransaction();

//             // First SaveChanges
//             var customer1 = new Customer
//             {
//                 Id = 1205,
//                 Name = "Nested Customer 1",
//                 Email = "nested1@example.com",
//                 Age = 30,
//                 City = "Nested City",
//                 IsActive = true
//             };

//             context.Customers.Add(customer1);
//             var result1 = context.SaveChanges();

//             // Second SaveChanges in the same transaction
//             var customer2 = new Customer
//             {
//                 Id = 1206,
//                 Name = "Nested Customer 2",
//                 Email = "nested2@example.com",
//                 Age = 35,
//                 City = "Nested City",
//                 IsActive = true
//             };

//             context.Customers.Add(customer2);
//             var result2 = context.SaveChanges();

//             transaction.Commit();

//             result1.Should().Be(1);
//             result2.Should().Be(1);
//             _output.WriteLine("Successfully handled nested SaveChanges in transaction");

//             // Verify both customers were committed
//             using var verifyContext = CreateContext();
//             var committedCustomer1 = verifyContext.Customers.Find(1205);
//             var committedCustomer2 = verifyContext.Customers.Find(1206);

//             committedCustomer1.Should().NotBeNull();
//             committedCustomer2.Should().NotBeNull();
//         }

//         #endregion

//         #region ASYNC TRANSACTION TESTS

//         [Fact]
//         public async Task CanCommit_Async_Transaction()
//         {
//             using var context = CreateContext();
//             using var transaction = await context.Database.BeginTransactionAsync();

//             var customer = new Customer
//             {
//                 Id = 1207,
//                 Name = "Async Transaction Customer",
//                 Email = "asynctransaction@example.com",
//                 Age = 28,
//                 City = "Async Transaction City",
//                 IsActive = true
//             };

//             context.Customers.Add(customer);
//             await context.SaveChangesAsync();

//             await transaction.CommitAsync();
//             _output.WriteLine("Successfully committed async transaction");

//             // Verify customer exists after commit
//             using var verifyContext = CreateContext();
//             var committedCustomer = verifyContext.Customers.Find(1207);
//             committedCustomer.Should().NotBeNull();
//         }

//         [Fact]
//         public async Task CanRollback_Async_Transaction()
//         {
//             using var context = CreateContext();
//             using var transaction = await context.Database.BeginTransactionAsync();

//             var customer = new Customer
//             {
//                 Id = 1208,
//                 Name = "Async Rollback Customer",
//                 Email = "asyncrollback@example.com",
//                 Age = 32,
//                 City = "Async Rollback City",
//                 IsActive = true
//             };

//             context.Customers.Add(customer);
//             await context.SaveChangesAsync();

//             await transaction.RollbackAsync();
//             _output.WriteLine("Successfully rolled back async transaction");

//             // Verify customer does not exist after rollback
//             using var verifyContext = CreateContext();
//             var rolledBackCustomer = verifyContext.Customers.Find(1208);
//             rolledBackCustomer.Should().BeNull();
//         }

//         #endregion

//         #region TRANSACTION ISOLATION TESTS

//         [Fact]
//         public void CanHandle_Concurrent_Transactions()
//         {
//             using var context1 = CreateContext();
//             using var context2 = CreateContext();

//             using var transaction1 = context1.Database.BeginTransaction();
//             using var transaction2 = context2.Database.BeginTransaction();

//             // Transaction 1: Insert customer
//             var customer1 = new Customer
//             {
//                 Id = 1209,
//                 Name = "Concurrent Transaction Customer 1",
//                 Email = "concurrent1@example.com",
//                 Age = 30,
//                 City = "Concurrent City 1",
//                 IsActive = true
//             };

//             context1.Customers.Add(customer1);
//             context1.SaveChanges();

//             // Transaction 2: Insert different customer
//             var customer2 = new Customer
//             {
//                 Id = 1210,
//                 Name = "Concurrent Transaction Customer 2",
//                 Email = "concurrent2@example.com",
//                 Age = 35,
//                 City = "Concurrent City 2",
//                 IsActive = true
//             };

//             context2.Customers.Add(customer2);
//             context2.SaveChanges();

//             // Commit both transactions
//             transaction1.Commit();
//             transaction2.Commit();

//             _output.WriteLine("Successfully handled concurrent transactions");

//             // Verify both customers were committed
//             using var verifyContext = CreateContext();
//             var committedCustomer1 = verifyContext.Customers.Find(1209);
//             var committedCustomer2 = verifyContext.Customers.Find(1210);

//             committedCustomer1.Should().NotBeNull();
//             committedCustomer2.Should().NotBeNull();
//         }

//         [Fact]
//         public void CanHandle_Transaction_Isolation()
//         {
//             using var context1 = CreateContext();
//             using var context2 = CreateContext();

//             using var transaction1 = context1.Database.BeginTransaction();

//             // Transaction 1: Insert customer but don't commit yet
//             var customer = new Customer
//             {
//                 Id = 1211,
//                 Name = "Isolation Customer",
//                 Email = "isolation@example.com",
//                 Age = 30,
//                 City = "Isolation City",
//                 IsActive = true
//             };

//             context1.Customers.Add(customer);
//             context1.SaveChanges();

//             // Context 2 (no transaction): Should not see the uncommitted customer
//             var invisibleCustomer = context2.Customers.Find(1211);
//             invisibleCustomer.Should().BeNull();

//             // Now commit the transaction
//             transaction1.Commit();

//             // Context 2 should now see the customer after commit
//             var visibleCustomer = context2.Customers.Find(1211);
//             visibleCustomer.Should().NotBeNull();

//             _output.WriteLine("Successfully demonstrated transaction isolation");
//         }

//         #endregion

//         #region COMPLEX TRANSACTION SCENARIOS

//         [Fact]
//         public void CanHandle_Complex_Business_Transaction()
//         {
//             using var context = CreateContext();
//             using var transaction = context.Database.BeginTransaction();

//             try
//             {
//                 // Create a new customer
//                 var customer = new Customer
//                 {
//                     Id = 1212,
//                     Name = "Complex Transaction Customer",
//                     Email = "complex@example.com",
//                     Age = 30,
//                     City = "Complex City",
//                     IsActive = true
//                 };

//                 context.Customers.Add(customer);
//                 context.SaveChanges();

//                 // Create an order for the customer
//                 var order = new Order
//                 {
//                     Id = 1200,
//                     CustomerId = 1212,
//                     OrderDate = DateTime.Now,
//                     TotalAmount = 299.97m,
//                     Status = "Processing"
//                 };

//                 context.Orders.Add(order);
//                 context.SaveChanges();

//                 // Create order items
//                 var orderItems = new[]
//                 {
//                     new OrderItem { Id = 1200, OrderId = 1200, ProductId = 1, Quantity = 1, UnitPrice = 99.99m },
//                     new OrderItem { Id = 1201, OrderId = 1200, ProductId = 2, Quantity = 2, UnitPrice = 49.99m },
//                     new OrderItem { Id = 1202, OrderId = 1200, ProductId = 3, Quantity = 1, UnitPrice = 149.99m }
//                 };

//                 context.OrderItems.AddRange(orderItems);
//                 context.SaveChanges();

//                 // Update product stock
//                 var products = context.Products.Where(p => p.Id == 1 || p.Id == 2 || p.Id == 3).ToList();
//                 foreach (var product in products)
//                 {
//                     product.InStock = false; // Mark as out of stock
//                 }
//                 context.SaveChanges();

//                 transaction.Commit();
//                 _output.WriteLine("Successfully completed complex business transaction");

//                 // Verify all changes were committed
//                 using var verifyContext = CreateContext();
//                 var verifyCustomer = verifyContext.Customers.Find(1212);
//                 var verifyOrder = verifyContext.Orders.Include(o => o.OrderItems).First(o => o.Id == 1200);
//                 var verifyProducts = verifyContext.Products.Where(p => p.Id == 1 || p.Id == 2 || p.Id == 3).ToList();

//                 verifyCustomer.Should().NotBeNull();
//                 verifyOrder.Should().NotBeNull();
//                 verifyOrder.OrderItems.Should().HaveCount(3);
//                 verifyProducts.Should().OnlyContain(p => p.InStock == false);
//             }
//             catch (Exception ex)
//             {
//                 transaction.Rollback();
//                 _output.WriteLine($"Transaction rolled back due to: {ex.Message}");
//                 throw;
//             }
//         }

//         #endregion

//         #region TRANSACTION PERFORMANCE TESTS

//         [Fact]
//         public void CanHandle_Large_Transaction()
//         {
//             using var context = CreateContext();
//             using var transaction = context.Database.BeginTransaction();

//             var startTime = DateTime.Now;

//             // Insert 50 customers in a single transaction
//             var customers = new List<Customer>();
//             for (int i = 0; i < 50; i++)
//             {
//                 customers.Add(new Customer
//                 {
//                     Id = 1300 + i,
//                     Name = $"Large Transaction Customer {i}",
//                     Email = $"large{i}@example.com",
//                     Age = 25 + (i % 30),
//                     City = $"Large City {i % 10}",
//                     IsActive = i % 2 == 0
//                 });
//             }

//             context.Customers.AddRange(customers);
//             context.SaveChanges();

//             transaction.Commit();

//             var endTime = DateTime.Now;
//             var duration = endTime - startTime;

//             _output.WriteLine($"Successfully completed large transaction with 50 customers in {duration.TotalMilliseconds}ms");

//             // Verify all customers were committed
//             using var verifyContext = CreateContext();
//             var insertedCustomers = verifyContext.Customers.Where(c => c.Id >= 1300 && c.Id < 1350).ToList();
//             insertedCustomers.Should().HaveCount(50);
//         }

//         #endregion
//     }
// }