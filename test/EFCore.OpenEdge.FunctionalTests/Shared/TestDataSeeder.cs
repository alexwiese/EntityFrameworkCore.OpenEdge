using System;
using System.Data.Odbc;

namespace EFCore.OpenEdge.FunctionalTests.Shared
{
    public static class TestDataSeeder
    {
        private static bool _databaseInitialized = false;
        private static readonly object _lock = new();

        public static void EnsureSeeded(string connectionString)
        {
            lock (_lock)
            {
                if (!_databaseInitialized)
                {
                    SetupDatabase(connectionString);
                    _databaseInitialized = true;
                }
            }
        }
        
        // Helper method to check if table exists
        private static bool TableExists(string tableName, OdbcConnection connection, OdbcTransaction transaction)
        {
            var checkQuery = @"SELECT COUNT(*) FROM sysprogress.SYSTABLES 
            WHERE TBL = ?";

            using var checkCmd = new OdbcCommand(checkQuery, connection, transaction);
            checkCmd.Parameters.Add(new OdbcParameter("tableName", tableName));
            var result = checkCmd.ExecuteScalar();

            return Convert.ToInt64(result) > 0;
        }

        private static void SetupDatabase(string connectionString)
        {
            using var connection = new OdbcConnection(connectionString);
            connection.Open();

            // Start an explicit transaction to ensure proper lock management
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                // STEP 1: Create all tables if they don't exist (parent tables first)
                CreateTablesIfNotExists();

                // STEP 2: Clear existing data in reverse dependency order (child tables first). 
                // Simply dropping tables in OpenEdge requires acquiring a lock on the entire database schema, which essentially means that there can be no other active connections.
                // Therefore the table contents are cleared out instead.
                ClearExistingData();

                // STEP 3: Insert fresh test data (parent tables first)
                InsertTestData();

                // Commit the transaction to release all locks
                transaction.Commit();
            }
            catch
            {
                // Rollback the transaction if any error occurs
                transaction.Rollback();
                throw;
            }

            void CreateTablesIfNotExists()
            {
                // Create Categories table first (referenced by Products)
                if (!TableExists("CATEGORIES_TEST_PROVIDER", connection, transaction))
                {
                    var createCategoriesTable = @"
                        CREATE TABLE PUB.CATEGORIES_TEST_PROVIDER (
                            Id INT NOT NULL PRIMARY KEY,
                            Name CHARACTER(100) NOT NULL,
                            Description CHARACTER(500)
                        )";

                    using var createCategories = new OdbcCommand(createCategoriesTable, connection, transaction);
                    createCategories.ExecuteNonQuery();
                }

                // Create Customers table
                if (!TableExists("CUSTOMERS_TEST_PROVIDER", connection, transaction))
                {
                    var createCustomersTable = @"
                        CREATE TABLE PUB.CUSTOMERS_TEST_PROVIDER (
                            Id INT NOT NULL PRIMARY KEY,
                            Name CHARACTER(100) NOT NULL,
                            Email CHARACTER(100),
                            Age INT NOT NULL,
                            City CHARACTER(50),
                            IsActive BIT NOT NULL
                        )";

                    using var createCustomers = new OdbcCommand(createCustomersTable, connection, transaction);
                    createCustomers.ExecuteNonQuery();
                }

                // Create Products table (references Categories)
                if (!TableExists("PRODUCTS_TEST_PROVIDER", connection, transaction))
                {
                    var createProductsTable = @"
                        CREATE TABLE PUB.PRODUCTS_TEST_PROVIDER (
                            Id INT NOT NULL PRIMARY KEY,
                            Name CHARACTER(100) NOT NULL,
                            Price DECIMAL(10,2) NOT NULL,
                            CategoryId INT NOT NULL REFERENCES PUB.CATEGORIES_TEST_PROVIDER,
                            Description CHARACTER(500),
                            InStock BIT NOT NULL
                        )";

                    using var createProducts = new OdbcCommand(createProductsTable, connection, transaction);
                    createProducts.ExecuteNonQuery();
                }

                // Create Orders table (references Customers)
                if (!TableExists("ORDERS_TEST_PROVIDER", connection, transaction))
                {
                    var createOrdersTable = @"
                        CREATE TABLE PUB.ORDERS_TEST_PROVIDER (
                            Id INT NOT NULL PRIMARY KEY,
                            CustomerId INT NOT NULL REFERENCES PUB.CUSTOMERS_TEST_PROVIDER,
                            OrderDate DATE NOT NULL,
                            TotalAmount DECIMAL(10,2) NOT NULL,
                            Status CHARACTER(50)
                        )";

                    using var createOrders = new OdbcCommand(createOrdersTable, connection, transaction);
                    createOrders.ExecuteNonQuery();
                }

                // Create OrderItems table (references Orders and Products)
                if (!TableExists("ORDER_ITEMS_TEST_PROVIDER", connection, transaction))
                {
                    var createOrderItemsTable = @"
                        CREATE TABLE PUB.ORDER_ITEMS_TEST_PROVIDER (
                            Id INT NOT NULL PRIMARY KEY,
                            OrderId INT NOT NULL REFERENCES PUB.ORDERS_TEST_PROVIDER,
                            ProductId INT NOT NULL REFERENCES PUB.PRODUCTS_TEST_PROVIDER,
                            Quantity INT NOT NULL,
                            UnitPrice DECIMAL(10,2) NOT NULL
                        )";

                    using var createOrderItems = new OdbcCommand(createOrderItemsTable, connection, transaction);
                    createOrderItems.ExecuteNonQuery();
                }
            }

