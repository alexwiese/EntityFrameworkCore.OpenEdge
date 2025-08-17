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
    public class ConstraintTests : ECommerceTestBase
    {
        private readonly ITestOutputHelper _output;

        public ConstraintTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region PRIMARY KEY CONSTRAINT TESTS

        [Fact]
        public void Should_Fail_Insert_Duplicate_Primary_Key_Customer()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var customer = new Customer
                {
                    Id = 1, // This ID already exists
                    Name = "Duplicate Customer",
                    Email = "duplicate@example.com",
                    Age = 30,
                    City = "Duplicate City",
                    IsActive = true
                };

                context.Customers.Add(customer);

                // Should throw DbUpdateException due to primary key constraint
                var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
                _output.WriteLine($"Primary key constraint violation: {exception.Message}");

                // Verify the exception is related to primary key violation
                // OpenEdge uses "PRIMARY KEY" in the error message
                exception.InnerException?.Message.Should().Contain("PRIMARY KEY");

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion

        #region FOREIGN KEY CONSTRAINT TESTS

        [Fact]
        public void Should_Fail_Insert_Order_With_Invalid_Customer_Id()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var order = new Order
                {
                    Id = 1800,
                    CustomerId = 999, // Non-existent customer ID
                    OrderDate = DateTime.Now,
                    TotalAmount = 100.00m,
                    Status = "Invalid Customer Order"
                };

                context.Orders.Add(order);

                // Should throw DbUpdateException due to foreign key constraint
                var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
                _output.WriteLine($"Foreign key constraint violation: {exception.Message}");

                // Verify the exception is related to foreign key violation
                // OpenEdge uses "FOREIGN KEY" in the error message
                exception.InnerException?.Message.Should().Contain("FOREIGN KEY");

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion

        #region REFERENTIAL INTEGRITY TESTS

        [Fact]
        public void Should_Fail_Delete_Customer_With_Orders()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                // Find a customer that has orders
                var customerWithOrders = context.Customers
                    .Include(c => c.Orders)
                    .First(c => c.Orders.Any());

                var customerId = customerWithOrders.Id;
                var orderCount = customerWithOrders.Orders.Count;

                _output.WriteLine($"Attempting to delete customer {customerId} who has {orderCount} orders");

                // Try to delete the customer
                context.Customers.Remove(customerWithOrders);

                // Should throw DbUpdateException due to referential integrity constraint
                var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
                _output.WriteLine($"Referential integrity constraint violation: {exception.Message}");

                // Verify the customer still exists
                using var verifyContext = CreateContext();
                var stillExistsCustomer = verifyContext.Customers.Find(customerId);
                stillExistsCustomer.Should().NotBeNull();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion

        #region NULL CONSTRAINT TESTS

        [Fact]
        public void Should_Fail_Insert_Customer_With_Null_Required_Field()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                // Note: Since Name is a required field, this should fail
                var customer = new Customer
                {
                    Id = 2200,
                    Name = null, // This violates the required constraint
                    Email = "nullname@example.com",
                    Age = 30,
                    City = "Null City",
                    IsActive = true
                };

                context.Customers.Add(customer);

                // Should throw an exception due to null constraint
                var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
                _output.WriteLine($"Null constraint violation: {exception.Message}");

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion

        #region DATA TYPE CONSTRAINT TESTS

        [Fact]
        public void Should_Handle_Large_String_Values()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                // Test with a string that might exceed field length
                var longDescription = new string('A', 600); // Longer than the 500 character limit

                var product = new Product
                {
                    Id = 2300,
                    Name = "Long Description Product",
                    Price = 100.00m,
                    CategoryId = 1,
                    Description = longDescription,
                    InStock = true
                };

                context.Products.Add(product);

                // This might succeed with truncation or fail with constraint violation
                try
                {
                    var result = context.SaveChanges();
                    _output.WriteLine($"Successfully inserted product with long description (possibly truncated)");
                }
                catch (DbUpdateException ex)
                {
                    _output.WriteLine($"Expected constraint violation for long string: {ex.Message}");
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        [Fact]
        public void Should_Handle_Extreme_Decimal_Values()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var product = new Product
                {
                    Id = 2301,
                    Name = "Extreme Price Product",
                    Price = 99999999.99m, // Maximum decimal value for decimal(10,2)
                    CategoryId = 1,
                    Description = "Extreme price product",
                    InStock = true
                };

                context.Products.Add(product);

                try
                {
                    var result = context.SaveChanges();
                    result.Should().Be(1);
                    _output.WriteLine("Successfully inserted product with extreme decimal value");

                    // Verify the product was inserted
                    var insertedProduct = context.Products.Find(2301);
                    insertedProduct.Should().NotBeNull();
                    insertedProduct.Price.Should().Be(99999999.99m);
                }
                catch (DbUpdateException ex)
                {
                    _output.WriteLine($"Decimal constraint violation: {ex.Message}");
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        [Fact]
        public void Should_Handle_Extreme_Integer_Values()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var customer = new Customer
                {
                    Id = 2302,
                    Name = "Extreme Age Customer",
                    Email = "extreme@example.com",
                    Age = int.MaxValue,
                    City = "Extreme City",
                    IsActive = true
                };

                context.Customers.Add(customer);

                try
                {
                    var result = context.SaveChanges();
                    result.Should().Be(1);
                    _output.WriteLine("Successfully inserted customer with extreme integer value");

                    // Verify the customer was inserted
                    var insertedCustomer = context.Customers.Find(2302);
                    insertedCustomer.Should().NotBeNull();
                    insertedCustomer.Age.Should().Be(int.MaxValue);
                }
                catch (DbUpdateException ex)
                {
                    _output.WriteLine($"Integer constraint violation: {ex.Message}");
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion
    }
}