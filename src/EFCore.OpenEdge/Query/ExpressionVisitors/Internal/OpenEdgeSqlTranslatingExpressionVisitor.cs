using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    /// <summary>
    /// Translates LINQ expressions to SQL expressions for OpenEdge.
    /// Handles OpenEdge-specific translation requirements.
    /// </summary>
    public class OpenEdgeSqlTranslatingExpressionVisitor : RelationalSqlTranslatingExpressionVisitor
    {
        public OpenEdgeSqlTranslatingExpressionVisitor(
            RelationalSqlTranslatingExpressionVisitorDependencies dependencies,
            QueryCompilationContext queryCompilationContext,
            QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
            : base(dependencies, queryCompilationContext, queryableMethodTranslatingExpressionVisitor)
        {
        }

    }
}