            void ClearExistingData()
            {
                // Delete in reverse dependency order (child tables first to avoid foreign key violations)
                // Delete OrderItems first (references Orders and Products)
                if (TableExists("ORDER_ITEMS_TEST_PROVIDER", connection, transaction))
                {
                    var deleteOrderItems = "DELETE FROM PUB.ORDER_ITEMS_TEST_PROVIDER";
                    using var deleteCmd = new OdbcCommand(deleteOrderItems, connection, transaction);
                    deleteCmd.ExecuteNonQuery();
                }

                // Delete Orders second (references Customers)
                if (TableExists("ORDERS_TEST_PROVIDER", connection, transaction))
                {
                    var deleteOrders = "DELETE FROM PUB.ORDERS_TEST_PROVIDER";
                    using var deleteCmd = new OdbcCommand(deleteOrders, connection, transaction);
                    deleteCmd.ExecuteNonQuery();
                }

                // Delete Products third (references Categories)
                if (TableExists("PRODUCTS_TEST_PROVIDER", connection, transaction))
                {
                    var deleteProducts = "DELETE FROM PUB.PRODUCTS_TEST_PROVIDER";
                    using var deleteCmd = new OdbcCommand(deleteProducts, connection, transaction);
                    deleteCmd.ExecuteNonQuery();
                }

                // Delete Customers fourth (no foreign key dependencies)
                if (TableExists("CUSTOMERS_TEST_PROVIDER", connection, transaction))
                {
                    var deleteCustomers = "DELETE FROM PUB.CUSTOMERS_TEST_PROVIDER";
                    using var deleteCmd = new OdbcCommand(deleteCustomers, connection, transaction);
                    deleteCmd.ExecuteNonQuery();
                }

                // Delete Categories last (no foreign key dependencies)
                if (TableExists("CATEGORIES_TEST_PROVIDER", connection, transaction))
                {
                    var deleteCategories = "DELETE FROM PUB.CATEGORIES_TEST_PROVIDER";
                    using var deleteCmd = new OdbcCommand(deleteCategories, connection, transaction);
                    deleteCmd.ExecuteNonQuery();
                }
            }

