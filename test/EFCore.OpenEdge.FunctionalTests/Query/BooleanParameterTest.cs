using System.Linq;
using EFCore.OpenEdge.FunctionalTests.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    /// <summary>
    /// Tests for boolean parameter conversion functionality.
    /// Ensures that boolean parameters are correctly converted to integer values for OpenEdge.
    /// </summary>
    public class BooleanParameterTest(ITestOutputHelper output) : ECommerceTestBase
    {
        private readonly ITestOutputHelper _output = output;

        [Fact]
        public void BooleanParameter_False_GeneratesCorrectSQL()
        {
            // Arrange
            using var context = CreateContext();
            bool isActiveFilter = false;
            
            // Act - This should generate WHERE column = 0
            var inactiveCustomers = context.Customers
                .Where(c => c.IsActive == isActiveFilter)
                .ToList();
            
            // Assert
            inactiveCustomers.Should().NotBeEmpty();
            inactiveCustomers.Should().OnlyContain(c => !c.IsActive);
        }

        [Fact]
        public void BooleanParameter_True_GeneratesCorrectSQL()
        {
            // Arrange
            using var context = CreateContext();
            bool isActiveFilter = true;
            
            // Act - This should generate WHERE column = 1
            var activeCustomers = context.Customers
                .Where(c => c.IsActive == isActiveFilter)
                .ToList();
            
            // Assert
            activeCustomers.Should().NotBeEmpty();
            activeCustomers.Should().OnlyContain(c => c.IsActive);
        }

        [Fact]
        public void BooleanParameter_NotEqual_False_GeneratesCorrectSQL()
        {
            // Arrange
            using var context = CreateContext();
            bool isActiveFilter = false;
            
            // Act - This should generate WHERE column <> 0
            var activeCustomers = context.Customers
                .Where(c => c.IsActive != isActiveFilter)
                .ToList();
            
            // Assert
            activeCustomers.Should().NotBeEmpty();
            activeCustomers.Should().OnlyContain(c => c.IsActive);
        }

        [Fact]
        public void BooleanParameter_NotEqual_True_GeneratesCorrectSQL()
        {
            // Arrange
            using var context = CreateContext();
            bool isActiveFilter = true;
            
            // Act - This should generate WHERE column <> 1
            var inactiveCustomers = context.Customers
                .Where(c => c.IsActive != isActiveFilter)
                .ToList();
            
            // Assert
            inactiveCustomers.Should().NotBeEmpty();
            inactiveCustomers.Should().OnlyContain(c => !c.IsActive);
        }

        [Fact]
        public void BooleanParameter_InComplexQuery_WorksCorrectly()
        {
            // Arrange
            using var context = CreateContext();
            bool isActiveFilter = false;
            int ageThreshold = 30;
            
            // Act - Complex query with boolean parameter
            var customers = context.Customers
                .Where(c => c.IsActive == isActiveFilter && c.Age > ageThreshold)
                .OrderBy(c => c.Name)
                .Take(5)
                .ToList();
            
            // Assert
            customers.Should().HaveCountLessOrEqualTo(5);
            customers.Should().OnlyContain(c => !c.IsActive && c.Age > ageThreshold);
            customers.Should().BeInAscendingOrder(c => c.Name);
        }
    }
}