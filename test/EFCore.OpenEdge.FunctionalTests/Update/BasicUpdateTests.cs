using System;
using System.Linq;
using EFCore.OpenEdge.FunctionalTests.Shared;
using EFCore.OpenEdge.FunctionalTests.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Update
{
    public class BasicUpdateTests : ECommerceTestBase
    {
        private readonly ITestOutputHelper _output;

        public BasicUpdateTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region INSERT TESTS

        [Fact]
        public void CanInsert_Customer()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var customer = new Customer
                {
                    Id = 100,
                    Name = "Test Customer",
                    Email = "test@example.com",
                    Age = 30,
                    City = "Test City",
                    IsActive = true
                };

                context.Customers.Add(customer);
                var result = context.SaveChanges();

                result.Should().Be(1);
                Console.WriteLine($"Inserted customer with Id: {customer.Id}");

                // Verify the customer was inserted
                var insertedCustomer = context.Customers.Find(100);
                insertedCustomer.Should().NotBeNull();
                insertedCustomer.Name.Should().Be("Test Customer");

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        [Fact]
        public void CanInsert_Product()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var product = new Product
                {
                    Id = 100,
                    Name = "Test Product",
                    Price = 99.99m,
                    CategoryId = 1, // Electronics
                    Description = "Test product description",
                    InStock = true
                };

                context.Products.Add(product);
                var result = context.SaveChanges();

                result.Should().Be(1);
                Console.WriteLine($"Inserted product with Id: {product.Id}");

                // Verify the product was inserted
                var insertedProduct = context.Products.Find(100);
                insertedProduct.Should().NotBeNull();
                insertedProduct.Name.Should().Be("Test Product");
                insertedProduct.Price.Should().Be(99.99m);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        [Fact]
        public void CanInsert_Order_WithOrderItems()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var order = new Order
                {
                    Id = 100,
                    CustomerId = 1, // John Doe
                    OrderDate = DateTime.Now,
                    TotalAmount = 129.98m,
                    Status = "Processing"
                };

                var orderItem1 = new OrderItem
                {
                    Id = 100,
                    OrderId = 100,
                    ProductId = 2, // Mouse
                    Quantity = 2,
                    UnitPrice = 29.99m
                };

                var orderItem2 = new OrderItem
                {
                    Id = 101,
                    OrderId = 100,
                    ProductId = 10, // Webcam
                    Quantity = 1,
                    UnitPrice = 79.99m
                };

                context.Orders.Add(order);
                context.OrderItems.AddRange(orderItem1, orderItem2);

                var result = context.SaveChanges();

                result.Should().Be(3); // 1 order + 2 order items
                Console.WriteLine($"Inserted order with {result} total changes");

                // Verify the order and items were inserted
                var insertedOrder = context.Orders.Include(o => o.OrderItems).First(o => o.Id == 100);
                insertedOrder.Should().NotBeNull();
                insertedOrder.OrderItems.Should().HaveCount(2);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion

        #region UPDATE TESTS

        [Fact]
        public void CanUpdate_Customer()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                // First, find an existing customer
                var customer = context.Customers.First(c => c.Id == 1);
                var originalName = customer.Name;

                // Update the customer
                customer.Name = "Updated John Doe";
                customer.Age = 31;
                customer.City = "Updated City";

                var result = context.SaveChanges();

                result.Should().Be(1);
                Console.WriteLine($"Updated customer: {originalName} -> {customer.Name}");

                // Verify the update
                var updatedCustomer = context.Customers.Find(1);
                updatedCustomer.Name.Should().Be("Updated John Doe");
                updatedCustomer.Age.Should().Be(31);
                updatedCustomer.City.Should().Be("Updated City");

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // [Fact]
        // public void CanUpdate_Product_Price()
        // {
        //     using var context = CreateContext();
        //     using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

        //     try
        //     {
        //         // Find an existing product
        //         var product = context.Products.First(p => p.Id == 1);
        //         var originalPrice = product.Price;

        //         // Update the price
        //         product.Price = 1099.99m;
        //         product.Description = "Updated description";

        //         var result = context.SaveChanges();

        //         result.Should().Be(1);
        //         Console.WriteLine($"Updated product price: {originalPrice} -> {product.Price}");

        //         // Verify the update
        //         var updatedProduct = context.Products.Find(1);
        //         updatedProduct.Price.Should().Be(1099.99m);
        //         updatedProduct.Description.Should().Be("Updated description");

        //         transaction.Commit();
        //     }
        //     catch
        //     {
        //         transaction.Rollback();
        //         throw;
        //     }
        // }

        // [Fact]
        // public void CanUpdate_Order_Status()
        // {
        //     using var context = CreateContext();
        //     using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

        //     try
        //     {
        //         // Find an existing order
        //         var order = context.Orders.First(o => o.Id == 1);
        //         var originalStatus = order.Status;

        //         // Update the status
        //         order.Status = "Delivered";
        //         order.TotalAmount = 1199.98m;

        //         var result = context.SaveChanges();

        //         result.Should().Be(1);
        //         Console.WriteLine($"Updated order status: {originalStatus} -> {order.Status}");

        //         // Verify the update
        //         var updatedOrder = context.Orders.Find(1);
        //         updatedOrder.Status.Should().Be("Delivered");
        //         updatedOrder.TotalAmount.Should().Be(1199.98m);

        //         transaction.Commit();
        //     }
        //     catch
        //     {
        //         transaction.Rollback();
        //         throw;
        //     }
        // }

        #endregion

        #region DELETE TESTS

        // [Fact]
        // public void CanDelete_Customer()
        // {
        //     using var context = CreateContext();
        //     using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

        //     try
        //     {
        //         // First, add a customer that we can delete
        //         var customer = new Customer
        //         {
        //             Id = 200,
        //             Name = "Customer To Delete",
        //             Email = "delete@example.com",
        //             Age = 25,
        //             City = "Delete City",
        //             IsActive = true
        //         };

        //         context.Customers.Add(customer);
        //         context.SaveChanges();

        //         // Now delete the customer
        //         var customerToDelete = context.Customers.Find(200);
        //         context.Customers.Remove(customerToDelete);

        //         var result = context.SaveChanges();

        //         result.Should().Be(1);
        //         Console.WriteLine($"Deleted customer: {customer.Name}");

        //         // Verify the deletion
        //         var deletedCustomer = context.Customers.Find(200);
        //         deletedCustomer.Should().BeNull();

        //         transaction.Commit();
        //     }
        //     catch
        //     {
        //         transaction.Rollback();
        //         throw;
        //     }
        // }

        // [Fact]
        // public void CanDelete_Product()
        // {
        //     using var context = CreateContext();
        //     using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

        //     try
        //     {
        //         // First, add a product that we can delete
        //         var product = new Product
        //         {
        //             Id = 200,
        //             Name = "Product To Delete",
        //             Price = 50.00m,
        //             CategoryId = 1,
        //             Description = "Product for deletion test",
        //             InStock = true
        //         };

        //         context.Products.Add(product);
        //         context.SaveChanges();

        //         // Now delete the product
        //         var productToDelete = context.Products.Find(200);
        //         context.Products.Remove(productToDelete);

        //         var result = context.SaveChanges();

        //         result.Should().Be(1);
        //         Console.WriteLine($"Deleted product: {product.Name}");

        //         // Verify the deletion
        //         var deletedProduct = context.Products.Find(200);
        //         deletedProduct.Should().BeNull();

        //         transaction.Commit();
        //     }
        //     catch
        //     {
        //         transaction.Rollback();
        //         throw;
        //     }
        // }

        // [Fact]
        // public void CanDelete_OrderItem()
        // {
        //     using var context = CreateContext();
        //     using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

        //     try
        //     {
        //         // Find an existing order item
        //         var orderItem = context.OrderItems.First(oi => oi.Id == 1);
        //         var orderId = orderItem.OrderId;

        //         // Delete the order item
        //         context.OrderItems.Remove(orderItem);

        //         var result = context.SaveChanges();

        //         result.Should().Be(1);
        //         Console.WriteLine($"Deleted order item from order {orderId}");

        //         // Verify the deletion
        //         var deletedOrderItem = context.OrderItems.Find(1);
        //         deletedOrderItem.Should().BeNull();

        //         transaction.Commit();
        //     }
        //     catch
        //     {
        //         transaction.Rollback();
        //         throw;
        //     }
        // }

        #endregion

        #region COMPLEX UPDATE TESTS

        // [Fact]
        // public void CanUpdate_Multiple_Entities()
        // {
        //     using var context = CreateContext();
        //     using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

        //     try
        //     {
        //         // Update multiple customers
        //         var customers = context.Customers.Where(c => c.City == "New York").ToList();
        //         foreach (var customer in customers)
        //         {
        //             customer.City = "New York Updated";
        //         }

        //         var result = context.SaveChanges();

        //         result.Should().BeGreaterThan(0);
        //         Console.WriteLine($"Updated {result} customers in New York");

        //         // Verify the updates
        //         var updatedCustomers = context.Customers.Where(c => c.City == "New York Updated").ToList();
        //         updatedCustomers.Should().HaveCount(result);

        //         transaction.Commit();
        //     }
        //     catch
        //     {
        //         transaction.Rollback();
        //         throw;
        //     }
        // }

        // [Fact]
        // public void CanUpdate_With_Navigation_Properties()
        // {
        //     using var context = CreateContext();
        //     using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

        //     try
        //     {
        //         // Load an order with its items
        //         var order = context.Orders
        //             .Include(o => o.OrderItems)
        //             .First(o => o.Id == 1);

        //         // Update the order
        //         order.Status = "Updated Status";

        //         // Update one of the order items
        //         var firstItem = order.OrderItems.First();
        //         firstItem.Quantity = 5;
        //         firstItem.UnitPrice = 199.99m;

        //         var result = context.SaveChanges();

        //         result.Should().Be(2); // 1 order + 1 order item
        //         Console.WriteLine($"Updated order and order item, total changes: {result}");

        //         // Verify the updates
        //         var updatedOrder = context.Orders
        //             .Include(o => o.OrderItems)
        //             .First(o => o.Id == 1);

        //         updatedOrder.Status.Should().Be("Updated Status");
        //         updatedOrder.OrderItems.First().Quantity.Should().Be(5);
        //         updatedOrder.OrderItems.First().UnitPrice.Should().Be(199.99m);

        //         transaction.Commit();
        //     }
        //     catch
        //     {
        //         transaction.Rollback();
        //         throw;
        //     }
        // }

        #endregion

        #region ERROR HANDLING TESTS

        // [Fact]
        // public void Should_Fail_Insert_Duplicate_Primary_Key()
        // {
        //     using var context = CreateContext();
        //     using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

        //     try
        //     {
        //         var customer = new Customer
        //         {
        //             Id = 1, // This ID already exists
        //             Name = "Duplicate Customer",
        //             Email = "duplicate@example.com",
        //             Age = 30,
        //             City = "Test City",
        //             IsActive = true
        //         };

        //         context.Customers.Add(customer);

        //         // This should throw an exception due to duplicate primary key
        //         var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        //         Console.WriteLine($"Expected exception caught: {exception.Message}");

        //         transaction.Commit();
        //     }
        //     catch
        //     {
        //         transaction.Rollback();
        //         throw;
        //     }
        // }

        // [Fact]
        // public void Should_Fail_Update_NonExistent_Entity()
        // {
        //     using var context = CreateContext();
        //     using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

        //     try
        //     {
        //         // Try to update a customer that doesn't exist
        //         var customer = new Customer
        //         {
        //             Id = 999, // Non-existent ID
        //             Name = "Non-existent Customer",
        //             Email = "nonexistent@example.com",
        //             Age = 30,
        //             City = "Test City",
        //             IsActive = true
        //         };

        //         context.Customers.Update(customer);

        //         // This should result in 0 changes
        //         var result = context.SaveChanges();
        //         result.Should().Be(1); // EF Core will insert if it doesn't exist when using Update
        //         Console.WriteLine($"Update resulted in {result} changes");

        //         transaction.Commit();
        //     }
        //     catch
        //     {
        //         transaction.Rollback();
        //         throw;
        //     }
        // }

        #endregion
    }
}