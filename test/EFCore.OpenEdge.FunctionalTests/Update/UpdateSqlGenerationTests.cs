// using System;
// using System.Linq;
// using EFCore.OpenEdge.FunctionalTests.Shared;
// using EFCore.OpenEdge.FunctionalTests.Shared.Models;
// using EFCore.OpenEdge.FunctionalTests.TestUtilities;
// using FluentAssertions;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Xunit;
// using Xunit.Abstractions;

// namespace EFCore.OpenEdge.FunctionalTests.Update
// {
//     public class UpdateSqlGenerationTests : ECommerceTestBase
//     {
//         private readonly ITestOutputHelper _output;
//         private readonly ILoggerFactory _loggerFactory;

//         public UpdateSqlGenerationTests(ITestOutputHelper output)
//         {
//             _output = output;
//             _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
//         }

//         private (ECommerceTestContext context, SqlCapturingInterceptor interceptor) CreateContextWithSqlCapture()
//         {
//             var sqlCapturingInterceptor = new SqlCapturingInterceptor();
//             var options = new DbContextOptionsBuilder<ECommerceTestContext>()
//                 .UseOpenEdge(ConnectionString)
//                 .EnableSensitiveDataLogging()
//                 .UseLoggerFactory(_loggerFactory)
//                 .AddInterceptors(sqlCapturingInterceptor)
//                 .Options;

//             var context = new ECommerceTestContext(options);
//             return (context, sqlCapturingInterceptor);
//         }

//         private string GetCapturedSql(SqlCapturingInterceptor interceptor)
//         {
//             return interceptor == null ? string.Empty : string.Join(Environment.NewLine, interceptor.CapturedSql);
//         }

//         #region INSERT SQL GENERATION TESTS

//         [Fact]
//         public void Should_Generate_Correct_Insert_SQL_For_Customer()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {
//                 var customer = new Customer
//                 {
//                     Id = 1400,
//                     Name = "SQL Test Customer",
//                     Email = "sqltest@example.com",
//                     Age = 30,
//                     City = "SQL Test City",
//                     IsActive = true
//                 };

//                 context.Customers.Add(customer);
//                 context.SaveChanges();

//                 var sql = GetCapturedSql(interceptor);
//                 _output.WriteLine($"Generated SQL: {sql}");

//                 // Verify INSERT statement structure
//                 sql.Should().Contain("INSERT INTO");
//                 sql.Should().Contain("PUB.CUSTOMERS_TEST_PROVIDER");
//                 sql.Should().Contain("VALUES");
            
//                 // Verify positional parameters (OpenEdge uses ? not named parameters)
//                 sql.Should().Contain("?");
//                 sql.Should().NotContain("@");
                
//                 // Verify column names are present
//                 sql.Should().Contain("Id");
//                 sql.Should().Contain("Name");
//                 sql.Should().Contain("Email");
//                 sql.Should().Contain("Age");
//                 sql.Should().Contain("City");
//                 sql.Should().Contain("IsActive");
//             }
//         }

//         [Fact]
//         public void Should_Generate_Correct_Insert_SQL_For_Product()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {
//                 var product = new Product
//                 {
//                     Id = 1400,
//                     Name = "SQL Test Product",
//                     Price = 199.99m,
//                     CategoryId = 1,
//                     Description = "SQL test product description",
//                     InStock = true
//                 };

//                 context.Products.Add(product);
//                 context.SaveChanges();

//                 var sql = GetCapturedSql(interceptor);
//                 _output.WriteLine($"Generated SQL: {sql}");

//                 // Verify INSERT statement structure
//                 sql.Should().Contain("INSERT INTO");
//                 sql.Should().Contain("PUB.PRODUCTS_TEST_PROVIDER");
//                 sql.Should().Contain("VALUES");
                
//                 // Verify positional parameters
//                 sql.Should().Contain("?");
//                 sql.Should().NotContain("@");
                
//                 // Verify decimal handling
//                 sql.Should().Contain("Price");
                
//                 // Verify foreign key column
//                 sql.Should().Contain("CategoryId");
//             }
//         }

