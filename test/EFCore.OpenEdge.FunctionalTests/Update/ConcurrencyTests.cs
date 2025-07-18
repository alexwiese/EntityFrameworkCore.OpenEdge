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
//     public class ConcurrencyTests : ECommerceTestBase
//     {
//         private readonly ITestOutputHelper _output;

//         public ConcurrencyTests(ITestOutputHelper output)
//         {
//             _output = output;
//         }

//         #region BASIC CONCURRENCY TESTS

//         [Fact]
//         public void CanHandle_Concurrent_Updates_To_Different_Entities()
//         {
//             // Test that updates to different entities don't interfere with each other
//             using var context1 = CreateContext();
//             using var context2 = CreateContext();

//             // Context 1 updates customer 1
//             var customer1 = context1.Customers.Find(1);
//             customer1.Age = 31;

//             // Context 2 updates customer 2
//             var customer2 = context2.Customers.Find(2);
//             customer2.Age = 26;

//             // Both should save successfully
//             var result1 = context1.SaveChanges();
//             var result2 = context2.SaveChanges();

//             result1.Should().Be(1);
//             result2.Should().Be(1);

//             _output.WriteLine("Successfully handled concurrent updates to different entities");

//             // Verify both updates were applied
//             using var verifyContext = CreateContext();
//             var verifyCustomer1 = verifyContext.Customers.Find(1);
//             var verifyCustomer2 = verifyContext.Customers.Find(2);

//             verifyCustomer1.Age.Should().Be(31);
//             verifyCustomer2.Age.Should().Be(26);
//         }

//         [Fact]
//         public void CanHandle_Concurrent_Inserts()
//         {
//             // Test that concurrent inserts work correctly
//             using var context1 = CreateContext();
//             using var context2 = CreateContext();

//             var customer1 = new Customer
//             {
//                 Id = 800,
//                 Name = "Concurrent Customer 1",
//                 Email = "concurrent1@example.com",
//                 Age = 25,
//                 City = "Concurrent City 1",
//                 IsActive = true
//             };

//             var customer2 = new Customer
//             {
//                 Id = 801,
//                 Name = "Concurrent Customer 2",
//                 Email = "concurrent2@example.com",
//                 Age = 30,
//                 City = "Concurrent City 2",
//                 IsActive = true
//             };

//             context1.Customers.Add(customer1);
//             context2.Customers.Add(customer2);

//             // Both should save successfully
//             var result1 = context1.SaveChanges();
//             var result2 = context2.SaveChanges();

//             result1.Should().Be(1);
//             result2.Should().Be(1);

//             _output.WriteLine("Successfully handled concurrent inserts");

//             // Verify both customers were inserted
//             using var verifyContext = CreateContext();
//             var insertedCustomers = verifyContext.Customers.Where(c => c.Id == 800 || c.Id == 801).ToList();
//             insertedCustomers.Should().HaveCount(2);
//         }

//         [Fact]
//         public void Should_Handle_Same_Entity_Updates_Sequentially()
//         {
//             // Test updating the same entity from different contexts
//             // Note: OpenEdge doesn't support optimistic concurrency by default
//             using var context1 = CreateContext();
//             using var context2 = CreateContext();

//             // Both contexts load the same customer
//             var customer1 = context1.Customers.Find(1);
//             var customer2 = context2.Customers.Find(1);

//             // Context 1 updates and saves first
//             customer1.Age = 35;
//             var result1 = context1.SaveChanges();

//             // Context 2 updates and saves second
//             customer2.City = "Updated City";
//             var result2 = context2.SaveChanges();

//             result1.Should().Be(1);
//             result2.Should().Be(1);

//             _output.WriteLine("Handled sequential updates to same entity");

//             // Verify the final state - last update wins
//             using var verifyContext = CreateContext();
//             var finalCustomer = verifyContext.Customers.Find(1);
//             finalCustomer.Age.Should().Be(35); // From context1
//             finalCustomer.City.Should().Be("Updated City"); // From context2
//         }

//         #endregion

//         #region TRANSACTION ISOLATION TESTS

