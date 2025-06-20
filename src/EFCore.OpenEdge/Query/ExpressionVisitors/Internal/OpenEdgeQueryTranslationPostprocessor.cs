using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    public class OpenEdgeQueryTranslationPostprocessor : RelationalQueryTranslationPostprocessor
    {
        public OpenEdgeQueryTranslationPostprocessor(
            QueryTranslationPostprocessorDependencies dependencies,
            RelationalQueryTranslationPostprocessorDependencies relationalDependencies,
            RelationalQueryCompilationContext queryCompilationContext)
            : base(dependencies, relationalDependencies, queryCompilationContext)
        {
        }

        public override Expression Process(Expression query)
        {
            query = new OpenEdgeQueryExpressionVisitor().Visit(query);
            return base.Process(query);
        }
    }
}