using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public class JoinQueryTests : BasicQueryTestBase
    {
        private readonly ITestOutputHelper _output;

        public JoinQueryTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // TODO: Ensure that there are no name collisions for table joins when tables have fields with the same names

        [Fact]
        public void CanExecute_SimpleInnerJoin()
        {
            using var context = CreateContext();

            var query = from customer in context.Customers
                       join order in context.Orders on customer.Id equals order.CustomerId
                       select new { customer.Name, order.OrderDate };

            var results = query.ToList();
            _output.WriteLine($"Found {results.Count} customer-order combinations");

            // Test should pass if joins are working
            results.Should().NotBeNull();
        }

        [Fact]
        public void CanExecute_NavigationPropertyInclude()
        {
            using var context = CreateContext();

            var customersWithOrders = context.Customers
                .Include(c => c.Orders.Where(o => o.TotalAmount > 1000))
                .ToList();

            // Test navigation property loading
            customersWithOrders.Should().NotBeNull();
        }

        [Fact]
        public void CanExecute_MultiLevelInclude()
        {
            using var context = CreateContext();

            var customersWithOrderDetails = context.Customers
                .Include(c => c.Orders)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ToList();

            _output.WriteLine($"Found {customersWithOrderDetails.Count} customers with full order details");

            customersWithOrderDetails.Should().NotBeNull();
        }

        [Fact]
        public void CanExecute_ComplexQueryWithJoins()
        {
            using var context = CreateContext();

            var orderSummaries = from order in context.Orders
                               join customer in context.Customers on order.CustomerId equals customer.Id
                               join orderItem in context.OrderItems on order.Id equals orderItem.OrderId
                               join product in context.Products on orderItem.ProductId equals product.Id
                               select new 
                               {
                                   CustomerName = customer.Name,
                                   OrderDate = order.OrderDate,
                                   ProductName = product.Name,
                                   Quantity = orderItem.Quantity,
                                   TotalPrice = orderItem.Quantity * orderItem.UnitPrice
                               };

            var results = orderSummaries.ToList();
            _output.WriteLine($"Found {results.Count} order line items");

            results.Should().NotBeNull();
        }

        [Fact]
        public void CanExecute_LeftOuterJoin()
        {
            using var context = CreateContext();

            var customersWithOptionalOrders = from customer in context.Customers
                                             join order in context.Orders on customer.Id equals order.CustomerId into orderGroup
                                             from order in orderGroup.DefaultIfEmpty()
                                             select new 
                                             {
                                                 CustomerName = customer.Name,
                                                 OrderDate = order != null ? order.OrderDate : (DateTime?)null
                                             };

            var results = customersWithOptionalOrders.ToList();
            _output.WriteLine($"Found {results.Count} customers (including those without orders)");

            results.Should().NotBeNull();
        }
    }
}
