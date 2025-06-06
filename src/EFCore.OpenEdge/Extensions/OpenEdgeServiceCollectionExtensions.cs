using EntityFrameworkCore.OpenEdge.Infrastructure.Internal;
using EntityFrameworkCore.OpenEdge.Metadata.Conventions.Internal;
using EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal;
using EntityFrameworkCore.OpenEdge.Query.Internal;
using EntityFrameworkCore.OpenEdge.Query.Sql.Internal;
using EntityFrameworkCore.OpenEdge.Storage;
using EntityFrameworkCore.OpenEdge.Storage.Internal;
using EntityFrameworkCore.OpenEdge.Storage.Internal.Mapping;
using EntityFrameworkCore.OpenEdge.Update;
using EntityFrameworkCore.OpenEdge.Update.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.OpenEdge.Extensions
{
    /*
     * Contains configurations that make OpenEdge provider work with Entity Framework Core's dependency injection system.
     * Essentially, when someone uses .UseOpenEdge(), this file describes all the OpenEdge-specific implementations
     * that should be used instead of the default ones.
     */
    public static class OpenEdgeServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkOpenEdge(this IServiceCollection serviceCollection)
        {
            var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
                // Registers the main provider that EF Core uses to identify this as the OpenEdge provider
                .TryAdd<IDatabaseProvider, DatabaseProvider<OpenEdgeOptionsExtension>>()
                // Contains code/database first type mappings
                .TryAdd<IRelationalTypeMappingSource, OpenEdgeTypeMappingSource>()
                // Handles OpenEdge-specific SQL syntax
                .TryAdd<ISqlGenerationHelper, OpenEdgeSqlGenerationHelper>()
                .TryAdd<IConventionSetBuilder, OpenEdgeRelationalConventionSetBuilder>()
                
                // TODO: Add appropriate informative explanations for these
                .TryAdd<IUpdateSqlGenerator, OpenEdgeUpdateSqlGenerator>()
                .TryAdd<ISingletonUpdateSqlGenerator, OpenEdgeUpdateSqlGenerator>()
                
                // Batches multiple database operations together
                .TryAdd<IModificationCommandBatchFactory, OpenEdgeModificationCommandBatchFactory>()
                
                // TODO: Add appropriate informative explanations for these
                .TryAdd<IRelationalConnection>(p => p.GetService<IOpenEdgeRelationalConnection>())
                .TryAdd<IRelationalResultOperatorHandler, OpenEdgeResultOperatorHandler>()
                .TryAdd<IQueryModelGenerator, OpenEdgeQueryModelGenerator>()

                .TryAdd<IBatchExecutor, BatchExecutor>()

                .TryAdd<IMemberTranslator, OpenEdgeCompositeMemberTranslator>()
                .TryAdd<ICompositeMethodCallTranslator, OpenEdgeCompositeMethodCallTranslator>()
                .TryAdd<IQuerySqlGeneratorFactory, OpenEdgeSqlGeneratorFactory>()
                
                .TryAddProviderSpecificServices(b => b
                    .TryAddScoped<IOpenEdgeUpdateSqlGenerator, OpenEdgeUpdateSqlGenerator>()
                    .TryAddScoped<IOpenEdgeRelationalConnection, OpenEdgeRelationalConnection>()); ;

            builder.TryAddCoreServices();
            return serviceCollection;
        }

    }
}