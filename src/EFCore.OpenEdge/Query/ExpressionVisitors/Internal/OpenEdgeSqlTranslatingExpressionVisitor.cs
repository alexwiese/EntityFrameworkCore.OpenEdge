using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

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

        /// <summary>
        /// Handles member access expressions and ensures OpenEdge-compatible boolean comparisons.
        /// </summary>
        /// <param name="memberExpression">The member expression to visit (e.g., c.IsActive)</param>
        /// <returns>A SQL expression with explicit boolean comparison for OpenEdge compatibility</returns>
        /// <remarks>
        /// <para><strong>OpenEdge Boolean Handling Requirements:</strong></para>
        /// <para>
        /// OpenEdge SQL does not support implicit boolean evaluation in WHERE clauses.
        /// Standard SQL databases typically allow expressions like <c>WHERE boolean_column</c>,
        /// but OpenEdge requires explicit comparisons like <c>WHERE boolean_column = 1</c>.
        /// </para>
        /// 
        /// <para><strong>Problem This Solves:</strong></para>
        /// <list type="bullet">
        /// <item>EF Core generates: <c>WHERE "c"."IsActive"</c></item>
        /// <item>OpenEdge requires: <c>WHERE "c"."IsActive" = 1</c></item>
        /// <item>Without this transformation, OpenEdge throws syntax errors</item>
        /// </list>
        /// 
        /// <para><strong>Transformation Examples:</strong></para>
        /// <list type="bullet">
        /// <item><c>c.IsActive</c> → <c>"c"."IsActive" = 1</c></item>
        /// <item><c>c.IsDeleted</c> → <c>"c"."IsDeleted" = 1</c></item>
        /// <item><c>!c.IsActive</c> → <c>"c"."IsActive" &lt;&gt; 1</c> (handled by base class NOT operator)</item>
        /// </list>
        /// 
        /// <para><strong>Technical Details:</strong></para>
        /// <para>
        /// The method uses integer type mapping for the comparison constants (0 and 1) to ensure
        /// proper SQL generation. This avoids type mapping conflicts that could occur if we used
        /// boolean constants in a context where EF Core expects integer comparisons.
        /// </para>
        /// </remarks>
        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            var result = base.VisitMember(memberExpression);
            
            // Check if this is a boolean member that needs explicit comparison for OpenEdge
            if (result is SqlExpression sqlResult && sqlResult.Type == typeof(bool))
            {
                // Transform boolean member to explicit comparison with 1
                // This ensures OpenEdge gets "boolean_column = 1" instead of just "boolean_column"
                var intTypeMapping = Dependencies.TypeMappingSource.FindMapping(typeof(int));
                
                return Dependencies.SqlExpressionFactory.Equal(
                    sqlResult,
                    Dependencies.SqlExpressionFactory.Constant(1, intTypeMapping));
            }
            
            return result;
        }
    }
}