//         [Fact]
//         public void Should_Generate_Correct_Insert_SQL_For_Multiple_Records()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {
//                 var customers = new[]
//                 {
//                     new Customer { Id = 1401, Name = "Bulk Customer 1", Email = "bulk1@example.com", Age = 25, City = "Bulk City", IsActive = true },
//                     new Customer { Id = 1402, Name = "Bulk Customer 2", Email = "bulk2@example.com", Age = 30, City = "Bulk City", IsActive = false }
//                 };

//                 context.Customers.AddRange(customers);
//                 context.SaveChanges();

//                 var sql = GetCapturedSql(interceptor);
//                 _output.WriteLine($"Generated SQL: {sql}");

//                 // Verify multiple INSERT statements are generated (OpenEdge doesn't support bulk INSERT)
//                 var insertCount = sql.Split(new[] { "INSERT INTO" }, StringSplitOptions.None).Length - 1;
//                 insertCount.Should().Be(2);
//             }
//         }

//         #endregion

//         #region UPDATE SQL GENERATION TESTS

//         [Fact]
//         public void Should_Generate_Correct_Update_SQL_For_Customer()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {
//                 // First, ensure we have a customer to update
//                 var customer = context.Customers.First(c => c.Id == 1);
//                 customer.Name = "Updated SQL Customer";
//                 customer.Age = 31;
//                 customer.City = "Updated SQL City";

//                 context.SaveChanges();

//                 var sql = GetCapturedSql(interceptor);
//                 _output.WriteLine($"Generated SQL: {sql}");

//                 // Verify UPDATE statement structure
//                 sql.Should().Contain("UPDATE");
//                 sql.Should().Contain("PUB.CUSTOMERS_TEST_PROVIDER");
//                 sql.Should().Contain("SET");
//                 sql.Should().Contain("WHERE");
                
//                 // Verify positional parameters
//                 sql.Should().Contain("?");
//                 sql.Should().NotContain("@");
                
//                 // Verify updated columns
//                 sql.Should().Contain("Name");
//                 sql.Should().Contain("Age");
//                 sql.Should().Contain("City");
                
//                 // Verify WHERE clause with primary key
//                 sql.Should().Contain("Id");
//             }
//         }

//         [Fact]
//         public void Should_Generate_Correct_Update_SQL_For_Product_Price()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {
//                 var product = context.Products.First(p => p.Id == 1);
//                 product.Price = 1199.99m;
//                 product.Description = "Updated product description";

//                 context.SaveChanges();

//                 var sql = GetCapturedSql(interceptor);
//                 _output.WriteLine($"Generated SQL: {sql}");

//                 // Verify UPDATE statement structure
//                 sql.Should().Contain("UPDATE");
//                 sql.Should().Contain("PUB.PRODUCTS_TEST_PROVIDER");
//                 sql.Should().Contain("SET");
//                 sql.Should().Contain("WHERE");
                
//                 // Verify decimal handling in UPDATE
//                 sql.Should().Contain("Price");
                
//                 // Verify string field update
//                 sql.Should().Contain("Description");
//             }
//         }

//         [Fact]
//         public void Should_Generate_Correct_Update_SQL_For_Boolean_Fields()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {
//                 var customer = context.Customers.First(c => c.Id == 1);
//                 customer.IsActive = false;

//                 context.SaveChanges();

//                 var sql = GetCapturedSql(interceptor);
//                 _output.WriteLine($"Generated SQL: {sql}");

//                 // Verify boolean field handling
//                 sql.Should().Contain("IsActive");
                
//                 // Verify OpenEdge boolean syntax (BIT type)
//                 sql.Should().Contain("UPDATE");
//                 sql.Should().Contain("SET");
//             }
//         }

//         [Fact]
//         public void Should_Generate_Correct_Update_SQL_For_Multiple_Records()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {
//                 var customers = context.Customers.Where(c => c.City == "New York").ToList();
//                 foreach (var customer in customers)
//                 {
//                     customer.City = "Updated New York";
//                 }

//                 context.SaveChanges();

//                 var sql = GetCapturedSql(interceptor);
//                 _output.WriteLine($"Generated SQL: {sql}");

//                 // Verify multiple UPDATE statements are generated
//                 var updateCount = sql.Split(new[] { "UPDATE" }, StringSplitOptions.None).Length - 1;
//                 updateCount.Should().Be(customers.Count);
//             }
//         }

//         #endregion

//         #region DELETE SQL GENERATION TESTS

