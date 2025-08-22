using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    public class OpenEdgeQueryTranslationPostprocessor(
        QueryTranslationPostprocessorDependencies dependencies,
        RelationalQueryTranslationPostprocessorDependencies relationalDependencies,
        RelationalQueryCompilationContext queryCompilationContext) : RelationalQueryTranslationPostprocessor(dependencies, relationalDependencies, queryCompilationContext)
    {
        public override Expression Process(Expression query)
        {
            // First, let the base class do its processing
            query = base.Process(query);
            
            // Then apply OpenEdge-specific boolean comparison simplification
            query = new BooleanComparisonSimplificationVisitor(RelationalDependencies.SqlExpressionFactory).Visit(query);
            
            return query;
        }
        
        /// <summary>
        /// Visitor that simplifies redundant boolean comparisons for OpenEdge compatibility.
        /// 
        /// OpenEdge doesn't support comparing boolean expressions with parameters like:
        /// WHERE (column = 1) = ?
        /// 
        /// This visitor simplifies such patterns to:
        /// - (bool_expr) = true  → bool_expr
        /// - (bool_expr) = false → NOT (bool_expr)
        /// </summary>
        private sealed class BooleanComparisonSimplificationVisitor(ISqlExpressionFactory sqlExpressionFactory) : ExpressionVisitor
        {
            private readonly ISqlExpressionFactory _sqlExpressionFactory = sqlExpressionFactory;

            protected override Expression VisitExtension(Expression extensionExpression)
            {
                // Handle ShapedQueryExpression - must be done manually
                if (extensionExpression is ShapedQueryExpression shapedQueryExpression)
                {
                    var queryExpression = Visit(shapedQueryExpression.QueryExpression);
                    
                    return queryExpression != shapedQueryExpression.QueryExpression
                        ? shapedQueryExpression.Update(queryExpression, shapedQueryExpression.ShaperExpression)
                        : shapedQueryExpression;
                }
                
                // Handle SqlBinaryExpression for equality comparisons
                if (extensionExpression is SqlBinaryExpression sqlBinary && 
                    sqlBinary.OperatorType == ExpressionType.Equal)
                {
                    var left = sqlBinary.Left;
                    var right = sqlBinary.Right;
                    
                    // Check if we have a boolean expression compared with a boolean constant/parameter
                    if (left.Type == typeof(bool) && right.Type == typeof(bool))
                    {
                        // Case 1: (bool_expr) = true constant
                        if (right is SqlConstantExpression { Value: true })
                        {
                            // Simplify to just the left expression
                            return Visit(left);
                        }
                        
                        // Case 2: (bool_expr) = false constant
                        if (right is SqlConstantExpression { Value: false })
                        {
                            // Simplify to NOT (bool_expr)
                            return Visit(_sqlExpressionFactory.Not(left));
                        }
                        
                        // Handle reverse cases: constant/parameter on left side
                        if (left is SqlConstantExpression { Value: true })
                        {
                            return Visit(right);
                        }
                        
                        if (left is SqlConstantExpression { Value: false })
                        {
                            return Visit(_sqlExpressionFactory.Not(right));
                        }
                    }
                }
                
                return base.VisitExtension(extensionExpression);
            }
            
            /// <summary>
            /// Checks if the expression is a boolean comparison (e.g., column = 1)
            /// </summary>
            private static bool IsBooleanComparison(SqlExpression expression)
            {
                return expression is SqlBinaryExpression binary &&
                       binary.Type == typeof(bool) &&
                       (binary.OperatorType == ExpressionType.Equal ||
                        binary.OperatorType == ExpressionType.NotEqual ||
                        binary.OperatorType == ExpressionType.GreaterThan ||
                        binary.OperatorType == ExpressionType.GreaterThanOrEqual ||
                        binary.OperatorType == ExpressionType.LessThan ||
                        binary.OperatorType == ExpressionType.LessThanOrEqual ||
                        binary.OperatorType == ExpressionType.AndAlso ||
                        binary.OperatorType == ExpressionType.OrElse);
            }
        }
    }
}