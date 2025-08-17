using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    public class OpenEdgeQueryableMethodTranslatingExpressionVisitor : RelationalQueryableMethodTranslatingExpressionVisitor
    {
        public OpenEdgeQueryableMethodTranslatingExpressionVisitor(
            QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
            RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
            RelationalQueryCompilationContext queryCompilationContext)
            : base(dependencies, relationalDependencies, queryCompilationContext)
        {
        }
    }
}