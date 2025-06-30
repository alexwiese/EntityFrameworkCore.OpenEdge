using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using EFCore.OpenEdge.FunctionalTests.Query.Models;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public class CrudOperationTests : BasicQueryTestBase
    {
        private readonly ITestOutputHelper _output;

        public CrudOperationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region INSERT TESTS

        [Fact]
        public void CanInsert_SingleCustomer()
        {
            using var context = CreateContext();

            var newCustomer = new Customer
            {
                Id = 999,
                Name = "Test Customer",
                Email = "test@example.com",
                Age = 30,
                City = "Test City",
                IsActive = true
            };

            context.Customers.Add(newCustomer);
            var result = context.SaveChanges();
            
            result.Should().Be(1, "One record should be inserted");
            
            // Verify insertion by reading back
            var insertedCustomer = context.Customers.Find(999);
            insertedCustomer.Should().NotBeNull();
            insertedCustomer.Name.Should().Be("Test Customer");
            
            _output.WriteLine($"✅ Successfully inserted customer: {insertedCustomer}");

            // Cleanup
            context.Customers.Remove(newCustomer);
            context.SaveChanges();
        }

        [Fact]
        public void CanInsert_MultipleRecords()
        {
            using var context = CreateContext();

            var customers = new[]
            {
                new Customer { Id = 1001, Name = "Batch Test 1", Email = "batch1@test.com", Age = 25, City = "City1", IsActive = true },
                new Customer { Id = 1002, Name = "Batch Test 2", Email = "batch2@test.com", Age = 35, City = "City2", IsActive = false },
                new Customer { Id = 1003, Name = "Batch Test 3", Email = "batch3@test.com", Age = 45, City = "City3", IsActive = true }
            };

            context.Customers.AddRange(customers);
            var result = context.SaveChanges();
            
            result.Should().Be(3, "Three records should be inserted");
            
            // Verify all were inserted
            var insertedCustomers = context.Customers
                .Where(c => c.Id >= 1001 && c.Id <= 1003)
                .ToList();
            
            insertedCustomers.Should().HaveCount(3);
            _output.WriteLine($"✅ Successfully inserted {insertedCustomers.Count} customers in batch");

            // Cleanup
            context.Customers.RemoveRange(customers);
            context.SaveChanges();
        }

        [Fact]
        public void CanInsert_ProductWithDecimalAndBoolean()
        {
            using var context = CreateContext();

            var product = new Product
            {
                Id = 999,
                Name = "Test Product",
                Price = 99.99m,
                CategoryId = 1,
                Description = "Test product description",
                InStock = true
            };

            context.Products.Add(product);
            var result = context.SaveChanges();
            
            result.Should().Be(1);
            
            // Verify insertion with data types
            var insertedProduct = context.Products.Find(999);
            insertedProduct.Should().NotBeNull();
            insertedProduct.Price.Should().Be(99.99m);
            insertedProduct.InStock.Should().BeTrue();
            
            _output.WriteLine($"✅ Successfully inserted product with decimal price {insertedProduct.Price:C} and boolean InStock={insertedProduct.InStock}");

            // Cleanup
            context.Products.Remove(product);
            context.SaveChanges();
        }

        #endregion

        #region UPDATE TESTS

        [Fact]
        public void CanUpdate_CustomerFields()
        {
            using var context = CreateContext();

            // First, insert a test record
            var customer = new Customer
            {
                Id = 998,
                Name = "Update Test",
                Email = "update@test.com",
                Age = 25,
                City = "Original City",
                IsActive = true
            };
            
            context.Customers.Add(customer);
            context.SaveChanges();

            // Now update it
            customer.Name = "Updated Name";
            customer.Age = 26;
            customer.City = "Updated City";

            var result = context.SaveChanges();
            
            result.Should().Be(1, "One record should be updated");
            
            // Verify the update
            var updatedCustomer = context.Customers.Find(998);
            updatedCustomer.Should().NotBeNull();
            updatedCustomer.Name.Should().Be("Updated Name");
            updatedCustomer.Age.Should().Be(26);
            updatedCustomer.City.Should().Be("Updated City");
            
            _output.WriteLine($"✅ Successfully updated customer: {updatedCustomer}");

            // Cleanup
            context.Customers.Remove(customer);
            context.SaveChanges();
        }

        [Fact]
        public void CanUpdate_ProductWithMixedDataTypes()
        {
            using var context = CreateContext();

            // Insert test data
            var product = new Product
            {
                Id = 998,
                Name = "Original Product",
                Price = 50.00m,
                CategoryId = 1,
                Description = "Original description",
                InStock = false
            };
            
            context.Products.Add(product);
            context.SaveChanges();

            // Update with various data types
            product.Name = "Updated Product";
            product.Price = 75.50m;
            product.InStock = true;

            var result = context.SaveChanges();
            _output.WriteLine($"Result: {result}");
            
            result.Should().Be(1);
            
            // Verify the update
            var updatedProduct = context.Products.Find(998);
            updatedProduct.Should().NotBeNull();
            updatedProduct.Name.Should().Be("Updated Product");
            updatedProduct.Price.Should().Be(75.50m);
            updatedProduct.InStock.Should().BeTrue();
            
            _output.WriteLine($"✅ Successfully updated product: {updatedProduct.Name}, Price: {updatedProduct.Price:C}, InStock: {updatedProduct.InStock}");

            // Cleanup
            context.Products.Remove(product);
            context.SaveChanges();
        }

        [Fact]
        public void CanUpdate_WithConcurrentModifications()
        {
            using var context1 = CreateContext();
            using var context2 = CreateContext();

            // Insert test data
            var customer = new Customer
            {
                Id = 997,
                Name = "Concurrency Test",
                Email = "concurrency@test.com",
                Age = 30,
                City = "Test City",
                IsActive = true
            };
            
            context1.Customers.Add(customer);
            context1.SaveChanges();

            // Load same entity in two contexts
            var customer1 = context1.Customers.Find(997);
            var customer2 = context2.Customers.Find(997);

            // Modify in first context
            customer1.Age = 31;
            context1.SaveChanges();

            // Modify in second context - OpenEdge should allow this (limited concurrency)
            customer2.Age = 32;
            var result = context2.SaveChanges();
            
            result.Should().Be(1, "OpenEdge should allow the update due to limited concurrency control");
            
            // Verify final state
            var finalCustomer = context1.Customers.Find(997);
            finalCustomer.Age.Should().Be(32, "Second update should win");
            
            _output.WriteLine($"✅ Concurrent modifications handled: final age = {finalCustomer.Age}");

            // Cleanup
            context1.Customers.Remove(finalCustomer);
            context1.SaveChanges();
        }

        #endregion

        #region DELETE TESTS

        [Fact]
        public void CanDelete_SingleCustomer()
        {
            using var context = CreateContext();

            // Insert test data
            var customer = new Customer
            {
                Id = 996,
                Name = "Delete Test",
                Email = "delete@test.com",
                Age = 25,
                City = "Delete City",
                IsActive = true
            };
            
            context.Customers.Add(customer);
            context.SaveChanges();

            // Delete the record
            context.Customers.Remove(customer);
            var result = context.SaveChanges();
            
            result.Should().Be(1, "One record should be deleted");

            // Verify deletion
            var deletedCustomer = context.Customers.Find(996);
            deletedCustomer.Should().BeNull("Customer should be deleted");
            
            _output.WriteLine("✅ Successfully deleted customer");
        }

        [Fact]
        public void CanDelete_MultipleCustomers()
        {
            using var context = CreateContext();

            // Insert test data
            var customers = new[]
            {
                new Customer { Id = 991, Name = "Delete Batch 1", Email = "del1@test.com", Age = 25, City = "City1", IsActive = true },
                new Customer { Id = 992, Name = "Delete Batch 2", Email = "del2@test.com", Age = 35, City = "City2", IsActive = false },
                new Customer { Id = 993, Name = "Delete Batch 3", Email = "del3@test.com", Age = 45, City = "City3", IsActive = true }
            };

            context.Customers.AddRange(customers);
            context.SaveChanges();

            // Delete all test records
            context.Customers.RemoveRange(customers);
            var result = context.SaveChanges();
            
            result.Should().Be(3, "Three records should be deleted");

            // Verify deletions
            var remainingCustomers = context.Customers
                .Where(c => c.Id >= 991 && c.Id <= 993)
                .ToList();
            
            remainingCustomers.Should().BeEmpty("All test customers should be deleted");
            
            _output.WriteLine("✅ Successfully deleted multiple customers in batch");
        }

        [Fact]
        public void CanDelete_WithComplexQuery()
        {
            using var context = CreateContext();

            // Insert test data
            var testCustomers = new[]
            {
                new Customer { Id = 981, Name = "Complex Delete 1", Email = "complex1@test.com", Age = 25, City = "TestCity", IsActive = true },
                new Customer { Id = 982, Name = "Complex Delete 2", Email = "complex2@test.com", Age = 35, City = "TestCity", IsActive = false },
                new Customer { Id = 983, Name = "Complex Delete 3", Email = "complex3@test.com", Age = 45, City = "OtherCity", IsActive = true }
            };

            context.Customers.AddRange(testCustomers);
            context.SaveChanges();

            // Delete using complex where condition
            var customersToDelete = context.Customers
                .Where(c => c.City == "TestCity" && c.Age > 30)
                .ToList();

            customersToDelete.Should().HaveCount(1, "Only one customer should match the criteria");

            context.Customers.RemoveRange(customersToDelete);
            var result = context.SaveChanges();
            
            result.Should().Be(1, "Only one customer should be deleted");
            
            // Verify correct customer was deleted
            var remainingInTestCity = context.Customers
                .Where(c => c.City == "TestCity")
                .ToList();
            
            remainingInTestCity.Should().HaveCount(1, "One customer in TestCity should remain");
            remainingInTestCity[0].Age.Should().Be(25, "The younger customer should remain");
            
            _output.WriteLine($"✅ Successfully deleted customer matching complex criteria, {remainingInTestCity.Count} customers remain in TestCity");

            // Cleanup remaining test data
            var remaining = context.Customers.Where(c => c.Id >= 981 && c.Id <= 983).ToList();
            context.Customers.RemoveRange(remaining);
            context.SaveChanges();
        }

        #endregion

        #region RELATIONSHIP CRUD TESTS

        [Fact]
        public void Insert_Should_Handle_Related_Entities()
        {
            using var context = CreateContext();

            var customer = new Customer
            {
                Id = 995,
                Name = "Related Test Customer",
                Email = "related@test.com",
                Age = 30,
                City = "Related City",
                IsActive = true
            };

            var order = new Order
            {
                Id = 995,
                CustomerId = 995,
                OrderDate = DateTime.Now,
                TotalAmount = 150.00m,
                Status = "Pending"
            };

            context.Customers.Add(customer);
            context.Orders.Add(order);
            
            var result = context.SaveChanges();
            
            result.Should().Be(2, "Both customer and order should be inserted");
            _output.WriteLine("Related entity insert succeeded");

            // Cleanup
            context.Orders.Remove(order);
            context.Customers.Remove(customer);
            context.SaveChanges();
        }

        #endregion
    }
}
