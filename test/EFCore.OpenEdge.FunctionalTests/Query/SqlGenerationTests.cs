using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using EFCore.OpenEdge.FunctionalTests.Query.Models;
using EFCore.OpenEdge.FunctionalTests.TestUtilities;
using Xunit;
using Xunit.Abstractions;
using System.Text.RegularExpressions;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public class SqlGenerationTests : BasicQueryTestBase
    {
        private readonly ITestOutputHelper _output;

        public SqlGenerationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private (BasicQueryContext context, SqlCapturingInterceptor interceptor) CreateContextWithSqlCapturing()
        {
            var interceptor = new SqlCapturingInterceptor();

            var options = CreateOptionsBuilder<BasicQueryContext>()
                .AddInterceptors(interceptor)
                .EnableSensitiveDataLogging()
                .Options;

            var context = new BasicQueryContext(options);
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

        #endregion
    }
}
