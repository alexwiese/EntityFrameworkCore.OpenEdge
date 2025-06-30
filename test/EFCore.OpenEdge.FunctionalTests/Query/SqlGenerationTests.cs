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

        #region PARAMETER GENERATION TESTS

        [Fact]
        public void Insert_Should_Generate_Positional_Parameters_Not_Named()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            var customer = new Customer
            {
                Id = 9001,
                Name = "Parameter Test",
                Email = "param@test.com",
                Age = 25,
                City = "Test City",
                IsActive = true
            };

            context.Customers.Add(customer);
            
            try
            {
                context.SaveChanges();
                
                // Check captured SQL
                interceptor.CapturedSql.Should().NotBeEmpty("SQL should be captured");
                var insertSql = interceptor.CapturedSql.First();
                
                _output.WriteLine($"Generated INSERT SQL: {insertSql}");
                
                var expectedSql = @"INSERT INTO ""CUSTOMERS_TEST_PROVIDER"" (""Age"", ""City"", ""Email"", ""Id"", ""IsActive"", ""Name"")
VALUES (?, ?, ?, ?, ?, ?)";

                insertSql.Should().Be(expectedSql, "Should generate correct INSERT SQL with positional parameters");
                
                _output.WriteLine("INSERT generates correct positional parameters");
            }
            finally
            {
                // Cleanup
                try
                {
                    interceptor.Clear();
                    context.Customers.Remove(customer);
                    context.SaveChanges();
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        [Fact]
        public void Update_Should_Maintain_Correct_Parameter_Order()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            // Insert test data first
            var customer = new Customer
            {
                Id = 9002,
                Name = "Order Test",
                Email = "order@test.com",
                Age = 30,
                City = "Original City",
                IsActive = true
            };

            context.Customers.Add(customer);
            context.SaveChanges();
            interceptor.Clear(); // Clear INSERT SQL

            // Update - SET parameters should come before WHERE parameters
            customer.Name = "Updated Order Test";
            customer.Age = 31;
            customer.City = "Updated City";

            try
            {
                context.SaveChanges();
                
                // Check captured UPDATE SQL
                interceptor.CapturedSql.Should().NotBeEmpty("UPDATE SQL should be captured");
                var updateSql = interceptor.CapturedSql.First();
                
                _output.WriteLine($"Generated UPDATE SQL: {updateSql}");
                
                var expectedSql = @"UPDATE ""CUSTOMERS_TEST_PROVIDER"" SET ""Age"" = ?, ""City"" = ?, ""Name"" = ?
WHERE ""Id"" = ?";

                updateSql.Should().Be(expectedSql, "Should generate correct UPDATE SQL with proper parameter order");
                
                _output.WriteLine("UPDATE generates correct parameter order (SET before WHERE)");
            }
            finally
            {
                // Cleanup
                try
                {
                    interceptor.Clear();
                    context.Customers.Remove(customer);
                    context.SaveChanges();
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        [Fact]
        public void Delete_Should_Generate_Simple_Where_Parameters()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            // Insert test data first
            var customer = new Customer
            {
                Id = 9003,
                Name = "Delete Test",
                Email = "delete@test.com",
                Age = 35,
                City = "Delete City",
                IsActive = false
            };

            context.Customers.Add(customer);
            context.SaveChanges();
            interceptor.Clear(); // Clear INSERT SQL

            // Delete
            context.Customers.Remove(customer);

            try
            {
                context.SaveChanges();
                
                // Check captured DELETE SQL
                interceptor.CapturedSql.Should().NotBeEmpty("DELETE SQL should be captured");
                var deleteSql = interceptor.CapturedSql.First();
                
                _output.WriteLine($"Generated DELETE SQL: {deleteSql}");
                
                var expectedSql = @"DELETE FROM ""CUSTOMERS_TEST_PROVIDER""
WHERE ""Id"" = ?";

                deleteSql.Should().Be(expectedSql, "Should generate correct DELETE SQL with WHERE parameters");
                
                _output.WriteLine("DELETE generates correct WHERE parameters");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"DELETE failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region OPENEDGE SPECIFIC SQL SYNTAX TESTS

        [Fact]
        public void Query_Should_Generate_TOP_Without_Parentheses()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            try
            {
                var topCustomers = context.Customers
                    .Take(5)
                    .ToList();

                // Check captured SELECT SQL
                interceptor.CapturedSql.Should().NotBeEmpty("SELECT SQL should be captured");
                var selectSql = interceptor.CapturedSql.First();
                
                _output.WriteLine($"Generated SELECT SQL: {selectSql}");
                
                var expectedSql = @"SELECT TOP 5 ""c"".""Id"", ""c"".""Age"", ""c"".""City"", ""c"".""Email"", ""c"".""IsActive"", ""c"".""Name""
FROM ""CUSTOMERS_TEST_PROVIDER"" AS ""c""";

                selectSql.Should().Be(expectedSql, "Should generate correct TOP SQL without parentheses");
                
                _output.WriteLine($"TOP clause generated correctly without parentheses, retrieved {topCustomers.Count} customers");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"TOP clause failed: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public void Query_Should_Handle_Boolean_Values()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            try
            {
                var activeCustomers = context.Customers
                    .Where(c => c.IsActive == true)
                    .ToList();

                // Check captured SQL for boolean handling
                interceptor.CapturedSql.Should().NotBeEmpty("SELECT SQL should be captured");
                var selectSql = interceptor.CapturedSql.First();
                
                _output.WriteLine($"Generated Boolean SQL: {selectSql}");
                
                var expectedSql = @"SELECT ""c"".""Id"", ""c"".""Age"", ""c"".""City"", ""c"".""Email"", ""c"".""IsActive"", ""c"".""Name""
FROM ""CUSTOMERS_TEST_PROVIDER"" AS ""c""
WHERE ""c"".""IsActive"" = ?";

                selectSql.Should().Be(expectedSql, "Should generate correct Boolean WHERE SQL");
                
                _output.WriteLine($"Boolean handling succeeded: {activeCustomers.Count} active customers found");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Boolean handling failed: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public void Query_Should_Handle_DateTime_Values()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();
            var testDate = new DateTime(2024, 1, 1);

            try
            {
                var recentOrders = context.Orders
                    .Where(o => o.OrderDate >= testDate)
                    .ToList();

                // Check captured SQL for DateTime handling
                interceptor.CapturedSql.Should().NotBeEmpty("SELECT SQL should be captured");
                var selectSql = interceptor.CapturedSql.First();
                
                _output.WriteLine($"Generated DateTime SQL: {selectSql}");
                
                var expectedSql = @"SELECT ""o"".""Id"", ""o"".""CustomerId"", ""o"".""OrderDate"", ""o"".""Total""
FROM ""ORDERS_TEST_PROVIDER"" AS ""o""
WHERE ""o"".""OrderDate"" >= ?";

                selectSql.Should().Be(expectedSql, "Should generate correct DateTime WHERE SQL");
                
                _output.WriteLine($"DateTime handling succeeded, found {recentOrders.Count} orders");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"DateTime handling failed: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public void Query_Should_Handle_Decimal_Values()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            try
            {
                var expensiveProducts = context.Products
                    .Where(p => p.Price > 100.00m)
                    .ToList();

                // Check captured SQL for Decimal handling
                interceptor.CapturedSql.Should().NotBeEmpty("SELECT SQL should be captured");
                var selectSql = interceptor.CapturedSql.First();
                
                _output.WriteLine($"Generated Decimal SQL: {selectSql}");
                
                var expectedSql = @"SELECT ""p"".""Id"", ""p"".""Name"", ""p"".""Price""
FROM ""PRODUCTS_TEST_PROVIDER"" AS ""p""
WHERE ""p"".""Price"" > ?";

                selectSql.Should().Be(expectedSql, "Should generate correct Decimal WHERE SQL");
                
                _output.WriteLine($"Decimal handling succeeded, found {expensiveProducts.Count} expensive products");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Decimal handling failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region STRING OPERATIONS TESTS

        [Fact]
        public void Query_Should_Generate_LIKE_For_Contains()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            try
            {
                var customersWithJohn = context.Customers
                    .Where(c => c.Name.Contains("John"))
                    .ToList();

                // Check captured SQL for LIKE translation
                interceptor.CapturedSql.Should().NotBeEmpty("SELECT SQL should be captured");
                var selectSql = interceptor.CapturedSql.First();
                
                _output.WriteLine($"Generated String.Contains SQL: {selectSql}");
                
                var expectedSql = @"SELECT ""c"".""Id"", ""c"".""Age"", ""c"".""City"", ""c"".""Email"", ""c"".""IsActive"", ""c"".""Name""
FROM ""CUSTOMERS_TEST_PROVIDER"" AS ""c""
WHERE ""c"".""Name"" LIKE ?";

                selectSql.Should().Be(expectedSql, "Should generate correct LIKE SQL for String.Contains");
                
                // Check parameters - should contain %John%
                interceptor.CapturedParameters.Should().NotBeEmpty("Parameters should be captured");

                var parameters = interceptor.CapturedParameters.First();
                var likeParam = parameters.FirstOrDefault(p => p.Value?.ToString().Contains("John") == true);
                
                likeParam.Should().NotBeNull("Should have John parameter");
                likeParam.Value.ToString().Should().Contain("%", "Should wrap with % wildcards");
                
                _output.WriteLine($"String.Contains() -> LIKE translation succeeded, found {customersWithJohn.Count} customers");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"String.Contains() translation failed: {ex.Message}");
                throw;
            }
        }

        #endregion

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
FROM ""CUSTOMERS_TEST_PROVIDER"" AS ""c""
ORDER BY ""c"".""Id""
OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

            sql.Should().Be(expectedSql, "Should generate proper OFFSET/FETCH SQL for pagination");
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

            var expectedSql = @"SELECT TOP 10 ""c"".""Id"", ""c"".""Age"", ""c"".""City"", ""c"".""Email"", ""c"".""IsActive"", ""c"".""Name""
FROM ""CUSTOMERS_TEST_PROVIDER"" AS ""c""
ORDER BY ""c"".""Id""";

            sql.Should().Be(expectedSql, "Should generate proper TOP SQL for Take");
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
FROM ""CUSTOMERS_TEST_PROVIDER"" AS ""c""
ORDER BY ""c"".""Id""
OFFSET ? ROWS";

            sql.Should().Be(expectedSql, "Should generate proper OFFSET SQL for Skip");
        }

        [Fact]
        public void Large_Skip_Take_Should_Generate_Efficient_SQL()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            var farPages = context.Customers
                .OrderBy(c => c.Id)
                .Skip(1000)
                .Take(10)
                .ToList();

            interceptor.CapturedSql.Should().NotBeEmpty("SQL should be captured for large Skip/Take");
            var sql = interceptor.CapturedSql.First();
            
            _output.WriteLine($"Skip(1000).Take(10) generated SQL: {sql}");

            var expectedSql = @"SELECT ""c"".""Id"", ""c"".""Age"", ""c"".""City"", ""c"".""Email"", ""c"".""IsActive"", ""c"".""Name""
FROM ""CUSTOMERS_TEST_PROVIDER"" AS ""c""
ORDER BY ""c"".""Id""
OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

            sql.Should().Be(expectedSql, "Should generate efficient OFFSET/FETCH SQL for large pagination");
        }

        [Fact]
        public void Different_Take_Sizes_Should_Generate_Correct_TOP_Parameters()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            var takeSizes = new[] { 1, 5, 100, 1000 };

            foreach (var size in takeSizes)
            {
                interceptor.Clear();
                
                var customers = context.Customers
                    .OrderBy(c => c.Id)
                    .Take(size)
                    .ToList();

                interceptor.CapturedSql.Should().NotBeEmpty($"SQL should be captured for Take({size})");
                var sql = interceptor.CapturedSql.First();
                
                _output.WriteLine($"Take({size}) generated SQL: {sql}");

                var expectedSql = $@"SELECT TOP {size} ""c"".""Id"", ""c"".""Age"", ""c"".""City"", ""c"".""Email"", ""c"".""IsActive"", ""c"".""Name""
FROM ""CUSTOMERS_TEST_PROVIDER"" AS ""c""
ORDER BY ""c"".""Id""";

                sql.Should().Be(expectedSql, $"Should generate correct TOP {size} SQL");
            }
        }

        [Fact]
        public void Parameterized_Skip_Take_Should_Generate_Correct_Parameters()
        {
            var (context, interceptor) = CreateContextWithSqlCapturing();

            var skipCount = 10;
            var takeCount = 20;
            
            var pagedCustomers = context.Customers
                .OrderBy(c => c.Id)
                .Skip(skipCount)
                .Take(takeCount)
                .ToList();

            interceptor.CapturedSql.Should().NotBeEmpty("SQL should be captured for parameterized Skip/Take");
            var sql = interceptor.CapturedSql.First();
            
            _output.WriteLine($"Parameterized Skip({skipCount}).Take({takeCount}) generated SQL: {sql}");

            var expectedSql = @"SELECT ""c"".""Id"", ""c"".""Age"", ""c"".""City"", ""c"".""Email"", ""c"".""IsActive"", ""c"".""Name""
FROM ""CUSTOMERS_TEST_PROVIDER"" AS ""c""
ORDER BY ""c"".""Id""
OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

            sql.Should().Be(expectedSql, "Should generate correct parameterized OFFSET/FETCH SQL");

            // Verify parameters
            var parameters = interceptor.CapturedParameters.FirstOrDefault();
            parameters.Should().NotBeNull("Parameters should be captured");
            parameters.Should().HaveCount(2, "Should have skip and take parameters");
            parameters[0].Value.Should().Be(skipCount, "First parameter should be skip count");
            parameters[1].Value.Should().Be(takeCount, "Second parameter should be take count");
        }

        #endregion
    }
}