            void InsertTestData()
            {
                // Insert in dependency order (parent tables first)

                // Insert Categories first
                var categoriesData = new[]
                {
                    "INSERT INTO PUB.CATEGORIES_TEST_PROVIDER (Id, Name, Description) VALUES (1, 'Electronics', 'Electronic devices and accessories')",
                    "INSERT INTO PUB.CATEGORIES_TEST_PROVIDER (Id, Name, Description) VALUES (2, 'Audio', 'Audio equipment and accessories')",
                    "INSERT INTO PUB.CATEGORIES_TEST_PROVIDER (Id, Name, Description) VALUES (3, 'Office', 'Office supplies and equipment')"
                };

                foreach (var categoryInsert in categoriesData)
                {
                    using var insertCategory = new OdbcCommand(categoryInsert, connection, transaction);
                    insertCategory.ExecuteNonQuery();
                }

                // Insert Customers second
                var customersData = new[]
                {
                    "INSERT INTO PUB.CUSTOMERS_TEST_PROVIDER (Id, Name, Email, Age, City, IsActive) VALUES (1, 'John Doe', 'john@example.com', 30, 'New York', 1)",
                    "INSERT INTO PUB.CUSTOMERS_TEST_PROVIDER (Id, Name, Email, Age, City, IsActive) VALUES (2, 'Jane Smith', 'jane@example.com', 25, 'Los Angeles', 1)",
                    "INSERT INTO PUB.CUSTOMERS_TEST_PROVIDER (Id, Name, Email, Age, City, IsActive) VALUES (3, 'Bob Johnson', 'bob@example.com', 35, 'Chicago', 0)",
                    "INSERT INTO PUB.CUSTOMERS_TEST_PROVIDER (Id, Name, Email, Age, City, IsActive) VALUES (4, 'Alice Brown', 'alice@example.com', 28, 'New York', 1)",
                    "INSERT INTO PUB.CUSTOMERS_TEST_PROVIDER (Id, Name, Email, Age, City, IsActive) VALUES (5, 'Charlie Wilson', 'charlie@example.com', 40, 'Boston', 0)",
                    "INSERT INTO PUB.CUSTOMERS_TEST_PROVIDER (Id, Name, Email, Age, City, IsActive) VALUES (6, 'Diana Prince', 'diana@example.com', 32, 'Washington', 1)",
                    "INSERT INTO PUB.CUSTOMERS_TEST_PROVIDER (Id, Name, Email, Age, City, IsActive) VALUES (7, 'Frank Miller', 'frank@example.com', 45, 'Seattle', 1)",
                    "INSERT INTO PUB.CUSTOMERS_TEST_PROVIDER (Id, Name, Email, Age, City, IsActive) VALUES (8, 'Grace Kelly', 'grace@example.com', 29, 'Miami', 1)",
                    "INSERT INTO PUB.CUSTOMERS_TEST_PROVIDER (Id, Name, Email, Age, City, IsActive) VALUES (9, 'Henry Ford', 'henry@example.com', 55, 'Detroit', 0)",
                    "INSERT INTO PUB.CUSTOMERS_TEST_PROVIDER (Id, Name, Email, Age, City, IsActive) VALUES (10, 'Ivy Chen', 'ivy@example.com', 27, 'San Francisco', 1)"
                };

                foreach (var customerInsert in customersData)
                {
                    using var insertCustomer = new OdbcCommand(customerInsert, connection, transaction);
                    insertCustomer.ExecuteNonQuery();
                }

                // Insert Products third (references Categories)
                var productsData = new[]
                {
                    "INSERT INTO PUB.PRODUCTS_TEST_PROVIDER (Id, Name, Price, CategoryId, Description, InStock) VALUES (1, 'Laptop', 999.99, 1, 'High-performance laptop', 1)",
                    "INSERT INTO PUB.PRODUCTS_TEST_PROVIDER (Id, Name, Price, CategoryId, Description, InStock) VALUES (2, 'Mouse', 29.99, 1, 'Wireless mouse', 1)",
                    "INSERT INTO PUB.PRODUCTS_TEST_PROVIDER (Id, Name, Price, CategoryId, Description, InStock) VALUES (3, 'Keyboard', 79.99, 1, 'Mechanical keyboard', 0)",
                    "INSERT INTO PUB.PRODUCTS_TEST_PROVIDER (Id, Name, Price, CategoryId, Description, InStock) VALUES (4, 'Monitor', 299.99, 1, '24-inch monitor', 1)",
                    "INSERT INTO PUB.PRODUCTS_TEST_PROVIDER (Id, Name, Price, CategoryId, Description, InStock) VALUES (5, 'Headphones', 149.99, 2, 'Noise-cancelling headphones', 1)",
                    "INSERT INTO PUB.PRODUCTS_TEST_PROVIDER (Id, Name, Price, CategoryId, Description, InStock) VALUES (6, 'Speakers', 199.99, 2, 'Bluetooth speakers', 1)",
                    "INSERT INTO PUB.PRODUCTS_TEST_PROVIDER (Id, Name, Price, CategoryId, Description, InStock) VALUES (7, 'Microphone', 89.99, 2, 'USB microphone', 1)",
                    "INSERT INTO PUB.PRODUCTS_TEST_PROVIDER (Id, Name, Price, CategoryId, Description, InStock) VALUES (8, 'Desk Chair', 259.99, 3, 'Ergonomic office chair', 1)",
                    "INSERT INTO PUB.PRODUCTS_TEST_PROVIDER (Id, Name, Price, CategoryId, Description, InStock) VALUES (9, 'Desk Lamp', 49.99, 3, 'LED desk lamp', 0)",
                    "INSERT INTO PUB.PRODUCTS_TEST_PROVIDER (Id, Name, Price, CategoryId, Description, InStock) VALUES (10, 'Webcam', 79.99, 1, 'HD webcam', 1)"
                };

                foreach (var productInsert in productsData)
                {
                    using var insertProduct = new OdbcCommand(productInsert, connection, transaction);
                    insertProduct.ExecuteNonQuery();
                }

                // Insert Orders fourth (references Customers)
                var ordersData = new[]
                {
                    "INSERT INTO PUB.ORDERS_TEST_PROVIDER (Id, CustomerId, OrderDate, TotalAmount, Status) VALUES (1, 1, '2024-01-15', 1079.98, 'Completed')",
                    "INSERT INTO PUB.ORDERS_TEST_PROVIDER (Id, CustomerId, OrderDate, TotalAmount, Status) VALUES (2, 2, '2024-01-16', 229.98, 'Shipped')",
                    "INSERT INTO PUB.ORDERS_TEST_PROVIDER (Id, CustomerId, OrderDate, TotalAmount, Status) VALUES (3, 4, '2024-01-17', 149.99, 'Processing')",
                    "INSERT INTO PUB.ORDERS_TEST_PROVIDER (Id, CustomerId, OrderDate, TotalAmount, Status) VALUES (4, 6, '2024-01-18', 359.98, 'Completed')",
                    "INSERT INTO PUB.ORDERS_TEST_PROVIDER (Id, CustomerId, OrderDate, TotalAmount, Status) VALUES (5, 10, '2024-01-19', 89.99, 'Shipped')"
                };

                foreach (var orderInsert in ordersData)
                {
                    using var insertOrder = new OdbcCommand(orderInsert, connection, transaction);
                    insertOrder.ExecuteNonQuery();
                }

                // Insert OrderItems last (references Orders and Products)
                var orderItemsData = new[]
                {
                    "INSERT INTO PUB.ORDER_ITEMS_TEST_PROVIDER (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES (1, 1, 1, 1, 999.99)",
                    "INSERT INTO PUB.ORDER_ITEMS_TEST_PROVIDER (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES (2, 1, 2, 1, 29.99)",
                    "INSERT INTO PUB.ORDER_ITEMS_TEST_PROVIDER (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES (3, 1, 3, 1, 79.99)",
                    "INSERT INTO PUB.ORDER_ITEMS_TEST_PROVIDER (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES (4, 2, 4, 1, 299.99)",
                    "INSERT INTO PUB.ORDER_ITEMS_TEST_PROVIDER (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES (5, 2, 5, 1, 149.99)",
                    "INSERT INTO PUB.ORDER_ITEMS_TEST_PROVIDER (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES (6, 3, 5, 1, 149.99)",
                    "INSERT INTO PUB.ORDER_ITEMS_TEST_PROVIDER (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES (7, 4, 4, 1, 299.99)",
                    "INSERT INTO PUB.ORDER_ITEMS_TEST_PROVIDER (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES (8, 4, 6, 1, 199.99)",
                    "INSERT INTO PUB.ORDER_ITEMS_TEST_PROVIDER (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES (9, 5, 7, 1, 89.99)"
                };

                foreach (var orderItemInsert in orderItemsData)
                {
                    using var insertOrderItem = new OdbcCommand(orderItemInsert, connection, transaction);
                    insertOrderItem.ExecuteNonQuery();
                }
            }
        }
    }
}