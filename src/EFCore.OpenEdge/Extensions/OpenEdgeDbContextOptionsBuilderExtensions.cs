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
        /// <summary>
        /// Configures the context to connect to an OpenEdge database.
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <param name="connectionString">The connection string of the database to connect to.</param>
        /// <param name="optionsAction">An optional action to allow additional configuration.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder UseOpenEdge(
            this DbContextOptionsBuilder optionsBuilder,
            string connectionString,
            Action<DbContextOptionsBuilder> optionsAction = null)
        {
            return UseOpenEdge(optionsBuilder, connectionString, defaultSchema: null, optionsAction);
        }

        /// <summary>
        /// Configures the context to connect to an OpenEdge database with a specific default schema.
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <param name="connectionString">The connection string of the database to connect to.</param>
        /// <param name="defaultSchema">The default schema to use for tables when not explicitly specified. Defaults to "pub" if null.</param>
        /// <param name="optionsAction">An optional action to allow additional configuration.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder UseOpenEdge(
            this DbContextOptionsBuilder optionsBuilder,
            string connectionString,
            string defaultSchema,
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
            
            if (defaultSchema != null)
            {
                extension = ((OpenEdgeOptionsExtension) extension).WithDefaultSchema(defaultSchema);
            }
            
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            optionsAction?.Invoke(optionsBuilder);

            return optionsBuilder;
        }

        /// <summary>
        /// Configures the context to connect to an OpenEdge database.
        /// </summary>
        /// <typeparam name="TContext">The type of context to be configured.</typeparam>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <param name="connectionString">The connection string of the database to connect to.</param>
        /// <param name="optionsAction">An optional action to allow additional configuration.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder<TContext> UseOpenEdge<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            string connectionString,
            Action<DbContextOptionsBuilder> optionsAction = null)
            where TContext : DbContext
            => UseOpenEdge(optionsBuilder, connectionString, defaultSchema: null, optionsAction);

        /// <summary>
        /// Configures the context to connect to an OpenEdge database with a specific default schema.
        /// </summary>
        /// <typeparam name="TContext">The type of context to be configured.</typeparam>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <param name="connectionString">The connection string of the database to connect to.</param>
        /// <param name="defaultSchema">The default schema to use for tables when not explicitly specified. Defaults to "pub" if null.</param>
        /// <param name="optionsAction">An optional action to allow additional configuration.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder<TContext> UseOpenEdge<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            string connectionString,
            string defaultSchema,
            Action<DbContextOptionsBuilder> optionsAction = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseOpenEdge(
                (DbContextOptionsBuilder)optionsBuilder, connectionString, defaultSchema, optionsAction);

        private static OpenEdgeOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder) 
            => optionsBuilder.Options.FindExtension<OpenEdgeOptionsExtension>() ?? new OpenEdgeOptionsExtension();
    }
}