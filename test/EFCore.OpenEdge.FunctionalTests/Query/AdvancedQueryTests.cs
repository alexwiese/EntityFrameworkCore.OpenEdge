using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public class AdvancedQueryTests : BasicQueryTestBase
    {
        private readonly ITestOutputHelper _output;

        public AdvancedQueryTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region AGGREGATION TESTS

        [Fact]
        public void CanExecute_GroupBy_WithCount()
        {
            using var context = CreateContext();

            var cityGroups = context.Customers
                .GroupBy(c => c.City)
                .Select(g => new { City = g.Key, Count = g.Count() })
                .ToList();

            cityGroups.Should().NotBeNull();
        }

        [Fact]
        public void CanExecute_GroupBy_WithSum()
        {
            using var context = CreateContext();

            var categoryTotals = context.Products
                .GroupBy(p => p.CategoryId)
                .Select(g => new 
                { 
                    CategoryId = g.Key, 
                    TotalValue = g.Sum(p => p.Price),
                    ProductCount = g.Count()
                })
                .ToList();

            categoryTotals.Should().NotBeNull();
        }

        // [Fact]
        // public void CanExecute_GroupBy_WithAverage()
        // {
        //     using var context = CreateContext();

        //     var ageByCity = context.Customers
        //         .Where(c => c.IsActive)
        //         .GroupBy(c => c.City)
        //         .Select(g => new 
        //         { 
        //             City = g.Key, 
        //             AverageAge = g.Average(c => c.Age),
        //             MinAge = g.Min(c => c.Age),
        //             MaxAge = g.Max(c => c.Age)
        //         })
        //         .ToList();

        //     ageByCity.Should().NotBeNull();
        // }

        #endregion

        #region SUBQUERY TESTS

        [Fact]
        public void CanExecute_SubqueryInWhere()
        {
            using var context = CreateContext();

            var averageAge = context.Customers.Average(c => c.Age);
            
            var aboveAverageCustomers = context.Customers
                .Where(c => c.Age > context.Customers.Average(x => x.Age))
                .ToList();

            _output.WriteLine($"Average age: {averageAge:F1}");
            _output.WriteLine($"Customers above average: {aboveAverageCustomers.Count}");

            aboveAverageCustomers.Should().NotBeNull();
        }

        [Fact]
        public void CanExecute_ExistsSubquery()
        {
            using var context = CreateContext();

            var customersWithOrders = context.Customers
                .Where(c => context.Orders.Any(o => o.CustomerId == c.Id))
                .ToList();

            _output.WriteLine($"Customers with orders: {customersWithOrders.Count}");

            customersWithOrders.Should().NotBeNull();
        }

        [Fact]
        public void CanExecute_NotExistsSubquery()
        {
            using var context = CreateContext();

            var customersWithoutOrders = context.Customers
                .Where(c => !context.Orders.Any(o => o.CustomerId == c.Id))
                .ToList();

            _output.WriteLine($"Customers without orders: {customersWithoutOrders.Count}");

            customersWithoutOrders.Should().NotBeNull();
        }

        #endregion

        #region STRING OPERATIONS

        [Fact]
        public void CanExecute_StringContains()
        {
            using var context = CreateContext();

            var customersWithJohnInName = context.Customers
                .Where(c => c.Name.Contains("John"))
                .ToList();

            _output.WriteLine($"Customers with 'John' in name: {customersWithJohnInName.Count}");

            customersWithJohnInName.Should().NotBeNull();
        }
        

        [Fact]
        public void CanExecute_StringStartsWith()
        {
            using var context = CreateContext();

            var customersStartingWithJ = context.Customers
                .Where(c => c.Name.StartsWith("J"))
                .ToList();

            _output.WriteLine($"Customers with names starting with 'J': {customersStartingWithJ.Count}");

            customersStartingWithJ.Should().NotBeNull();
        }

        [Fact]
        public void CanExecute_StringEndsWith()
        {
            using var context = CreateContext();

            var emailsEndingWithCom = context.Customers
                .Where(c => c.Email.EndsWith(".com"))
                .ToList();

            _output.WriteLine($"Customers with .com emails: {emailsEndingWithCom.Count}");

            emailsEndingWithCom.Should().NotBeNull();
        }

        // [Fact]
        // public void CanExecute_StringLength()
        // {
        //     using var context = CreateContext();

        //     var customersWithLongNames = context.Customers
        //         .Where(c => c.Name.Length > 10)
        //         .ToList();

        //     _output.WriteLine($"Customers with names longer than 10 characters: {customersWithLongNames.Count}");

        //     customersWithLongNames.Should().NotBeNull();
        // }

        #endregion

        #region MATH OPERATIONS

        [Fact]
        public void CanExecute_MathOperations()
        {
            using var context = CreateContext();

            var priceCalculations = context.Products
                .Select(p => new 
                {
                    p.Name,
                    OriginalPrice = p.Price,
                    DiscountedPrice = p.Price * 0.9m,
                    RoundedPrice = Math.Round(p.Price, 0),
                    AbsolutePrice = Math.Abs(p.Price - 100)
                })
                .ToList();

            priceCalculations.Should().NotBeNull();
        }

        #endregion

        #region DATE OPERATIONS

        // [Fact]
        // public void CanExecute_DateOperations()
        // {
        //     using var context = CreateContext();

        //     var recentOrders = context.Orders
        //         .Where(o => o.OrderDate >= DateTime.Now.AddDays(-30))
        //         .Select(o => new 
        //         {
        //             o.Id,
        //             o.OrderDate,
        //             DaysAgo = (DateTime.Now - o.OrderDate).Days,
        //             Year = o.OrderDate.Year,
        //             Month = o.OrderDate.Month
        //         })
        //         .ToList();

        //     _output.WriteLine($"Recent orders (last 30 days): {recentOrders.Count}");

        //     recentOrders.Should().NotBeNull();
        // }

        #endregion

        #region PAGING AND SORTING

        [Fact]
        public void CanExecute_Skip_Take()
        {
            using var context = CreateContext();

            var pagedCustomers = context.Customers
                .OrderBy(c => c.Name)
                .Skip(5)
                .Take(10)
                .ToList();

            _output.WriteLine($"Retrieved page of {pagedCustomers.Count} customers (skipped 5, took 10)");

            pagedCustomers.Should().NotBeNull();
            pagedCustomers.Count.Should().BeLessOrEqualTo(10);
        }

        [Fact]
        public void CanExecute_Top_Without_Skip()
        {
            using var context = CreateContext();

            var topCustomers = context.Customers
                .OrderByDescending(c => c.Age)
                .Take(5)
                .ToList();

            _output.WriteLine($"Top 5 oldest customers retrieved");

            topCustomers.Should().NotBeNull();
            topCustomers.Count.Should().BeLessOrEqualTo(5);
        }

        [Fact]
        public void CanExecute_Multiple_OrderBy()
        {
            using var context = CreateContext();

            var sortedCustomers = context.Customers
                .OrderBy(c => c.City)
                .ThenByDescending(c => c.Age)
                .ThenBy(c => c.Name)
                .ToList();

            _output.WriteLine($"Customers sorted by City (asc), Age (desc), Name (asc): {sortedCustomers.Count}");

            sortedCustomers.Should().NotBeNull();
        }

        #endregion

        #region CASE/CONDITIONAL OPERATIONS

        // [Fact]
        // public void CanExecute_ConditionalSelect()
        // {
        //     using var context = CreateContext();

        //     var customerCategories = context.Customers
        //         .Select(c => new 
        //         {
        //             c.Name,
        //             c.Age,
        //             AgeCategory = c.Age < 30 ? "Young" : c.Age < 50 ? "Middle-aged" : "Senior",
        //             IsAdult = c.Age >= 18
        //         })
        //         .ToList();

        //     _output.WriteLine($"Customer age categories for {customerCategories.Count} customers");

        //     customerCategories.Should().NotBeNull();
        // }

        #endregion

        #region NULL HANDLING

        [Fact]
        public void CanExecute_NullChecks()
        {
            using var context = CreateContext();

            var customersWithEmail = context.Customers
                .Where(c => c.Email != null && c.Email != "")
                .ToList();

            var customersWithoutEmail = context.Customers
                .Where(c => c.Email == null || c.Email == "")
                .ToList();

            _output.WriteLine($"Customers with email: {customersWithEmail.Count}");
            _output.WriteLine($"Customers without email: {customersWithoutEmail.Count}");

            customersWithEmail.Should().NotBeNull();
            customersWithoutEmail.Should().NotBeNull();
        }

        #endregion
    }
}