//         [Fact]
//         public void Should_Generate_Correct_Delete_SQL_For_Customer()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {
//                 // First, add a customer to delete
//                 var customer = new Customer
//                 {
//                     Id = 1500,
//                     Name = "Delete SQL Customer",
//                     Email = "deletesql@example.com",
//                     Age = 25,
//                     City = "Delete SQL City",
//                     IsActive = true
//                 };

//                 context.Customers.Add(customer);
//                 context.SaveChanges();

//                 // Clear previous SQL
//                 interceptor?.Clear();

//                 // Now delete the customer
//                 context.Customers.Remove(customer);
//                 context.SaveChanges();

//                 var sql = GetCapturedSql(interceptor);
//                 _output.WriteLine($"Generated SQL: {sql}");

//                 // Verify DELETE statement structure
//                 sql.Should().Contain("DELETE FROM");
//                 sql.Should().Contain("PUB.CUSTOMERS_TEST_PROVIDER");
//                 sql.Should().Contain("WHERE");
                
//                 // Verify positional parameters
//                 sql.Should().Contain("?");
//                 sql.Should().NotContain("@");
                
//                 // Verify WHERE clause with primary key
//                 sql.Should().Contain("Id");
//             }
//         }

//         [Fact]
//         public void Should_Generate_Correct_Delete_SQL_For_Multiple_Records()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {
//                 // First, add customers to delete
//                 var customers = new[]
//                 {
//                     new Customer { Id = 1501, Name = "Delete Customer 1", Email = "delete1@example.com", Age = 25, City = "Delete City", IsActive = true },
//                     new Customer { Id = 1502, Name = "Delete Customer 2", Email = "delete2@example.com", Age = 30, City = "Delete City", IsActive = true }
//                 };

//                 context.Customers.AddRange(customers);
//                 context.SaveChanges();

//                 // Clear previous SQL
//                 interceptor?.Clear();

//                 // Now delete the customers
//                 context.Customers.RemoveRange(customers);
//                 context.SaveChanges();

//                 var sql = GetCapturedSql(interceptor);
//                 _output.WriteLine($"Generated SQL: {sql}");

//                 // Verify multiple DELETE statements are generated
//                 var deleteCount = sql.Split(new[] { "DELETE FROM" }, StringSplitOptions.None).Length - 1;
//                 deleteCount.Should().Be(2);
//             }
//         }

//         #endregion

//         #region COMPLEX SQL GENERATION TESTS

//         [Fact]
//         public void Should_Generate_Correct_SQL_For_Mixed_Operations()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {

//             // Insert a new customer
//             var newCustomer = new Customer
//             {
//                 Id = 1600,
//                 Name = "Mixed Operations Customer",
//                 Email = "mixed@example.com",
//                 Age = 35,
//                 City = "Mixed City",
//                 IsActive = true
//             };

//             context.Customers.Add(newCustomer);

//             // Update an existing customer
//             var existingCustomer = context.Customers.First(c => c.Id == 1);
//             existingCustomer.Age = 46;

//             // Add a product
//             var product = new Product
//             {
//                 Id = 1600,
//                 Name = "Mixed Operations Product",
//                 Price = 250.00m,
//                 CategoryId = 1,
//                 Description = "Mixed operations product",
//                 InStock = true
//             };

//             context.Products.Add(product);

//             context.SaveChanges();

//             var sql = GetCapturedSql(interceptor);
//             _output.WriteLine($"Generated SQL: {sql}");

//             // Verify all three operations are present
//             sql.Should().Contain("INSERT INTO");
//             sql.Should().Contain("UPDATE");
            
//             // Verify both table references
//             sql.Should().Contain("PUB.CUSTOMERS_TEST_PROVIDER");
//             sql.Should().Contain("PUB.PRODUCTS_TEST_PROVIDER");
//             }
//         }

//         [Fact]
//         public void Should_Generate_Correct_SQL_For_Foreign_Key_Operations()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {

//             // Create an order with order items
//             var order = new Order
//             {
//                 Id = 1600,
//                 CustomerId = 1,
//                 OrderDate = DateTime.Now,
//                 TotalAmount = 199.98m,
//                 Status = "Test Order"
//             };

//             var orderItem = new OrderItem
//             {
//                 Id = 1600,
//                 OrderId = 1600,
//                 ProductId = 1,
//                 Quantity = 2,
//                 UnitPrice = 99.99m
//             };

