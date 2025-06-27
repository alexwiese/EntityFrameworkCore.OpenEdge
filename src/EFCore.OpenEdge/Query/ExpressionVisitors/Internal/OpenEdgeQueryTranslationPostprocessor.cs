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
            // TODO: Fix OpenEdgeQueryExpressionVisitor - it's corrupting projection bindings
            // For now, disable it to get basic SQL generation working
            return base.Process(query);
            
            // if (query is ShapedQueryExpression shapedQueryExpression)
            // {
            //     var visitor = new OpenEdgeQueryExpressionVisitor();
            //     return shapedQueryExpression.Update(
            //         visitor.Visit(shapedQueryExpression.QueryExpression),
            //         visitor.Visit(shapedQueryExpression.ShaperExpression));
            // }
            //
            // query = new OpenEdgeQueryExpressionVisitor().Visit(query);
            // return base.Process(query);
        }
    }
}