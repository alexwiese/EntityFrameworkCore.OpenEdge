using EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EntityFrameworkCore.OpenEdge.Query.Internal
{
    public class OpenEdgeQueryTranslationPreprocessor : RelationalQueryTranslationPreprocessor
    {
        public OpenEdgeQueryTranslationPreprocessor(QueryTranslationPreprocessorDependencies dependencies, 
                                                    RelationalQueryTranslationPreprocessorDependencies relationalDependencies, 
                                                    QueryCompilationContext queryCompilationContext) : 
            base(dependencies, relationalDependencies, queryCompilationContext)
        {

        }

        public override Expression Process(Expression query) => base.Process(Preprocess(query));

        private Expression Preprocess(Expression query)
        {             
            var result = new OpenEdgeExtractingExpressionVisitor().Visit(query);

            return result;
        }
    }
}