//             context.Orders.Add(order);
//             context.OrderItems.Add(orderItem);
//             context.SaveChanges();

//             var sql = GetCapturedSql(interceptor);
//             _output.WriteLine($"Generated SQL: {sql}");

//             // Verify foreign key columns are included
//             sql.Should().Contain("CustomerId");
//             sql.Should().Contain("OrderId");
//             sql.Should().Contain("ProductId");
            
//             // Verify both tables are referenced
//             sql.Should().Contain("PUB.ORDERS_TEST_PROVIDER");
//             sql.Should().Contain("PUB.ORDER_ITEMS_TEST_PROVIDER");
//             }
//         }

//         #endregion

//         #region OPENEDGE SPECIFIC SQL TESTS

//         [Fact]
//         public void Should_Use_Positional_Parameters_Not_Named_Parameters()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {

//             var customer = new Customer
//             {
//                 Id = 1700,
//                 Name = "Parameter Test Customer",
//                 Email = "paramtest@example.com",
//                 Age = 30,
//                 City = "Parameter City",
//                 IsActive = true
//             };

//             context.Customers.Add(customer);
//             context.SaveChanges();

//             var sql = GetCapturedSql(interceptor);
//             _output.WriteLine($"Generated SQL: {sql}");

//             // Verify OpenEdge uses positional parameters
//             sql.Should().Contain("?");
            
//             // Verify no named parameters are used
//             sql.Should().NotContain("@");
//             sql.Should().NotContain(":");
//             }
//         }

//         [Fact]
//         public void Should_Generate_Correct_Date_Handling()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {

//             var order = new Order
//             {
//                 Id = 1700,
//                 CustomerId = 1,
//                 OrderDate = new DateTime(2024, 1, 15),
//                 TotalAmount = 100.00m,
//                 Status = "Date Test"
//             };

//             context.Orders.Add(order);
//             context.SaveChanges();

//             var sql = GetCapturedSql(interceptor);
//             _output.WriteLine($"Generated SQL: {sql}");

//             // Verify date column is included
//             sql.Should().Contain("OrderDate");
            
//             // Verify basic INSERT structure
//             sql.Should().Contain("INSERT INTO");
//             sql.Should().Contain("PUB.ORDERS_TEST_PROVIDER");
//             }
//         }

//         [Fact]
//         public void Should_Generate_Correct_Decimal_Handling()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {

//             var product = new Product
//             {
//                 Id = 1700,
//                 Name = "Decimal Test Product",
//                 Price = 123.45m,
//                 CategoryId = 1,
//                 Description = "Decimal test",
//                 InStock = true
//             };

//             context.Products.Add(product);
//             context.SaveChanges();

//             var sql = GetCapturedSql(interceptor);
//             _output.WriteLine($"Generated SQL: {sql}");

//             // Verify decimal column is included
//             sql.Should().Contain("Price");
            
//             // Verify proper table reference
//             sql.Should().Contain("PUB.PRODUCTS_TEST_PROVIDER");
//             }
//         }

//         #endregion

//         #region ERROR HANDLING TESTS

//         [Fact]
//         public void Should_Handle_SQL_Generation_For_Constraint_Violations()
//         {
//             var (context, interceptor) = CreateContextWithSqlCapture();
//             using (context)
//             {

//             var customer = new Customer
//             {
//                 Id = 1, // Duplicate ID
//                 Name = "Constraint Test Customer",
//                 Email = "constraint@example.com",
//                 Age = 30,
//                 City = "Constraint City",
//                 IsActive = true
//             };

//             context.Customers.Add(customer);

//             // This should generate SQL but fail on execution
//             var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            
//             var sql = GetCapturedSql(interceptor);
//             _output.WriteLine($"Generated SQL before constraint violation: {sql}");

//             // Verify SQL was generated correctly even though execution failed
//             sql.Should().Contain("INSERT INTO");
//             sql.Should().Contain("PUB.CUSTOMERS_TEST_PROVIDER");
            
//             _output.WriteLine($"Expected constraint violation: {exception.Message}");
//             }
//         }

//         #endregion

//         public override void Dispose()
//         {
//             _loggerFactory?.Dispose();
//             base.Dispose();
//         }
//     }
// }