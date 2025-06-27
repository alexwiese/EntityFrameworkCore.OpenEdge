using System;
using System.Data.Odbc;
using EFCore.OpenEdge.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public abstract class BasicQueryTestBase : OpenEdgeTestBase, IDisposable
    {
        private static bool _databaseInitialized = false;
        private static readonly object _lock = new();

        protected BasicQueryTestBase()
        {
            lock (_lock)
            {
                if (!_databaseInitialized)
                {
                    SetupDatabase();
                    _databaseInitialized = true;
                }
            }
        }

        private void SetupDatabase()
        {
            // using var connection = new OdbcConnection(ConnectionString);
            // connection.Open();

            // // Drop tables if they exist (ignore errors)
            // try
            // {
            //     using var dropCustomers = new OdbcCommand("DROP TABLE Customers", connection);
            //     dropCustomers.ExecuteNonQuery();
            // }
            // catch { }

            // try
            // {
            //     using var dropProducts = new OdbcCommand("DROP TABLE Products", connection);
            //     dropProducts.ExecuteNonQuery();
            // }
            // catch { }

            // // Create tables
            // var createCustomersTable = @"
            //     CREATE TABLE Customers_TEST_PROVIDER (
            //         Id INTEGER NOT NULL,
            //         Name CHARACTER(100) NOT NULL,
            //         Email CHARACTER(100),
            //         Age INTEGER NOT NULL,
            //         City CHARACTER(50),
            //         IsActive LOGICAL NOT NULL,
            //         PRIMARY KEY (Id)
            //     )";

            // var createProductsTable = @"
            //     CREATE TABLE Products_TEST_PROVIDER (
            //         Id INTEGER NOT NULL,
            //         Name CHARACTER(100) NOT NULL,
            //         Price DECIMAL(10,2) NOT NULL,
            //         CategoryId INTEGER NOT NULL,
            //         Description CHARACTER(500),
            //         InStock LOGICAL NOT NULL,
            //         PRIMARY KEY (Id)
            //     )";

            // using var createCustomers = new OdbcCommand(createCustomersTable, connection);
            // createCustomers.ExecuteNonQuery();

            // using var createProducts = new OdbcCommand(createProductsTable, connection);
            // createProducts.ExecuteNonQuery();

            // // Insert test data
            // var insertCustomersData = @"
            //     INSERT INTO Customers_TEST_PROVIDER (Id, Name, Email, Age, City, IsActive) VALUES 
            //     (1, 'John Doe', 'john@example.com', 30, 'New York', TRUE),
            //     (2, 'Jane Smith', 'jane@example.com', 25, 'Los Angeles', TRUE),
            //     (3, 'Bob Johnson', 'bob@example.com', 35, 'Chicago', FALSE),
            //     (4, 'Alice Brown', 'alice@example.com', 28, 'New York', TRUE),
            //     (5, 'Charlie Wilson', 'charlie@example.com', 40, 'Boston', FALSE)";

            // var insertProductsData = @"
            //     INSERT INTO Products_TEST_PROVIDER (Id, Name, Price, CategoryId, Description, InStock) VALUES 
            //     (1, 'Laptop', 999.99, 1, 'High-performance laptop', TRUE),
            //     (2, 'Mouse', 29.99, 1, 'Wireless mouse', TRUE),
            //     (3, 'Keyboard', 79.99, 1, 'Mechanical keyboard', FALSE),
            //     (4, 'Monitor', 299.99, 1, '24-inch monitor', TRUE),
            //     (5, 'Headphones', 149.99, 2, 'Noise-cancelling headphones', TRUE)";

            // using var insertCustomers = new OdbcCommand(insertCustomersData, connection);
            // insertCustomers.ExecuteNonQuery();

            // using var insertProducts = new OdbcCommand(insertProductsData, connection);
            // insertProducts.ExecuteNonQuery();
        }

        protected BasicQueryContext CreateContext()
        {
            var options = CreateOptionsBuilder<BasicQueryContext>().Options;
            return new BasicQueryContext(options);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
