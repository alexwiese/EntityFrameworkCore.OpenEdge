using System;
using System.Linq;
using EFCore.OpenEdge.FunctionalTests.Shared;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public class DateOnlyMethodTranslationTests : ECommerceTestBase
    {
        private readonly ITestOutputHelper _output;

        public DateOnlyMethodTranslationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CanUseAddDays()
        {
            using var context = CreateContext();
            var baseDate = new DateOnly(2024, 1, 1);
            
            // Find orders that are 30 days after a base date
            var orders = context.Orders
                .Where(o => o.OrderDate == baseDate.AddDays(30))
                .ToList();

            // Query should execute without translation errors
            orders.Should().NotBeNull();
        }

        [Fact]
        public void CanUseAddDaysInComparison()
        {
            using var context = CreateContext();
            var baseDate = new DateOnly(2024, 1, 1);
            
            // Find orders that are within 7 days of base date
            var orders = context.Orders
                .Where(o => o.OrderDate > baseDate && o.OrderDate <= baseDate.AddDays(7))
                .ToList();

            orders.Should().NotBeNull();
        }

        [Fact]
        public void CanUseAddMonths()
        {
            using var context = CreateContext();
            var baseDate = new DateOnly(2024, 1, 1);
            
            // Find orders that are 3 months after base date
            var orders = context.Orders
                .Where(o => o.OrderDate == baseDate.AddMonths(3))
                .ToList();

            orders.Should().NotBeNull();
        }

        [Fact]
        public void CanUseAddMonthsInRange()
        {
            using var context = CreateContext();
            var baseDate = new DateOnly(2024, 1, 1);
            
            // Find orders in a 6-month window
            var orders = context.Orders
                .Where(o => o.OrderDate >= baseDate && o.OrderDate < baseDate.AddMonths(6))
                .ToList();

            orders.Should().NotBeNull();
        }

        [Fact]
        public void CanUseAddYears()
        {
            using var context = CreateContext();
            var baseDate = new DateOnly(2023, 1, 1);
            
            // Find orders that are 1 year after base date
            var orders = context.Orders
                .Where(o => o.OrderDate == baseDate.AddYears(1))
                .ToList();

            orders.Should().NotBeNull();
        }

        [Fact]
        public void CanUseAddYearsInComparison()
        {
            using var context = CreateContext();
            var baseDate = new DateOnly(2023, 1, 1);
            
            // Find orders within a year
            var orders = context.Orders
                .Where(o => o.OrderDate >= baseDate && o.OrderDate < baseDate.AddYears(1))
                .ToList();

            orders.Should().NotBeNull();
        }

        [Fact]
        public void CanCombineAddMethods()
        {
            using var context = CreateContext();
            var baseDate = new DateOnly(2024, 1, 1);
            
            // Find orders that are 1 year and 3 months after base date
            var targetDate = baseDate.AddYears(1).AddMonths(3);
            var orders = context.Orders
                .Where(o => o.OrderDate == targetDate)
                .ToList();

            orders.Should().NotBeNull();
        }

        [Fact]
        public void CanUseNegativeValuesInAddMethods()
        {
            using var context = CreateContext();
            var baseDate = new DateOnly(2024, 6, 15);
            
            // Find orders from 30 days before base date
            var orders = context.Orders
                .Where(o => o.OrderDate == baseDate.AddDays(-30))
                .ToList();

            orders.Should().NotBeNull();
        }

        [Fact]
        public void CanFilterByDayOfYear()
        {
            using var context = CreateContext();
            
            // Find orders on the 100th day of the year
            var orders = context.Orders
                .Where(o => o.OrderDate.Value.DayOfYear == 100)
                .ToList();

            orders.Should().NotBeNull();
        }

        [Fact]
        public void ComplexDateCalculation()
        {
            using var context = CreateContext();
            var referenceDate = new DateOnly(2024, 1, 15);
            
            // Complex query combining multiple date operations
            var orders = context.Orders
                .Where(o => o.OrderDate != null &&
                           o.OrderDate.Value.Year == referenceDate.Year &&
                           o.OrderDate.Value >= referenceDate.AddMonths(-1) &&
                           o.OrderDate.Value <= referenceDate.AddMonths(1).AddDays(15))
                .OrderBy(o => o.OrderDate)
                .ToList();

            orders.Should().NotBeNull();
        }
    }
}