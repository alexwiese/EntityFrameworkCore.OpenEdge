using System;
using System.Linq;
using EFCore.OpenEdge.FunctionalTests.Shared;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public class DateOnlyTranslationTests(ITestOutputHelper output) : ECommerceTestBase
    {
        private readonly ITestOutputHelper _output = output;

        [Fact]
        public void CanFilterByExactDate()
        {
            using var context = CreateContext();
            var targetDate = new DateOnly(2024, 1, 15);
            
            var orders = context.Orders
                .Where(o => o.OrderDate == targetDate)
                .ToList();

            // Assert query executes without translation errors
            orders.Should().NotBeNull();
        }

        [Fact]
        public void CanFilterByDateComparison_GreaterThan()
        {
            using var context = CreateContext();
            var cutoffDate = new DateOnly(2024, 1, 1);
            
            var orders = context.Orders
                .Where(o => o.OrderDate > cutoffDate)
                .ToList();

            orders.Should().NotBeNull();
            orders.Where(o => o.OrderDate.HasValue)
                .Should().OnlyContain(o => o.OrderDate.Value > cutoffDate);
        }

        [Fact]
        public void CanFilterByDateComparison_LessThanOrEqual()
        {
            using var context = CreateContext();
            var cutoffDate = new DateOnly(2024, 12, 31);
            
            var orders = context.Orders
                .Where(o => o.OrderDate <= cutoffDate)
                .ToList();

            orders.Should().NotBeNull();
            orders.Where(o => o.OrderDate.HasValue)
                .Should().OnlyContain(o => o.OrderDate.Value <= cutoffDate);
        }

        [Fact]
        public void CanFilterByYearOnly()
        {
            using var context = CreateContext();
            
            var orders = context.Orders
                .Where(o => o.OrderDate.Value.Year == 2024)
                .ToList();

            orders.Should().NotBeNull();
            orders.Where(o => o.OrderDate.HasValue)
                .Should().OnlyContain(o => o.OrderDate.Value.Year == 2024);
        }

        [Fact]
        public void CanFilterByMonth()
        {
            using var context = CreateContext();
            
            var orders = context.Orders
                .Where(o => o.OrderDate.Value.Month == 1)
                .ToList();

            orders.Should().NotBeNull();
            orders.Where(o => o.OrderDate.HasValue)
                .Should().OnlyContain(o => o.OrderDate.Value.Month == 1);
        }

        [Fact]
        public void CanFilterByDay()
        {
            using var context = CreateContext();
            
            var orders = context.Orders
                .Where(o => o.OrderDate.Value.Day == 15)
                .ToList();

            orders.Should().NotBeNull();
            orders.Where(o => o.OrderDate.HasValue)
                .Should().OnlyContain(o => o.OrderDate.Value.Day == 15);
        }

        [Fact]
        public void CanFilterByYearAndMonth()
        {
            using var context = CreateContext();
            
            var orders = context.Orders
                .Where(o => o.OrderDate.Value.Year == 2024 && o.OrderDate.Value.Month == 1)
                .ToList();

            orders.Should().NotBeNull();
            orders.Where(o => o.OrderDate.HasValue)
                .Should().OnlyContain(o => o.OrderDate.Value.Year == 2024 && o.OrderDate.Value.Month == 1);
        }

        [Fact]
        public void CanFilterByDateRange()
        {
            using var context = CreateContext();
            var startDate = new DateOnly(2024, 1, 1);
            var endDate = new DateOnly(2024, 6, 30);
            
            var orders = context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToList();

            orders.Should().NotBeNull();
            orders.Where(o => o.OrderDate.HasValue)
                .Should().OnlyContain(o => o.OrderDate.Value >= startDate && o.OrderDate.Value <= endDate);
        }
    }
}