//         [Fact]
//         public void CanHandle_Read_While_Update_In_Progress()
//         {
//             // Test reading data while an update is in progress
//             using var updateContext = CreateContext();
//             using var readContext = CreateContext();

//             // Start an update but don't commit yet
//             var customer = updateContext.Customers.Find(1);
//             customer.Age = 40;

//             // Read from another context before the update is committed
//             var readCustomer = readContext.Customers.Find(1);
//             var originalAge = readCustomer.Age;

//             // Now commit the update
//             updateContext.SaveChanges();

//             // Read again after commit
//             var updatedCustomer = readContext.Customers.Find(1);

//             _output.WriteLine($"Age before update: {originalAge}, Age after update: {updatedCustomer.Age}");

//             // The read context should see the updated value after refresh
//             updatedCustomer.Age.Should().Be(40);
//         }

//         [Fact]
//         public void CanHandle_Multiple_Readers()
//         {
//             // Test that multiple readers can access the same data simultaneously
//             using var context1 = CreateContext();
//             using var context2 = CreateContext();
//             using var context3 = CreateContext();

//             // All contexts read the same customer
//             var customer1 = context1.Customers.Find(1);
//             var customer2 = context2.Customers.Find(1);
//             var customer3 = context3.Customers.Find(1);

//             // All should have the same data
//             customer1.Name.Should().Be(customer2.Name);
//             customer2.Name.Should().Be(customer3.Name);
//             customer1.Age.Should().Be(customer2.Age);
//             customer2.Age.Should().Be(customer3.Age);

//             _output.WriteLine("Successfully handled multiple concurrent readers");
//         }

//         #endregion

//         #region ASYNC CONCURRENCY TESTS

//         [Fact]
//         public async Task CanHandle_Async_Concurrent_Operations()
//         {
//             // Test async operations running concurrently
//             var task1 = InsertCustomerAsync(900, "Async Customer 1");
//             var task2 = InsertCustomerAsync(901, "Async Customer 2");
//             var task3 = InsertProductAsync(900, "Async Product 1");

//             // Wait for all tasks to complete
//             var results = await Task.WhenAll(task1, task2, task3);

//             results.Should().AllSatisfy(result => result.Should().Be(1));

//             _output.WriteLine("Successfully handled async concurrent operations");

//             // Verify all entities were inserted
//             using var verifyContext = CreateContext();
//             var customers = verifyContext.Customers.Where(c => c.Id >= 900 && c.Id <= 901).ToList();
//             var products = verifyContext.Products.Where(p => p.Id == 900).ToList();

//             customers.Should().HaveCount(2);
//             products.Should().HaveCount(1);
//         }

//         private async Task<int> InsertCustomerAsync(int id, string name)
//         {
//             using var context = CreateContext();
//             var customer = new Customer
//             {
//                 Id = id,
//                 Name = name,
//                 Email = $"async{id}@example.com",
//                 Age = 25,
//                 City = "Async City",
//                 IsActive = true
//             };

//             context.Customers.Add(customer);
//             return await context.SaveChangesAsync();
//         }

//         private async Task<int> InsertProductAsync(int id, string name)
//         {
//             using var context = CreateContext();
//             var product = new Product
//             {
//                 Id = id,
//                 Name = name,
//                 Price = 100.00m,
//                 CategoryId = 1,
//                 Description = "Async product",
//                 InStock = true
//             };

//             context.Products.Add(product);
//             return await context.SaveChangesAsync();
//         }

//         #endregion

//         #region DEADLOCK PREVENTION TESTS

//         [Fact]
//         public void CanHandle_Potential_Deadlock_Scenario()
//         {
//             // Test a scenario that might cause deadlocks in other databases
//             using var context1 = CreateContext();
//             using var context2 = CreateContext();

//             // Context 1: Update customer 1, then customer 2
//             var customer1_ctx1 = context1.Customers.Find(1);
//             customer1_ctx1.Age = 50;
//             context1.SaveChanges();

//             var customer2_ctx1 = context1.Customers.Find(2);
//             customer2_ctx1.Age = 51;

