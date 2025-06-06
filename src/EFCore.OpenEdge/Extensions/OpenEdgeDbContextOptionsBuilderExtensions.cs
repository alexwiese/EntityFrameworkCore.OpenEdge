using System;
using EntityFrameworkCore.OpenEdge.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

// Note!: Namespace intentionally matches EF Core provider convention
// rather than file location for better user experience
#pragma warning disable IDE0130
namespace Microsoft.EntityFrameworkCore
{
    public static class OpenEdgeDbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseOpenEdge(
            this DbContextOptionsBuilder optionsBuilder,
            string connectionString,
            Action<DbContextOptionsBuilder> optionsAction = null)
        {
            /*
             * Adds the OpenEdgeOptionsExtension extension to the internal collection.
             * 
             *   // Without this pattern, users would need to do:
             *    services.AddEntityFrameworkOpenEdge();  // Manual registration
             *    services.AddDbContext<MyContext>(options => 
             *      options.UseOpenEdge("connection"));
             *
             *    // With this pattern, users only need:
             *    services.AddDbContext<MyContext>(options => 
             *      options.UseOpenEdge("connection"));  // Automatic registration
             */
            var extension = GetOrCreateExtension(optionsBuilder).WithConnectionString(connectionString);
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            optionsAction?.Invoke(optionsBuilder);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseOpenEdge<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            string connectionString,
            Action<DbContextOptionsBuilder> optionsAction = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseOpenEdge(
                (DbContextOptionsBuilder)optionsBuilder, connectionString, optionsAction);

        private static OpenEdgeOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder) 
            => optionsBuilder.Options.FindExtension<OpenEdgeOptionsExtension>() ?? new OpenEdgeOptionsExtension();
    }
}