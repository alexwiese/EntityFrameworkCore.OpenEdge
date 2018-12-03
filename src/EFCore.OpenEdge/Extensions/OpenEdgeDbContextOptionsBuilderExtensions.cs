using System;
using EntityFrameworkCore.OpenEdge.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkCore.OpenEdge.Extensions
{
    public static class OpenEdgeDbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseOpenEdge(
            this DbContextOptionsBuilder optionsBuilder,
            string connectionString,
            Action<DbContextOptionsBuilder> optionsAction = null)
        {
            
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