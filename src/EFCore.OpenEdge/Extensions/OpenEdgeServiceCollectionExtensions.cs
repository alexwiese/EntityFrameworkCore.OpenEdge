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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.OpenEdge.Extensions
{
    public static class OpenEdgeServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkOpenEdge(this IServiceCollection serviceCollection)
        {
#pragma warning disable EF1001 // Internal EF Core API usage.
            var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
                .TryAdd<IBatchExecutor, BatchExecutor>()
#pragma warning restore EF1001 // Internal EF Core API usage.
                .TryAdd<IDatabaseProvider, DatabaseProvider<OpenEdgeOptionsExtension>>()
                .TryAdd<IRelationalTypeMappingSource, OpenEdgeTypeMappingSource>()
                .TryAdd<ISqlGenerationHelper, OpenEdgeSqlGenerationHelper>()
                .TryAdd<IConventionSetBuilder, OpenEdgeRelationalConventionSetBuilder>()
                .TryAdd<IUpdateSqlGenerator, OpenEdgeUpdateSqlGenerator>()
                .TryAdd<IModificationCommandBatchFactory, OpenEdgeModificationCommandBatchFactory>()
                .TryAdd<IRelationalConnection>(p => p.GetService<IOpenEdgeRelationalConnection>())
                //.TryAdd<IRelationalResultOperatorHandler, OpenEdgeResultOperatorHandler>()
                //.TryAdd<IQueryModelGenerator, OpenEdgeQueryModelGenerator>()
                .TryAdd<IQueryTranslationPreprocessorFactory, OpenEdgeQueryTranslationPreprocessorFactory>()

                /* TESTING */
            
                .TryAdd<IAsyncQueryProvider, EntityQueryProvider>()
                .TryAdd<IQueryCompiler, QueryCompiler>()
                .TryAdd<IEvaluatableExpressionFilter, RelationalEvaluatableExpressionFilter>()

                /* TESTING */


                .TryAdd<LoggingDefinitions, OpenEdgeLoggingDefinitions>()
                .TryAdd<IMemberTranslatorProvider, OpenEdgeCompositeMemberTranslator>()
                .TryAdd<IMethodCallTranslatorProvider, OpenEdgeCompositeMethodCallTranslator>()
                .TryAdd<IQuerySqlGeneratorFactory, OpenEdgeSqlGeneratorFactory>()

                .TryAddProviderSpecificServices(b => b
                    .TryAddScoped<IOpenEdgeUpdateSqlGenerator, OpenEdgeUpdateSqlGenerator>()
                    .TryAddScoped<IOpenEdgeRelationalConnection, OpenEdgeRelationalConnection>());

            builder.TryAddCoreServices();
            return serviceCollection;
        }
    }
}


/*
== FROM EFCore

public static IServiceCollection AddEntityFrameworkSqlServer(this IServiceCollection serviceCollection)
{
    Check.NotNull(serviceCollection, nameof(serviceCollection));

    new EntityFrameworkRelationalServicesBuilder(serviceCollection)
        .TryAdd<LoggingDefinitions, SqlServerLoggingDefinitions>()
        .TryAdd<IDatabaseProvider, DatabaseProvider<SqlServerOptionsExtension>>()
        .TryAdd<IValueGeneratorCache>(p => p.GetRequiredService<ISqlServerValueGeneratorCache>())
        .TryAdd<IRelationalTypeMappingSource, SqlServerTypeMappingSource>()
        .TryAdd<ISqlGenerationHelper, SqlServerSqlGenerationHelper>()
        .TryAdd<IRelationalAnnotationProvider, SqlServerAnnotationProvider>()
        .TryAdd<IMigrationsAnnotationProvider, SqlServerMigrationsAnnotationProvider>()
        .TryAdd<IModelValidator, SqlServerModelValidator>()
        .TryAdd<IProviderConventionSetBuilder, SqlServerConventionSetBuilder>()
        .TryAdd<IUpdateSqlGenerator>(p => p.GetRequiredService<ISqlServerUpdateSqlGenerator>())
        .TryAdd<IEvaluatableExpressionFilter, SqlServerEvaluatableExpressionFilter>()
        .TryAdd<IRelationalTransactionFactory, SqlServerTransactionFactory>()
        .TryAdd<IModificationCommandBatchFactory, SqlServerModificationCommandBatchFactory>()
        .TryAdd<IValueGeneratorSelector, SqlServerValueGeneratorSelector>()
        .TryAdd<IRelationalConnection>(p => p.GetRequiredService<ISqlServerConnection>())
        .TryAdd<IMigrationsSqlGenerator, SqlServerMigrationsSqlGenerator>()
        .TryAdd<IRelationalDatabaseCreator, SqlServerDatabaseCreator>()
        .TryAdd<IHistoryRepository, SqlServerHistoryRepository>()
        .TryAdd<IExecutionStrategyFactory, SqlServerExecutionStrategyFactory>()
        .TryAdd<IRelationalQueryStringFactory, SqlServerQueryStringFactory>()
        .TryAdd<ICompiledQueryCacheKeyGenerator, SqlServerCompiledQueryCacheKeyGenerator>()
        .TryAdd<IQueryCompilationContextFactory, SqlServerQueryCompilationContextFactory>()
        .TryAdd<IMethodCallTranslatorProvider, SqlServerMethodCallTranslatorProvider>()
        .TryAdd<IMemberTranslatorProvider, SqlServerMemberTranslatorProvider>()
        .TryAdd<IQuerySqlGeneratorFactory, SqlServerQuerySqlGeneratorFactory>()
        .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory, SqlServerSqlTranslatingExpressionVisitorFactory>()
        .TryAdd<IRelationalParameterBasedSqlProcessorFactory, SqlServerParameterBasedSqlProcessorFactory>()
        .TryAdd<IQueryRootCreator, SqlServerQueryRootCreator>()
        .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, SqlServerQueryableMethodTranslatingExpressionVisitorFactory>()
        .TryAddProviderSpecificServices(
            b => b
                .TryAddSingleton<ISqlServerValueGeneratorCache, SqlServerValueGeneratorCache>()
                .TryAddSingleton<ISqlServerUpdateSqlGenerator, SqlServerUpdateSqlGenerator>()
                .TryAddSingleton<ISqlServerSequenceValueGeneratorFactory, SqlServerSequenceValueGeneratorFactory>()
                .TryAddScoped<ISqlServerConnection, SqlServerConnection>())
        .TryAddCoreServices();

    return serviceCollection;
}
*/
