using System;
using System.Collections.Generic;
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
    public class TransactionTests : ECommerceTestBase
    {
        private readonly ITestOutputHelper _output;

        public TransactionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region ROLLBACK SCENARIOS

        [Fact]
        public void CanRollback_Simple_Transaction()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var customer = new Customer
                {
                    Id = 1202,
                    Name = "Rollback Customer",
                    Email = "rollback@example.com",
                    Age = 30,
                    City = "Rollback City",
                    IsActive = true
                };

                context.Customers.Add(customer);
                context.SaveChanges();

                // Explicitly rollback instead of commit
                transaction.Rollback();

                _output.WriteLine("Successfully rolled back simple transaction");

                // Verify the customer was not inserted
                using var verifyContext = CreateContext();
                var notInsertedCustomer = verifyContext.Customers.Find(1202);
                notInsertedCustomer.Should().BeNull();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        [Fact]
        public void CanRollback_Multiple_Operations_In_Transaction()
        {
            using var context = CreateContext();
            using var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                // Get original state
                var originalCustomer = context.Customers.Find(1);
                var originalAge = originalCustomer.Age;

                // Insert a customer
                var customer = new Customer
                {
                    Id = 1203,
                    Name = "Rollback Multi-Op Customer",
                    Email = "rollbackmultiop@example.com",
                    Age = 40,
                    City = "Rollback Multi-Op City",
                    IsActive = true
                };

                context.Customers.Add(customer);

                // Update an existing customer
                originalCustomer.Age = 99;

                context.SaveChanges();
                transaction.Rollback();

                _output.WriteLine("Successfully rolled back transaction with multiple operations");

                // Verify all operations were rolled back
                using var verifyContext = CreateContext();
                var notInsertedCustomer = verifyContext.Customers.Find(1203);
                var notUpdatedCustomer = verifyContext.Customers.Find(1);

                notInsertedCustomer.Should().BeNull();
                notUpdatedCustomer.Age.Should().Be(originalAge);
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