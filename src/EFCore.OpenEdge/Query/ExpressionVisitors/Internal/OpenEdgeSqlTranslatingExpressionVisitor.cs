using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    /// <summary>
    /// Translates LINQ expressions to SQL expressions for OpenEdge.
    /// Handles OpenEdge-specific translation requirements.
    /// </summary>
    public class OpenEdgeSqlTranslatingExpressionVisitor(
        RelationalSqlTranslatingExpressionVisitorDependencies dependencies,
        QueryCompilationContext queryCompilationContext,
        QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor) : RelationalSqlTranslatingExpressionVisitor(dependencies, queryCompilationContext, queryableMethodTranslatingExpressionVisitor)
    {
        private bool _isInPredicateContext = false;
        private bool _isInComparisonContext = false;

        #nullable enable
        private readonly OpenEdgeQueryableMethodTranslatingExpressionVisitor? _openEdgeQueryableVisitor = queryableMethodTranslatingExpressionVisitor as OpenEdgeQueryableMethodTranslatingExpressionVisitor;

        /// <summary>
        /// Overrides the Translate method to track when we're in a predicate context.
        /// The allowOptimizedExpansion parameter is typically true for WHERE/HAVING predicates.
        /// </summary>
#nullable enable
        public override SqlExpression? Translate(Expression expression, bool allowOptimizedExpansion = false)
        {
            var oldIsInPredicateContext = _isInPredicateContext;
            
            // When allowOptimizedExpansion is true AND we're not in ORDER BY context,
            // we're in a WHERE/HAVING/JOIN predicate (presumably?)
            if (allowOptimizedExpansion && !(_openEdgeQueryableVisitor?.IsTranslatingOrderBy ?? false))
            {
                _isInPredicateContext = true;
            }

            try
            {
                return base.Translate(expression, allowOptimizedExpansion);
            }
            finally
            {
                _isInPredicateContext = oldIsInPredicateContext;
            }
        }

        /// <summary>
        /// Handles member access expressions and ensures OpenEdge-compatible boolean comparisons.
        /// Only applies comparison transformation in WHERE/HAVING/JOIN contexts.
        /// Never applies transformation in ORDER BY or SELECT projection contexts.
        /// </summary>
        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            var result = base.VisitMember(memberExpression);
            
            // Never transform boolean members in ORDER BY context (double-check)
            if (_openEdgeQueryableVisitor?.IsTranslatingOrderBy ?? false)
            {
                return result;
            }
            
            // Only transform boolean members to comparisons when:
            // 1. We're definitively in a predicate context (WHERE/HAVING/JOIN)
            // 2. NOT already in a comparison context (to avoid double comparisons)
            // 3. The result is a boolean SQL expression
            if (_isInPredicateContext &&
                !_isInComparisonContext && 
                result is SqlExpression sqlResult && 
                sqlResult.Type == typeof(bool))
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

        /// <summary>
        /// Handles binary expressions to set comparison context.
        /// </summary>
        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            // If this is a comparison operation, set the context flag to prevent
            // boolean members from being transformed to comparisons
            var oldIsInComparisonContext = _isInComparisonContext;

            if (IsBooleanComparison(binaryExpression))
            {
                _isInComparisonContext = true;
            }
            
            try
            {
                return base.VisitBinary(binaryExpression);
            }
            finally
            {
                _isInComparisonContext = oldIsInComparisonContext;
            }
        }

        /// <summary>
        /// Checks if the expression is a comparison operation.
        /// </summary>
        private static bool IsBooleanComparison(BinaryExpression expression)
        {
            return expression.NodeType == ExpressionType.Equal ||
                   expression.NodeType == ExpressionType.NotEqual ||
                   expression.NodeType == ExpressionType.GreaterThan ||
                   expression.NodeType == ExpressionType.GreaterThanOrEqual ||
                   expression.NodeType == ExpressionType.LessThan ||
                   expression.NodeType == ExpressionType.LessThanOrEqual ||
                   expression.NodeType == ExpressionType.AndAlso ||
                   expression.NodeType == ExpressionType.OrElse;
        }
    }
}