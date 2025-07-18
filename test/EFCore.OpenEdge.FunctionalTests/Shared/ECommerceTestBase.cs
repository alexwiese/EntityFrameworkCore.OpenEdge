using System;
using EFCore.OpenEdge.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.OpenEdge.FunctionalTests.Shared
{
    public abstract class ECommerceTestBase : OpenEdgeTestBase, IDisposable
    {
        protected ECommerceTestBase()
        {
            // Ensure database is seeded with test data
            TestDataSeeder.EnsureSeeded(ConnectionString);
        }

        protected ECommerceTestContext CreateContext()
        {
            var options = CreateOptionsBuilder<ECommerceTestContext>().Options;
            var context = new ECommerceTestContext(options);
            
            // Disable savepoints for OpenEdge compatibility
            context.Database.AutoSavepointsEnabled = false;
            
            return context;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}