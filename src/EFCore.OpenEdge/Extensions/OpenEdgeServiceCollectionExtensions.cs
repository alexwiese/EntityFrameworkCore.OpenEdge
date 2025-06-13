using EntityFrameworkCore.OpenEdge.Infrastructure.Internal;
using EntityFrameworkCore.OpenEdge.Metadata.Conventions.Internal;
using EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal;
using EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal;
using EntityFrameworkCore.OpenEdge.Storage;
using EntityFrameworkCore.OpenEdge.Storage.Internal;
using EntityFrameworkCore.OpenEdge.Storage.Internal.Mapping;
using EntityFrameworkCore.OpenEdge.Update;
using EntityFrameworkCore.OpenEdge.Update.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
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
                .TryAdd<IUpdateSqlGenerator, OpenEdgeUpdateSqlGenerator>()
                .TryAdd<IModificationCommandBatchFactory, OpenEdgeModificationCommandBatchFactory>()
                .TryAdd<IRelationalConnection>(p => p.GetService<IOpenEdgeRelationalConnection>())
                .TryAdd<IBatchExecutor, BatchExecutor>()
                
                .TryAdd<IQueryTranslationPostprocessorFactory, OpenEdgeQueryTranslationPostprocessorFactory>()
                .TryAdd<IQuerySqlGeneratorFactory, OpenEdgeQuerySqlGeneratorFactory>()
                .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory, OpenEdgeSqlTranslatingExpressionVisitorFactory>()
                
                // .TryAdd<ISingletonUpdateSqlGenerator, OpenEdgeUpdateSqlGenerator>() // ISingletonUpdateSqlGenerator has been removed
                // .TryAdd<IRelationalResultOperatorHandler, OpenEdgeResultOperatorHandler>() // Supposedly no longer exists
                // .TryAdd<IQueryModelGenerator, OpenEdgeQueryModelGenerator>() // Supposedly no longer exists

                .TryAdd<IMemberTranslator, OpenEdgeCompositeMemberTranslator>()
                .TryAdd<IMethodCallTranslator, OpenEdgeCompositeMethodCallTranslator>()
                
                .TryAddProviderSpecificServices(b => b
                    .TryAddScoped<IOpenEdgeUpdateSqlGenerator, OpenEdgeUpdateSqlGenerator>()
                    .TryAddScoped<IOpenEdgeRelationalConnection, OpenEdgeRelationalConnection>()); ;

            builder.TryAddCoreServices();
            return serviceCollection;
        }

    }
}