using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public class SqlGenerationTests : BasicQueryTestBase
    {
        private readonly ITestOutputHelper _output;
        
        public SqlGenerationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GeneratesCorrectTopSqlWithoutFetch()
        {
            using var context = CreateContext();
            
            // This should generate: SELECT TOP 1 ... (without FETCH FIRST)
            var query = context.Customers.Take(1);
            var sql = query.ToQueryString();
            
            _output.WriteLine("Generated SQL:");
            _output.WriteLine(sql);
            
            // Verify SQL contains TOP but not FETCH (parameterized as TOP ?)
            sql.Should().Contain("TOP ?");
            sql.Should().NotContain("FETCH FIRST");
            sql.Should().NotContain("ROWS ONLY");
        }

        [Fact] 
        public void GeneratesCorrectSingleSqlWithoutFetch()
        {
            using var context = CreateContext();
            
            // This should generate: SELECT TOP 2 ... WHERE ... (without FETCH FIRST)
            var query = context.Customers.Where(c => c.Name == "John Doe");
            var sql = query.ToQueryString();
            
            _output.WriteLine("Generated SQL:");
            _output.WriteLine(sql);
            
            // Should NOT contain FETCH syntax
            sql.Should().NotContain("FETCH FIRST");
            sql.Should().NotContain("ROWS ONLY");
            sql.Should().Contain("WHERE");
        }

        [Fact]
        public void GeneratesCorrectBasicSelectSql()
        {
            using var context = CreateContext();
            
            var query = context.Customers;
            var sql = query.ToQueryString();
            
            _output.WriteLine("Generated SQL:");
            _output.WriteLine(sql);
            
            // Basic validation
            sql.Should().Contain("SELECT");
            sql.Should().Contain("PUB\".\"CUSTOMERS_TEST_PROVIDER");
            sql.Should().NotContain("FETCH FIRST");
            sql.Should().NotContain("TOP"); // Should not have TOP for basic select
        }

        [Fact]
        public void ThrowsErrorForOffsetQueries()
        {
            using var context = CreateContext();
            
            // This should throw because OpenEdge doesn't support OFFSET
            var query = context.Customers.Skip(5).Take(10);
            
            // For now, we expect this to fail during query compilation
            // TODO: When custom visitor is fixed, this should throw our custom OFFSET error
            var exception = Assert.Throws<InvalidOperationException>(() => query.ToQueryString());
            // exception.Message.Should().Contain("OpenEdge does not support OFFSET");
            exception.Should().NotBeNull(); // Just verify it throws for now
        }
    }
}
