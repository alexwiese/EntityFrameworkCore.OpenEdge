using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.OpenEdge.Query.Internal
{
    public class OpenEdgeQueryTranslationPreprocessorFactory : IQueryTranslationPreprocessorFactory
    {
        public OpenEdgeQueryTranslationPreprocessorFactory(QueryTranslationPreprocessorDependencies dependencies,
                                                           RelationalQueryTranslationPreprocessorDependencies relationalDependencies)
        {
            Dependencies = dependencies;
            RelationalDependencies = relationalDependencies;
        }

        protected QueryTranslationPreprocessorDependencies Dependencies { get; }

        protected RelationalQueryTranslationPreprocessorDependencies RelationalDependencies;

        public QueryTranslationPreprocessor Create(QueryCompilationContext queryCompilationContext)
            => new OpenEdgeQueryTranslationPreprocessor(Dependencies, RelationalDependencies, queryCompilationContext);
    }
}
