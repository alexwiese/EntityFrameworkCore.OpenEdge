using System;
using System.Linq;
using System.Threading.Tasks;
using EFCore.OpenEdge.FunctionalTests.Shared;
using EFCore.OpenEdge.FunctionalTests.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Update
{
    public class ConcurrencyTests : ECommerceTestBase
    {
        private readonly ITestOutputHelper _output;

        public ConcurrencyTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region BASIC CONCURRENCY TESTS

        [Fact]
        public void CanHandle_Concurrent_Updates_To_Different_Entities()
        {
            // Test that updates to different entities don't interfere with each other
            using var context1 = CreateContext();
            using var transaction1 = context1.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            using var context2 = CreateContext();
            using var transaction2 = context2.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                // Context 1 updates customer 1
                var customer1 = context1.Customers.Find(1);
                customer1.Age = 31;

                // Context 2 updates customer 2
                var customer2 = context2.Customers.Find(2);
                customer2.Age = 26;

                // Both should save successfully
                var result1 = context1.SaveChanges();
                var result2 = context2.SaveChanges();

                result1.Should().Be(1);
                result2.Should().Be(1);

                _output.WriteLine("Successfully handled concurrent updates to different entities");

                transaction1.Commit();
                transaction2.Commit();

                // Verify both updates were applied
                using var verifyContext = CreateContext();
                var verifyCustomer1 = verifyContext.Customers.Find(1);
                var verifyCustomer2 = verifyContext.Customers.Find(2);

                verifyCustomer1.Age.Should().Be(31);
                verifyCustomer2.Age.Should().Be(26);
            }
            catch
            {
                transaction1.Rollback();
                transaction2.Rollback();
                throw;
            }
        }

        [Fact]
        public void CanHandle_Concurrent_Inserts()
        {
            // Test that concurrent inserts work correctly
            using var context1 = CreateContext();
            using var transaction1 = context1.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            using var context2 = CreateContext();
            using var transaction2 = context2.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var customer1 = new Customer
                {
                    Id = 800,
                    Name = "Concurrent Customer 1",
                    Email = "concurrent1@example.com",
                    Age = 25,
                    City = "Concurrent City 1",
                    IsActive = true
                };

                var customer2 = new Customer
                {
                    Id = 801,
                    Name = "Concurrent Customer 2",
                    Email = "concurrent2@example.com",
                    Age = 30,
                    City = "Concurrent City 2",
                    IsActive = true
                };

                context1.Customers.Add(customer1);
                context2.Customers.Add(customer2);

                // Both should save successfully
                var result1 = context1.SaveChanges();
                var result2 = context2.SaveChanges();

                result1.Should().Be(1);
                result2.Should().Be(1);

                _output.WriteLine("Successfully handled concurrent inserts");

                transaction1.Commit();
                transaction2.Commit();

                // Verify both customers were inserted
                using var verifyContext = CreateContext();
                var insertedCustomers = verifyContext.Customers.Where(c => c.Id == 800 || c.Id == 801).ToList();
                insertedCustomers.Should().HaveCount(2);
            }
            catch
            {
                transaction1.Rollback();
                transaction2.Rollback();
                throw;
            }
        }

        #endregion

        #region TRANSACTION ISOLATION TESTS

        [Fact]
        public void CanHandle_Multiple_Readers()
        {
            // Test that multiple readers can access the same data simultaneously
            using var context1 = CreateContext();
            using var context2 = CreateContext();
            using var context3 = CreateContext();

            // All contexts read the same customer
            var customer1 = context1.Customers.Find(1);
            var customer2 = context2.Customers.Find(1);
            var customer3 = context3.Customers.Find(1);

            // All should have the same data
            customer1.Name.Should().Be(customer2.Name);
            customer2.Name.Should().Be(customer3.Name);
            customer1.Age.Should().Be(customer2.Age);
            customer2.Age.Should().Be(customer3.Age);

            _output.WriteLine("Successfully handled multiple concurrent readers");
        }

        #endregion

        #region ASYNC CONCURRENCY TESTS

        [Fact]
        public async Task CanHandle_Async_Concurrent_Operations()
        {
            // Test async operations running concurrently
            var task1 = InsertCustomerAsync(900, "Async Customer 1");
            var task2 = InsertCustomerAsync(901, "Async Customer 2");
            var task3 = InsertProductAsync(900, "Async Product 1");

            // Wait for all tasks to complete
            var results = await Task.WhenAll(task1, task2, task3);

            results.Should().AllSatisfy(result => result.Should().Be(1));

            _output.WriteLine("Successfully handled async concurrent operations");

            // Verify all entities were inserted
            using var verifyContext = CreateContext();
            var customers = verifyContext.Customers.Where(c => c.Id >= 900 && c.Id <= 901).ToList();
            var products = verifyContext.Products.Where(p => p.Id == 900).ToList();

            customers.Should().HaveCount(2);
            products.Should().HaveCount(1);
        }

        private async Task<int> InsertCustomerAsync(int id, string name)
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            
            try
            {
                var customer = new Customer
                {
                    Id = id,
                    Name = name,
                    Email = $"async{id}@example.com",
                    Age = 25,
                    City = "Async City",
                    IsActive = true
                };

                context.Customers.Add(customer);
                var result = await context.SaveChangesAsync();
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private async Task<int> InsertProductAsync(int id, string name)
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            
            try
            {
                var product = new Product
                {
                    Id = id,
                    Name = name,
                    Price = 100.00m,
                    CategoryId = 1,
                    Description = "Async product",
                    InStock = true
                };

                context.Products.Add(product);
                var result = await context.SaveChangesAsync();
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion

        #region DEADLOCK PREVENTION TESTS

        [Fact]
        public void CanHandle_Potential_Deadlock_Scenario()
        {
            // Test a scenario that might cause deadlocks in other databases
            // OpenEdge prevents deadlocks by throwing lock errors
            using var context1 = CreateContext();
            using var transaction1 = context1.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            using var context2 = CreateContext();
            using var transaction2 = context2.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            bool lockErrorOccurred = false;
            
            try
            {
                // Context 1: Update customer 1, then customer 2
                var customer1_ctx1 = context1.Customers.Find(1);
                customer1_ctx1.Age = 50;
                context1.SaveChanges();

                // Context 2: Update customer 2, then customer 1
                var customer2_ctx2 = context2.Customers.Find(2);
                customer2_ctx2.City = "Deadlock City 2";
                context2.SaveChanges();

                // Now try to access the records in opposite order - this should cause a lock conflict
                try
                {
                    // Context 1 tries to access customer 2 (which context 2 has locked)
                    var customer2_ctx1 = context1.Customers.Find(2);
                    customer2_ctx1.Age = 51;
                    context1.SaveChanges();
                }
                catch (Exception ex) when (ex.Message.Contains("Failure getting record lock"))
                {
                    _output.WriteLine($"Context 1 lock error (expected): {ex.Message}");
                    lockErrorOccurred = true;
                }

                if (!lockErrorOccurred)
                {
                    try
                    {
                        // Context 2 tries to access customer 1 (which context 1 has locked)
                        var customer1_ctx2 = context2.Customers.Find(1);
                        customer1_ctx2.City = "Deadlock City 1";
                        context2.SaveChanges();
                    }
                    catch (Exception ex) when (ex.Message.Contains("Failure getting record lock"))
                    {
                        _output.WriteLine($"Context 2 lock error (expected): {ex.Message}");
                        lockErrorOccurred = true;
                    }
                }

                // OpenEdge should have prevented the deadlock by throwing a lock error
                lockErrorOccurred.Should().BeTrue("OpenEdge should detect and prevent deadlocks by throwing lock errors");
                
                _output.WriteLine("Successfully prevented deadlock with lock error");

                transaction1.Rollback();
                transaction2.Rollback();
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Unexpected error: {ex.Message}");
                transaction1.Rollback();
                transaction2.Rollback();
                throw;
            }
        }

        #endregion

        #region ERROR HANDLING UNDER CONCURRENCY

        [Fact]
        public void Should_Handle_Constraint_Violations_Under_Concurrency()
        {
            // Test handling of constraint violations when multiple contexts try to insert duplicate keys
            using var context1 = CreateContext();
            using var transaction1 = context1.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            using var context2 = CreateContext();
            using var transaction2 = context2.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            var customer1 = new Customer
            {
                Id = 1100,
                Name = "Constraint Customer 1",
                Email = "constraint1@example.com",
                Age = 25,
                City = "Constraint City",
                IsActive = true
            };

            var customer2 = new Customer
            {
                Id = 1100, // Same ID - this should cause a constraint violation
                Name = "Constraint Customer 2",
                Email = "constraint2@example.com",
                Age = 30,
                City = "Constraint City",
                IsActive = true
            };

            context1.Customers.Add(customer1);
            context2.Customers.Add(customer2);

            // First context should succeed
            var result1 = context1.SaveChanges();
            result1.Should().Be(1);
            transaction1.Commit();

            // Second context should fail with constraint violation
            try
            {
                context2.SaveChanges();
                // If we get here, the test should fail
                Assert.Fail("Expected DbUpdateException for duplicate key constraint violation");
            }
            catch (DbUpdateException ex)
            {
                _output.WriteLine($"Expected constraint violation: {ex.Message}");
                transaction2.Rollback();
            }

            // Verify only one customer was inserted
            using var verifyContext = CreateContext();
            var insertedCustomers = verifyContext.Customers.Where(c => c.Id == 1100).ToList();
            insertedCustomers.Should().HaveCount(1);
            insertedCustomers.First().Name.Trim().Should().Be("Constraint Customer 1");
        }

        #endregion
    }
}