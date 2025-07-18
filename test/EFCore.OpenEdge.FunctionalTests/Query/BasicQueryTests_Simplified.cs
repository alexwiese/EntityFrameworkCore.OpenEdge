using System.Linq;
using EFCore.OpenEdge.FunctionalTests.Shared;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public class BasicQueryTests : ECommerceTestBase
    {

        private readonly ITestOutputHelper _output;

        public BasicQueryTests(ITestOutputHelper output)
        {
            _output = output;
        }


        [Fact]
        public void CanExecuteBasicSelect()
        {
            using var context = CreateContext();

            var customers = context.Customers.ToList();

            customers.Should().NotBeEmpty();
        }

        [Fact]
        public void CanExecuteBasicWhere()
        {
            using var context = CreateContext();
            
            var customer = context.Customers.Where(c => c.Name == "John Doe").Single();

            customer.Should().NotBeNull();
        }

        [Fact]
        public void CanExecuteBasicOrderBy()
        {
            using var context = CreateContext();

            var customers = context.Customers.OrderBy(c => c.Age).ToList();

            customers.Should().NotBeEmpty();
            customers[0].Age.Should().Be(25);
            customers[9].Age.Should().Be(55);
        }

        [Fact]
        public void CanExecuteBasicCount()
        {
            using var context = CreateContext();

            var count = context.Customers.Count();
            
            count.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CanExecuteBasicFirst()
        {
            using var context = CreateContext();

            var customer = context.Customers.First();

            customer.Should().NotBeNull();
        }
    }
}
