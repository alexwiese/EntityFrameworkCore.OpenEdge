using EntityFrameworkCore.OpenEdge.Diagnostics.Internal;
using EntityFrameworkCore.OpenEdge.Infrastructure.Internal;
using EntityFrameworkCore.OpenEdge.Metadata.Conventions.Internal;
using EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal;
using EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal;
using EntityFrameworkCore.OpenEdge.Query.Sql.Internal;
using EntityFrameworkCore.OpenEdge.Storage;
using EntityFrameworkCore.OpenEdge.Storage.Internal;
using EntityFrameworkCore.OpenEdge.Storage.Internal.Mapping;
using EntityFrameworkCore.OpenEdge.Update;
using EntityFrameworkCore.OpenEdge.Update.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
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
                .TryAdd<LoggingDefinitions, OpenEdgeLoggingDefinitions>()
                .TryAdd<IDatabaseProvider, DatabaseProvider<OpenEdgeOptionsExtension>>()
                .TryAdd<IRelationalTypeMappingSource, OpenEdgeTypeMappingSource>()
                .TryAdd<ISqlGenerationHelper, OpenEdgeSqlGenerationHelper>()
                .TryAdd<IConventionSetBuilder, OpenEdgeRelationalConventionSetBuilder>()
                .TryAdd<IModelCustomizer, OpenEdgeModelCustomizer>()
                .TryAdd<IModificationCommandBatchFactory, OpenEdgeModificationCommandBatchFactory>()
                .TryAdd<IRelationalConnection>(p => p.GetService<IOpenEdgeRelationalConnection>())
                .TryAdd<IRelationalDatabaseCreator, OpenEdgeDatabaseCreator>()

                .TryAdd<IQueryTranslationPostprocessorFactory, OpenEdgeQueryTranslationPostprocessorFactory>()
                .TryAdd<IRelationalParameterBasedSqlProcessorFactory, OpenEdgeParameterBasedSqlProcessorFactory>()
                .TryAdd<IQuerySqlGeneratorFactory, OpenEdgeSqlGeneratorFactory>()
                .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, OpenEdgeQueryableMethodTranslatingExpressionVisitorFactory>()
                .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory,
                    OpenEdgeSqlTranslatingExpressionVisitorFactory>()

                .TryAdd<IMethodCallTranslatorProvider, OpenEdgeMethodCallTranslatorProvider>()
                .TryAdd<IMemberTranslatorProvider, OpenEdgeMemberTranslatorProvider>()
                
                .TryAddProviderSpecificServices(b => b
                    .TryAddScoped<IOpenEdgeRelationalConnection, OpenEdgeRelationalConnection>()
                );

            builder.TryAddCoreServices();
            
            // Force registration of our UpdateSqlGenerator after all other services
            serviceCollection.AddScoped<IUpdateSqlGenerator, OpenEdgeUpdateSqlGenerator>();
            
            return serviceCollection;
        }
    }
}