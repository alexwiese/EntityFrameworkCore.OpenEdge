using System;
using System.Linq;
using EFCore.OpenEdge.FunctionalTests.Shared;
using EFCore.OpenEdge.FunctionalTests.Shared.Models;
using EFCore.OpenEdge.FunctionalTests.TestUtilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using System.Text.RegularExpressions;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public class SqlGenerationTests : ECommerceTestBase
    {
        private readonly ITestOutputHelper _output;

        public SqlGenerationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private (ECommerceTestContext context, SqlCapturingInterceptor interceptor) CreateContextWithSqlCapturing()
        {
            var interceptor = new SqlCapturingInterceptor();

            var options = CreateOptionsBuilder<ECommerceTestContext>()
                .AddInterceptors(interceptor)
                .EnableSensitiveDataLogging()
                .Options;

            var context = new ECommerceTestContext(options);
            return (context, interceptor);
        }

        #region PAGINATION TESTS

        [Fact]
        public void Skip_Take_Should_Generate_OFFSET_FETCH_SQL()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            var pagedCustomers = context.Customers
                .OrderBy(c => c.Id)
                .Skip(5)
                .Take(10)
                .ToList();

            interceptor.CapturedSql.Should().NotBeEmpty("SQL should be captured for Skip/Take");
            var sql = interceptor.CapturedSql.First();

            _output.WriteLine($"Skip(5).Take(10) generated SQL: {sql}");

            var expectedSql = @"SELECT ""c"".""Id"", ""c"".""Age"", ""c"".""City"", ""c"".""Email"", ""c"".""IsActive"", ""c"".""Name""
FROM ""PUB"".""CUSTOMERS_TEST_PROVIDER"" AS ""c""
ORDER BY ""c"".""Id""
OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY";

            // Normalize both strings to handle line ending differences
            var normalizedSql = sql.Replace("\r\n", "\n").Replace("\r", "\n");
            var normalizedExpected = expectedSql.Replace("\r\n", "\n").Replace("\r", "\n");

            normalizedSql.Should().Be(normalizedExpected, "Should generate proper OFFSET/FETCH SQL for pagination");
        }

        [Fact]
        public void Take_Only_Should_Generate_TOP_SQL()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            var topCustomers = context.Customers
                .OrderBy(c => c.Id)
                .Take(10)
                .ToList();

            interceptor.CapturedSql.Should().NotBeEmpty("SQL should be captured for Take");
            var sql = interceptor.CapturedSql.First();

            _output.WriteLine($"Take(10) generated SQL: {sql}");

            var expectedSql = @"SELECT ""c"".""Id"", ""c"".""Age"", ""c"".""City"", ""c"".""Email"", ""c"".""IsActive"", ""c"".""Name""
FROM ""PUB"".""CUSTOMERS_TEST_PROVIDER"" AS ""c""
ORDER BY ""c"".""Id""
FETCH FIRST 10 ROWS ONLY";

            // Normalize both strings to handle line ending differences
            var normalizedSql = sql.Replace("\r\n", "\n").Replace("\r", "\n");
            var normalizedExpected = expectedSql.Replace("\r\n", "\n").Replace("\r", "\n");

            normalizedSql.Should().Be(normalizedExpected, "Should generate proper TOP SQL for Take");
        }

        [Fact]
        public void Skip_Only_Should_Generate_OFFSET_SQL()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            var skippedCustomers = context.Customers
                .OrderBy(c => c.Id)
                .Skip(5)
                .ToList();

            interceptor.CapturedSql.Should().NotBeEmpty("SQL should be captured for Skip");
            var sql = interceptor.CapturedSql.First();

            _output.WriteLine($"Skip(5) generated SQL: {sql}");

            var expectedSql = @"SELECT ""c"".""Id"", ""c"".""Age"", ""c"".""City"", ""c"".""Email"", ""c"".""IsActive"", ""c"".""Name""
FROM ""PUB"".""CUSTOMERS_TEST_PROVIDER"" AS ""c""
ORDER BY ""c"".""Id""
OFFSET 5 ROWS";

            // Normalize both strings to handle line ending differences
            var normalizedSql = sql.Replace("\r\n", "\n").Replace("\r", "\n");
            var normalizedExpected = expectedSql.Replace("\r\n", "\n").Replace("\r", "\n");

            normalizedSql.Should().Be(normalizedExpected, "Should generate proper OFFSET SQL for Skip");
        }

        [Fact]
        public void Nested_Subquery_With_Take_Should_Generate_Inlined_FETCH_At_All_Levels()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            // Create a query with nested subquery similar to what OData might generate
            // This simulates a scenario where there's a subquery with its own FETCH clause
            var query = context.Customers
                .Where(c => context.Orders
                    .OrderBy(o => o.Id)
                    .Take(2)  // This inner Take should generate an inlined FETCH
                    .Any(o => o.CustomerId == c.Id))
                .OrderBy(c => c.Id)
                .Take(5)  // This outer Take should also generate an inlined FETCH
                .ToList();

            interceptor.CapturedSql.Should().NotBeEmpty("SQL should be captured for nested query");
            var sql = interceptor.CapturedSql.First();

            _output.WriteLine($"Nested query with Take generated SQL: {sql}");

            // The SQL should NOT contain any '?' parameters in FETCH clauses
            sql.Should().NotContain("FETCH FIRST ? ROWS", "All FETCH clauses should use inlined literal values, not parameters");
            sql.Should().NotContain("FETCH NEXT ? ROWS", "All FETCH clauses should use inlined literal values, not parameters");
            
            // The SQL SHOULD contain inlined FETCH clauses with literal numbers
            sql.Should().Match("*FETCH*2*ROWS*", "Inner Take(2) should generate inlined FETCH with literal 2");
            sql.Should().Match("*FETCH*5*ROWS*", "Outer Take(5) should generate inlined FETCH with literal 5");
        }
        
        #endregion
    }
}