//             // Context 2: Update customer 2, then customer 1
//             var customer2_ctx2 = context2.Customers.Find(2);
//             customer2_ctx2.City = "Deadlock City 2";
//             context2.SaveChanges();

//             var customer1_ctx2 = context2.Customers.Find(1);
//             customer1_ctx2.City = "Deadlock City 1";

//             // Both contexts save their remaining changes
//             var result1 = context1.SaveChanges();
//             var result2 = context2.SaveChanges();

//             result1.Should().Be(1);
//             result2.Should().Be(1);

//             _output.WriteLine("Successfully handled potential deadlock scenario");

//             // Verify final state
//             using var verifyContext = CreateContext();
//             var finalCustomer1 = verifyContext.Customers.Find(1);
//             var finalCustomer2 = verifyContext.Customers.Find(2);

//             finalCustomer1.Age.Should().Be(50);
//             finalCustomer1.City.Should().Be("Deadlock City 1");
//             finalCustomer2.Age.Should().Be(51);
//             finalCustomer2.City.Should().Be("Deadlock City 2");
//         }

//         #endregion

//         #region PERFORMANCE UNDER CONCURRENCY

//         [Fact]
//         public async Task CanHandle_High_Concurrency_Load()
//         {
//             // Test performance under high concurrency
//             var tasks = new Task[10];
//             var startTime = DateTime.Now;

//             for (int i = 0; i < 10; i++)
//             {
//                 int taskId = i;
//                 tasks[i] = Task.Run(async () =>
//                 {
//                     using var context = CreateContext();
//                     var customer = new Customer
//                     {
//                         Id = 1000 + taskId,
//                         Name = $"Concurrent Customer {taskId}",
//                         Email = $"concurrent{taskId}@example.com",
//                         Age = 25 + taskId,
//                         City = $"Concurrent City {taskId}",
//                         IsActive = true
//                     };

//                     context.Customers.Add(customer);
//                     await context.SaveChangesAsync();
//                 });
//             }

//             await Task.WhenAll(tasks);
//             var endTime = DateTime.Now;
//             var duration = endTime - startTime;

//             _output.WriteLine($"Completed 10 concurrent operations in {duration.TotalMilliseconds}ms");

//             // Verify all customers were inserted
//             using var verifyContext = CreateContext();
//             var insertedCustomers = verifyContext.Customers.Where(c => c.Id >= 1000 && c.Id < 1010).ToList();
//             insertedCustomers.Should().HaveCount(10);
//         }

//         #endregion

//         #region ERROR HANDLING UNDER CONCURRENCY

//         [Fact]
//         public void Should_Handle_Constraint_Violations_Under_Concurrency()
//         {
//             // Test handling of constraint violations when multiple contexts try to insert duplicate keys
//             using var context1 = CreateContext();
//             using var context2 = CreateContext();

//             var customer1 = new Customer
//             {
//                 Id = 1100,
//                 Name = "Constraint Customer 1",
//                 Email = "constraint1@example.com",
//                 Age = 25,
//                 City = "Constraint City",
//                 IsActive = true
//             };

//             var customer2 = new Customer
//             {
//                 Id = 1100, // Same ID - this should cause a constraint violation
//                 Name = "Constraint Customer 2",
//                 Email = "constraint2@example.com",
//                 Age = 30,
//                 City = "Constraint City",
//                 IsActive = true
//             };

//             context1.Customers.Add(customer1);
//             context2.Customers.Add(customer2);

//             // First context should succeed
//             var result1 = context1.SaveChanges();
//             result1.Should().Be(1);

//             // Second context should fail with constraint violation
//             var exception = Assert.Throws<DbUpdateException>(() => context2.SaveChanges());
//             _output.WriteLine($"Expected constraint violation: {exception.Message}");

//             // Verify only one customer was inserted
//             using var verifyContext = CreateContext();
//             var insertedCustomers = verifyContext.Customers.Where(c => c.Id == 1100).ToList();
//             insertedCustomers.Should().HaveCount(1);
//             insertedCustomers.First().Name.Should().Be("Constraint Customer 1");
//         }

//         #endregion
//     }
// }