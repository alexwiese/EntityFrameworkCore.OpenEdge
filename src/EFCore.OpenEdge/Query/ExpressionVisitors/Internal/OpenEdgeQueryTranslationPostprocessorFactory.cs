using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    /// <summary>
    /// Factory for creating OpenEdgeQueryTranslationPostprocessor instances.
    /// Replaces the old QueryModelGenerator pattern.
    /// </summary>
    public class OpenEdgeQueryTranslationPostprocessorFactory(
        QueryTranslationPostprocessorDependencies dependencies,
        RelationalQueryTranslationPostprocessorDependencies relationalDependencies) : IQueryTranslationPostprocessorFactory
    {
        public virtual QueryTranslationPostprocessor Create(QueryCompilationContext queryCompilationContext)
            => new OpenEdgeQueryTranslationPostprocessor(
                dependencies,
                relationalDependencies,
                (RelationalQueryCompilationContext) queryCompilationContext);
    }
}