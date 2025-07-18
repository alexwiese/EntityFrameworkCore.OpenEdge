using System;
using System.Collections.Generic;
using System.Linq;
using EFCore.OpenEdge.FunctionalTests.Shared;
using EFCore.OpenEdge.FunctionalTests.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Update
{
    public class BulkUpdateTests : ECommerceTestBase
    {
        private readonly ITestOutputHelper _output;

        public BulkUpdateTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region BULK INSERT TESTS

        [Fact]
        public void CanInsert_Multiple_Customers()
        {
            using var context = CreateContext();

            var customers = new List<Customer>
            {
                new Customer { Id = 300, Name = "Bulk Customer 1", Email = "bulk1@example.com", Age = 25, City = "Bulk City 1", IsActive = true },
                new Customer { Id = 301, Name = "Bulk Customer 2", Email = "bulk2@example.com", Age = 30, City = "Bulk City 2", IsActive = true },
                new Customer { Id = 302, Name = "Bulk Customer 3", Email = "bulk3@example.com", Age = 35, City = "Bulk City 3", IsActive = false },
                new Customer { Id = 303, Name = "Bulk Customer 4", Email = "bulk4@example.com", Age = 40, City = "Bulk City 4", IsActive = true },
                new Customer { Id = 304, Name = "Bulk Customer 5", Email = "bulk5@example.com", Age = 45, City = "Bulk City 5", IsActive = false }
            };

            context.Customers.AddRange(customers);
            var result = context.SaveChanges();

            result.Should().Be(5);
            _output.WriteLine($"Bulk inserted {result} customers");

            // Verify all customers were inserted
            var insertedCustomers = context.Customers.Where(c => c.Id >= 300 && c.Id <= 304).ToList();
            insertedCustomers.Should().HaveCount(5);
        }

        [Fact]
        public void CanInsert_Multiple_Products()
        {
            using var context = CreateContext();

            var products = new List<Product>
            {
                new Product { Id = 300, Name = "Bulk Product 1", Price = 10.00m, CategoryId = 1, Description = "Bulk product 1", InStock = true },
                new Product { Id = 301, Name = "Bulk Product 2", Price = 20.00m, CategoryId = 2, Description = "Bulk product 2", InStock = true },
                new Product { Id = 302, Name = "Bulk Product 3", Price = 30.00m, CategoryId = 3, Description = "Bulk product 3", InStock = false },
                new Product { Id = 303, Name = "Bulk Product 4", Price = 40.00m, CategoryId = 1, Description = "Bulk product 4", InStock = true },
                new Product { Id = 304, Name = "Bulk Product 5", Price = 50.00m, CategoryId = 2, Description = "Bulk product 5", InStock = false }
            };

            context.Products.AddRange(products);
            var result = context.SaveChanges();

            result.Should().Be(5);
            _output.WriteLine($"Bulk inserted {result} products");

            // Verify all products were inserted
            var insertedProducts = context.Products.Where(p => p.Id >= 300 && p.Id <= 304).ToList();
            insertedProducts.Should().HaveCount(5);
        }

        [Fact]
        public void CanInsert_Order_With_Multiple_OrderItems()
        {
            using var context = CreateContext();

            var order = new Order
            {
                Id = 300,
                CustomerId = 1,
                OrderDate = DateTime.Now,
                TotalAmount = 450.00m,
                Status = "Processing"
            };

            var orderItems = new List<OrderItem>
            {
                new OrderItem { Id = 300, OrderId = 300, ProductId = 1, Quantity = 1, UnitPrice = 100.00m },
                new OrderItem { Id = 301, OrderId = 300, ProductId = 2, Quantity = 2, UnitPrice = 50.00m },
                new OrderItem { Id = 302, OrderId = 300, ProductId = 3, Quantity = 3, UnitPrice = 75.00m },
                new OrderItem { Id = 303, OrderId = 300, ProductId = 4, Quantity = 1, UnitPrice = 25.00m }
            };

            context.Orders.Add(order);
            context.OrderItems.AddRange(orderItems);

            var result = context.SaveChanges();

            result.Should().Be(5); // 1 order + 4 order items
            _output.WriteLine($"Bulk inserted order with {orderItems.Count} items, total changes: {result}");

            // Verify the order and items were inserted
            var insertedOrder = context.Orders.Include(o => o.OrderItems).First(o => o.Id == 300);
            insertedOrder.OrderItems.Should().HaveCount(4);
        }

        #endregion

        #region BULK UPDATE TESTS

        [Fact]
        public void CanUpdate_Multiple_Customers_Status()
        {
            using var context = CreateContext();

            // First, insert some customers to update
            var customers = new List<Customer>
            {
                new Customer { Id = 400, Name = "Update Customer 1", Email = "update1@example.com", Age = 25, City = "Update City", IsActive = true },
                new Customer { Id = 401, Name = "Update Customer 2", Email = "update2@example.com", Age = 30, City = "Update City", IsActive = true },
                new Customer { Id = 402, Name = "Update Customer 3", Email = "update3@example.com", Age = 35, City = "Update City", IsActive = true }
            };

            context.Customers.AddRange(customers);
            context.SaveChanges();

            // Now update all customers in "Update City" to inactive
            var customersToUpdate = context.Customers.Where(c => c.City == "Update City").ToList();
            foreach (var customer in customersToUpdate)
            {
                customer.IsActive = false;
            }

            var result = context.SaveChanges();

            result.Should().Be(3);
            _output.WriteLine($"Bulk updated {result} customers to inactive");

            // Verify all customers were updated
            var updatedCustomers = context.Customers.Where(c => c.City == "Update City").ToList();
            updatedCustomers.Should().OnlyContain(c => c.IsActive == false);
        }

        [Fact]
        public void CanUpdate_Multiple_Products_Price()
        {
            using var context = CreateContext();

            // First, insert some products to update
            var products = new List<Product>
            {
                new Product { Id = 400, Name = "Update Product 1", Price = 100.00m, CategoryId = 1, Description = "Update product 1", InStock = true },
                new Product { Id = 401, Name = "Update Product 2", Price = 200.00m, CategoryId = 1, Description = "Update product 2", InStock = true },
                new Product { Id = 402, Name = "Update Product 3", Price = 300.00m, CategoryId = 1, Description = "Update product 3", InStock = true }
            };

            context.Products.AddRange(products);
            context.SaveChanges();

            // Now apply a 10% discount to all products in category 1
            var productsToUpdate = context.Products.Where(p => p.CategoryId == 1 && p.Id >= 400).ToList();
            foreach (var product in productsToUpdate)
            {
                product.Price = product.Price * 0.9m; // 10% discount
            }

            var result = context.SaveChanges();

            result.Should().Be(3);
            _output.WriteLine($"Bulk updated {result} products with 10% discount");

            // Verify all products were updated
            var updatedProducts = context.Products.Where(p => p.Id >= 400 && p.Id <= 402).ToList();
            updatedProducts.Should().Contain(p => p.Price == 90.00m);
            updatedProducts.Should().Contain(p => p.Price == 180.00m);
            updatedProducts.Should().Contain(p => p.Price == 270.00m);
        }

        #endregion

        #region BULK DELETE TESTS

        [Fact]
        public void CanDelete_Multiple_Customers()
        {
            using var context = CreateContext();

            // First, insert some customers to delete
            var customers = new List<Customer>
            {
                new Customer { Id = 500, Name = "Delete Customer 1", Email = "delete1@example.com", Age = 25, City = "Delete City", IsActive = true },
                new Customer { Id = 501, Name = "Delete Customer 2", Email = "delete2@example.com", Age = 30, City = "Delete City", IsActive = true },
                new Customer { Id = 502, Name = "Delete Customer 3", Email = "delete3@example.com", Age = 35, City = "Delete City", IsActive = true }
            };

            context.Customers.AddRange(customers);
            context.SaveChanges();

            // Now delete all customers from "Delete City"
            var customersToDelete = context.Customers.Where(c => c.City == "Delete City").ToList();
            context.Customers.RemoveRange(customersToDelete);

            var result = context.SaveChanges();

            result.Should().Be(3);
            _output.WriteLine($"Bulk deleted {result} customers");

            // Verify all customers were deleted
            var remainingCustomers = context.Customers.Where(c => c.City == "Delete City").ToList();
            remainingCustomers.Should().BeEmpty();
        }

        [Fact]
        public void CanDelete_Multiple_Products()
        {
            using var context = CreateContext();

            // First, insert some products to delete
            var products = new List<Product>
            {
                new Product { Id = 500, Name = "Delete Product 1", Price = 100.00m, CategoryId = 3, Description = "Delete product 1", InStock = false },
                new Product { Id = 501, Name = "Delete Product 2", Price = 200.00m, CategoryId = 3, Description = "Delete product 2", InStock = false },
                new Product { Id = 502, Name = "Delete Product 3", Price = 300.00m, CategoryId = 3, Description = "Delete product 3", InStock = false }
            };

            context.Products.AddRange(products);
            context.SaveChanges();

            // Now delete all out-of-stock products in category 3
            var productsToDelete = context.Products.Where(p => p.CategoryId == 3 && !p.InStock && p.Id >= 500).ToList();
            context.Products.RemoveRange(productsToDelete);

            var result = context.SaveChanges();

            result.Should().Be(3);
            _output.WriteLine($"Bulk deleted {result} out-of-stock products");

            // Verify all products were deleted
            var remainingProducts = context.Products.Where(p => p.Id >= 500 && p.Id <= 502).ToList();
            remainingProducts.Should().BeEmpty();
        }

        #endregion

        #region MIXED BULK OPERATIONS

        [Fact]
        public void CanPerform_Mixed_Bulk_Operations()
        {
            using var context = CreateContext();

            // Add some new customers
            var newCustomers = new List<Customer>
            {
                new Customer { Id = 600, Name = "Mixed Customer 1", Email = "mixed1@example.com", Age = 25, City = "Mixed City", IsActive = true },
                new Customer { Id = 601, Name = "Mixed Customer 2", Email = "mixed2@example.com", Age = 30, City = "Mixed City", IsActive = true }
            };

            context.Customers.AddRange(newCustomers);

            // Update existing customers
            var existingCustomers = context.Customers.Where(c => c.Id >= 1 && c.Id <= 2).ToList();
            foreach (var customer in existingCustomers)
            {
                customer.Age += 1; // Age everyone by 1 year
            }

            // Add new products
            var newProducts = new List<Product>
            {
                new Product { Id = 600, Name = "Mixed Product 1", Price = 100.00m, CategoryId = 1, Description = "Mixed product 1", InStock = true },
                new Product { Id = 601, Name = "Mixed Product 2", Price = 200.00m, CategoryId = 2, Description = "Mixed product 2", InStock = true }
            };

            context.Products.AddRange(newProducts);

            var result = context.SaveChanges();

            result.Should().Be(6); // 2 new customers + 2 updated customers + 2 new products
            _output.WriteLine($"Mixed bulk operations completed with {result} total changes");

            // Verify the operations
            var insertedCustomers = context.Customers.Where(c => c.Id >= 600 && c.Id <= 601).ToList();
            insertedCustomers.Should().HaveCount(2);

            var insertedProducts = context.Products.Where(p => p.Id >= 600 && p.Id <= 601).ToList();
            insertedProducts.Should().HaveCount(2);
        }

        #endregion

        #region PERFORMANCE TESTS

        [Fact]
        public void CanInsert_Large_Number_Of_Customers()
        {
            using var context = CreateContext();

            var customers = new List<Customer>();
            for (int i = 0; i < 50; i++)
            {
                customers.Add(new Customer
                {
                    Id = 1000 + i,
                    Name = $"Performance Customer {i}",
                    Email = $"perf{i}@example.com",
                    Age = 20 + (i % 40),
                    City = $"Performance City {i % 10}",
                    IsActive = i % 2 == 0
                });
            }

            var startTime = DateTime.Now;
            context.Customers.AddRange(customers);
            var result = context.SaveChanges();
            var endTime = DateTime.Now;

            var duration = endTime - startTime;

            result.Should().Be(50);
            _output.WriteLine($"Bulk inserted {result} customers in {duration.TotalMilliseconds}ms");

            // Verify all customers were inserted
            var insertedCustomers = context.Customers.Where(c => c.Id >= 1000 && c.Id < 1050).ToList();
            insertedCustomers.Should().HaveCount(50);
        }

        #endregion

        #region ERROR HANDLING TESTS

        [Fact]
        public void Should_Handle_Bulk_Insert_With_Duplicate_Keys()
        {
            using var context = CreateContext();

            var customers = new List<Customer>
            {
                new Customer { Id = 700, Name = "Duplicate Customer 1", Email = "dup1@example.com", Age = 25, City = "Dup City", IsActive = true },
                new Customer { Id = 1, Name = "Duplicate Customer 2", Email = "dup2@example.com", Age = 30, City = "Dup City", IsActive = true }, // This ID already exists
                new Customer { Id = 701, Name = "Duplicate Customer 3", Email = "dup3@example.com", Age = 35, City = "Dup City", IsActive = true }
            };

            context.Customers.AddRange(customers);

            // This should throw an exception due to duplicate primary key
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Expected exception caught: {exception.Message}");

            // Verify that no customers were inserted due to the failed transaction
            var insertedCustomers = context.Customers.Where(c => c.Id == 700 || c.Id == 701).ToList();
            insertedCustomers.Should().BeEmpty();
        }

        #endregion
    }
}