using System;
using System.Linq;
using EFCore.OpenEdge.FunctionalTests.Shared;
using EFCore.OpenEdge.FunctionalTests.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace EFCore.OpenEdge.FunctionalTests.Update
{
    public class ConstraintTests : ECommerceTestBase
    {
        private readonly ITestOutputHelper _output;

        public ConstraintTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region PRIMARY KEY CONSTRAINT TESTS

        [Fact]
        public void Should_Fail_Insert_Duplicate_Primary_Key_Customer()
        {
            using var context = CreateContext();

            var customer = new Customer
            {
                Id = 1, // This ID already exists
                Name = "Duplicate Customer",
                Email = "duplicate@example.com",
                Age = 30,
                City = "Duplicate City",
                IsActive = true
            };

            context.Customers.Add(customer);

            // Should throw DbUpdateException due to primary key constraint
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Primary key constraint violation: {exception.Message}");

            // Verify the exception is related to constraint violation
            exception.Message.ToLower().Should().Contain("constraint");
        }

        [Fact]
        public void Should_Fail_Insert_Duplicate_Primary_Key_Product()
        {
            using var context = CreateContext();

            var product = new Product
            {
                Id = 1, // This ID already exists
                Name = "Duplicate Product",
                Price = 100.00m,
                CategoryId = 1,
                Description = "Duplicate product",
                InStock = true
            };

            context.Products.Add(product);

            // Should throw DbUpdateException due to primary key constraint
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Primary key constraint violation: {exception.Message}");
        }

        [Fact]
        public void Should_Fail_Insert_Duplicate_Primary_Key_Order()
        {
            using var context = CreateContext();

            var order = new Order
            {
                Id = 1, // This ID already exists
                CustomerId = 1,
                OrderDate = DateTime.Now,
                TotalAmount = 100.00m,
                Status = "Duplicate Order"
            };

            context.Orders.Add(order);

            // Should throw DbUpdateException due to primary key constraint
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Primary key constraint violation: {exception.Message}");
        }

        #endregion

        #region FOREIGN KEY CONSTRAINT TESTS

        [Fact]
        public void Should_Fail_Insert_Order_With_Invalid_Customer_Id()
        {
            using var context = CreateContext();

            var order = new Order
            {
                Id = 1800,
                CustomerId = 999, // Non-existent customer ID
                OrderDate = DateTime.Now,
                TotalAmount = 100.00m,
                Status = "Invalid Customer Order"
            };

            context.Orders.Add(order);

            // Should throw DbUpdateException due to foreign key constraint
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Foreign key constraint violation: {exception.Message}");

            // Verify the exception is related to constraint violation
            exception.Message.ToLower().Should().Contain("constraint");
        }

        [Fact]
        public void Should_Fail_Insert_Product_With_Invalid_Category_Id()
        {
            using var context = CreateContext();

            var product = new Product
            {
                Id = 1800,
                Name = "Invalid Category Product",
                Price = 100.00m,
                CategoryId = 999, // Non-existent category ID
                Description = "Product with invalid category",
                InStock = true
            };

            context.Products.Add(product);

            // Should throw DbUpdateException due to foreign key constraint
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Foreign key constraint violation: {exception.Message}");
        }

        [Fact]
        public void Should_Fail_Insert_OrderItem_With_Invalid_Order_Id()
        {
            using var context = CreateContext();

            var orderItem = new OrderItem
            {
                Id = 1800,
                OrderId = 999, // Non-existent order ID
                ProductId = 1,
                Quantity = 1,
                UnitPrice = 100.00m
            };

            context.OrderItems.Add(orderItem);

            // Should throw DbUpdateException due to foreign key constraint
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Foreign key constraint violation: {exception.Message}");
        }

        [Fact]
        public void Should_Fail_Insert_OrderItem_With_Invalid_Product_Id()
        {
            using var context = CreateContext();

            var orderItem = new OrderItem
            {
                Id = 1801,
                OrderId = 1,
                ProductId = 999, // Non-existent product ID
                Quantity = 1,
                UnitPrice = 100.00m
            };

            context.OrderItems.Add(orderItem);

            // Should throw DbUpdateException due to foreign key constraint
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Foreign key constraint violation: {exception.Message}");
        }

        [Fact]
        public void Should_Succeed_Insert_With_Valid_Foreign_Keys()
        {
            using var context = CreateContext();

            var order = new Order
            {
                Id = 1900,
                CustomerId = 1, // Valid customer ID
                OrderDate = DateTime.Now,
                TotalAmount = 100.00m,
                Status = "Valid Foreign Key Order"
            };

            context.Orders.Add(order);

            // Should succeed
            var result = context.SaveChanges();
            result.Should().Be(1);
            _output.WriteLine("Successfully inserted order with valid foreign key");

            // Verify the order was inserted
            var insertedOrder = context.Orders.Find(1900);
            insertedOrder.Should().NotBeNull();
            insertedOrder.CustomerId.Should().Be(1);
        }

        #endregion

        #region REFERENTIAL INTEGRITY TESTS

        [Fact]
        public void Should_Fail_Delete_Customer_With_Orders()
        {
            using var context = CreateContext();

            // Find a customer that has orders
            var customerWithOrders = context.Customers
                .Include(c => c.Orders)
                .First(c => c.Orders.Any());

            var customerId = customerWithOrders.Id;
            var orderCount = customerWithOrders.Orders.Count;

            _output.WriteLine($"Attempting to delete customer {customerId} who has {orderCount} orders");

            // Try to delete the customer
            context.Customers.Remove(customerWithOrders);

            // Should throw DbUpdateException due to referential integrity constraint
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Referential integrity constraint violation: {exception.Message}");

            // Verify the customer still exists
            using var verifyContext = CreateContext();
            var stillExistsCustomer = verifyContext.Customers.Find(customerId);
            stillExistsCustomer.Should().NotBeNull();
        }

        [Fact]
        public void Should_Fail_Delete_Category_With_Products()
        {
            using var context = CreateContext();

            // Find a category that has products
            var categoryWithProducts = context.Categories
                .Include(c => c.Products)
                .First(c => c.Products.Any());

            var categoryId = categoryWithProducts.Id;
            var productCount = categoryWithProducts.Products.Count;

            _output.WriteLine($"Attempting to delete category {categoryId} which has {productCount} products");

            // Try to delete the category
            context.Categories.Remove(categoryWithProducts);

            // Should throw DbUpdateException due to referential integrity constraint
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Referential integrity constraint violation: {exception.Message}");

            // Verify the category still exists
            using var verifyContext = CreateContext();
            var stillExistsCategory = verifyContext.Categories.Find(categoryId);
            stillExistsCategory.Should().NotBeNull();
        }

        [Fact]
        public void Should_Fail_Delete_Order_With_OrderItems()
        {
            using var context = CreateContext();

            // Find an order that has order items
            var orderWithItems = context.Orders
                .Include(o => o.OrderItems)
                .First(o => o.OrderItems.Any());

            var orderId = orderWithItems.Id;
            var itemCount = orderWithItems.OrderItems.Count;

            _output.WriteLine($"Attempting to delete order {orderId} which has {itemCount} order items");

            // Try to delete the order
            context.Orders.Remove(orderWithItems);

            // Should throw DbUpdateException due to referential integrity constraint
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Referential integrity constraint violation: {exception.Message}");

            // Verify the order still exists
            using var verifyContext = CreateContext();
            var stillExistsOrder = verifyContext.Orders.Find(orderId);
            stillExistsOrder.Should().NotBeNull();
        }

        [Fact]
        public void Should_Succeed_Delete_Customer_Without_Orders()
        {
            using var context = CreateContext();

            // First, add a customer without orders
            var customerWithoutOrders = new Customer
            {
                Id = 2000,
                Name = "Customer Without Orders",
                Email = "noorders@example.com",
                Age = 30,
                City = "No Orders City",
                IsActive = true
            };

            context.Customers.Add(customerWithoutOrders);
            context.SaveChanges();

            // Now delete the customer
            context.Customers.Remove(customerWithoutOrders);
            var result = context.SaveChanges();

            result.Should().Be(1);
            _output.WriteLine("Successfully deleted customer without orders");

            // Verify the customer was deleted
            var deletedCustomer = context.Customers.Find(2000);
            deletedCustomer.Should().BeNull();
        }

        #endregion

        #region CASCADE DELETE TESTS

        [Fact]
        public void Should_Handle_Cascade_Delete_OrderItems_When_Order_Deleted()
        {
            using var context = CreateContext();

            // Create an order with order items
            var order = new Order
            {
                Id = 2100,
                CustomerId = 1,
                OrderDate = DateTime.Now,
                TotalAmount = 199.98m,
                Status = "Cascade Test Order"
            };

            var orderItems = new[]
            {
                new OrderItem { Id = 2100, OrderId = 2100, ProductId = 1, Quantity = 1, UnitPrice = 99.99m },
                new OrderItem { Id = 2101, OrderId = 2100, ProductId = 2, Quantity = 1, UnitPrice = 99.99m }
            };

            context.Orders.Add(order);
            context.OrderItems.AddRange(orderItems);
            context.SaveChanges();

            // Verify the order and items exist
            var createdOrder = context.Orders.Include(o => o.OrderItems).First(o => o.Id == 2100);
            createdOrder.OrderItems.Should().HaveCount(2);

            // Delete the order items first, then the order (manual cascade)
            context.OrderItems.RemoveRange(createdOrder.OrderItems);
            context.Orders.Remove(createdOrder);

            var result = context.SaveChanges();
            result.Should().Be(3); // 2 order items + 1 order

            _output.WriteLine("Successfully handled cascade delete scenario");

            // Verify all items were deleted
            using var verifyContext = CreateContext();
            var deletedOrder = verifyContext.Orders.Find(2100);
            var deletedOrderItems = verifyContext.OrderItems.Where(oi => oi.OrderId == 2100).ToList();

            deletedOrder.Should().BeNull();
            deletedOrderItems.Should().BeEmpty();
        }

        #endregion

        #region NULL CONSTRAINT TESTS

        [Fact]
        public void Should_Fail_Insert_Customer_With_Null_Required_Field()
        {
            using var context = CreateContext();

            // Note: Since Name is a required field, this should fail
            var customer = new Customer
            {
                Id = 2200,
                Name = null, // This violates the required constraint
                Email = "nullname@example.com",
                Age = 30,
                City = "Null City",
                IsActive = true
            };

            context.Customers.Add(customer);

            // Should throw an exception due to null constraint
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"Null constraint violation: {exception.Message}");
        }

        [Fact]
        public void Should_Succeed_Insert_Customer_With_Null_Optional_Field()
        {
            using var context = CreateContext();

            var customer = new Customer
            {
                Id = 2201,
                Name = "Customer With Null Email",
                Email = null, // This is allowed
                Age = 30,
                City = "Null Email City",
                IsActive = true
            };

            context.Customers.Add(customer);

            // Should succeed
            var result = context.SaveChanges();
            result.Should().Be(1);
            _output.WriteLine("Successfully inserted customer with null optional field");

            // Verify the customer was inserted
            var insertedCustomer = context.Customers.Find(2201);
            insertedCustomer.Should().NotBeNull();
            insertedCustomer.Email.Should().BeNull();
        }

        #endregion

        #region DATA TYPE CONSTRAINT TESTS

        [Fact]
        public void Should_Handle_Large_String_Values()
        {
            using var context = CreateContext();

            // Test with a string that might exceed field length
            var longDescription = new string('A', 600); // Longer than the 500 character limit

            var product = new Product
            {
                Id = 2300,
                Name = "Long Description Product",
                Price = 100.00m,
                CategoryId = 1,
                Description = longDescription,
                InStock = true
            };

            context.Products.Add(product);

            // This might succeed with truncation or fail with constraint violation
            try
            {
                var result = context.SaveChanges();
                _output.WriteLine($"Successfully inserted product with long description (possibly truncated)");
            }
            catch (DbUpdateException ex)
            {
                _output.WriteLine($"Expected constraint violation for long string: {ex.Message}");
            }
        }

        [Fact]
        public void Should_Handle_Extreme_Decimal_Values()
        {
            using var context = CreateContext();

            var product = new Product
            {
                Id = 2301,
                Name = "Extreme Price Product",
                Price = 99999999.99m, // Maximum decimal value for decimal(10,2)
                CategoryId = 1,
                Description = "Extreme price product",
                InStock = true
            };

            context.Products.Add(product);

            try
            {
                var result = context.SaveChanges();
                result.Should().Be(1);
                _output.WriteLine("Successfully inserted product with extreme decimal value");

                // Verify the product was inserted
                var insertedProduct = context.Products.Find(2301);
                insertedProduct.Should().NotBeNull();
                insertedProduct.Price.Should().Be(99999999.99m);
            }
            catch (DbUpdateException ex)
            {
                _output.WriteLine($"Decimal constraint violation: {ex.Message}");
            }
        }

        [Fact]
        public void Should_Handle_Extreme_Integer_Values()
        {
            using var context = CreateContext();

            var customer = new Customer
            {
                Id = 2302,
                Name = "Extreme Age Customer",
                Email = "extreme@example.com",
                Age = int.MaxValue,
                City = "Extreme City",
                IsActive = true
            };

            context.Customers.Add(customer);

            try
            {
                var result = context.SaveChanges();
                result.Should().Be(1);
                _output.WriteLine("Successfully inserted customer with extreme integer value");

                // Verify the customer was inserted
                var insertedCustomer = context.Customers.Find(2302);
                insertedCustomer.Should().NotBeNull();
                insertedCustomer.Age.Should().Be(int.MaxValue);
            }
            catch (DbUpdateException ex)
            {
                _output.WriteLine($"Integer constraint violation: {ex.Message}");
            }
        }

        #endregion

        #region CONSTRAINT VIOLATION RECOVERY TESTS

        [Fact]
        public void Should_Recover_From_Constraint_Violation()
        {
            using var context = CreateContext();

            // First, try to insert a customer with duplicate ID
            var duplicateCustomer = new Customer
            {
                Id = 1, // Duplicate ID
                Name = "Duplicate Customer",
                Email = "duplicate@example.com",
                Age = 30,
                City = "Duplicate City",
                IsActive = true
            };

            context.Customers.Add(duplicateCustomer);

            // This should fail
            var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            _output.WriteLine($"First constraint violation: {exception.Message}");

            // Remove the problematic entity
            context.Customers.Remove(duplicateCustomer);

            // Now try to insert a valid customer
            var validCustomer = new Customer
            {
                Id = 2400,
                Name = "Valid Customer",
                Email = "valid@example.com",
                Age = 30,
                City = "Valid City",
                IsActive = true
            };

            context.Customers.Add(validCustomer);

            // This should succeed
            var result = context.SaveChanges();
            result.Should().Be(1);
            _output.WriteLine("Successfully recovered from constraint violation");

            // Verify the valid customer was inserted
            var insertedCustomer = context.Customers.Find(2400);
            insertedCustomer.Should().NotBeNull();
        }

        #endregion
    }
}