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
    }
}