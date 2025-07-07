using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    /// <summary>
    /// OpenEdge-specific parameter-based SQL processor that handles cases where 
    /// SQL query needs to change based on parameter values.
    /// 
    /// Primary responsibility: Inline OFFSET/FETCH parameter values since OpenEdge 
    /// doesn't support parameterized OFFSET/FETCH clauses.
    /// </summary>
    public class OpenEdgeParameterBasedSqlProcessor : RelationalParameterBasedSqlProcessor
    {
        public OpenEdgeParameterBasedSqlProcessor(
            RelationalParameterBasedSqlProcessorDependencies dependencies,
            RelationalParameterBasedSqlProcessorParameters parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Main entry point - optimizes the query expression with parameter values
        /// </summary>
        #nullable enable
        public override Expression Optimize(Expression queryExpression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
        {
            // First, let the base class do its work (e.g. nullability processing)
            queryExpression = base.Optimize(queryExpression, parametersValues, out canCache);

            // Now, run our custom visitor to inline OFFSET/FETCH values
            return new OffsetValueInliningExpressionVisitor(Dependencies.SqlExpressionFactory, parametersValues).Visit(queryExpression);
        }

        /// <summary>
        /// Visitor that finds OFFSET/FETCH SqlParameterExpressions and replaces them with SqlConstantExpressions
        /// so that the parameter values are inlined directly into the SQL string.
        /// </summary>
    private sealed class OffsetValueInliningExpressionVisitor : ExpressionVisitor
    {
        private readonly ISqlExpressionFactory _sqlExpressionFactory;
        #nullable enable
        private readonly IReadOnlyDictionary<string, object?> _parameterValues;


        public OffsetValueInliningExpressionVisitor(
            ISqlExpressionFactory sqlExpressionFactory,
            IReadOnlyDictionary<string, object?> parameterValues)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
            _parameterValues = parameterValues;
        }

        protected override Expression VisitExtension(Expression extensionExpression)
        {
            if (extensionExpression is ShapedQueryExpression shapedQuery)
            {
                var newQueryExpression = Visit(shapedQuery.QueryExpression);
                return newQueryExpression != shapedQuery.QueryExpression
                    ? shapedQuery.Update(newQueryExpression, shapedQuery.ShaperExpression)
                    : shapedQuery;
            }

            if (extensionExpression is SelectExpression selectExpression)
            {
                var newOffset = InlineParameter(selectExpression.Offset);
                var newLimit = InlineParameter(selectExpression.Limit);

                if (newOffset != selectExpression.Offset || newLimit != selectExpression.Limit)
                {
                    // Create a new SelectExpression with the updated, inlined values
                    return selectExpression.Update(
                        selectExpression.Tables,
                        selectExpression.Predicate,
                        selectExpression.GroupBy,
                        selectExpression.Having,
                        selectExpression.Projection,
                        selectExpression.Orderings,
                        // !This order is important to match the expected OpenEdge SQL OFFSET/FETCH order
                        newOffset, // Use the potentially updated offset
                        newLimit); // Use the potentially updated limit
                }
            }

            return base.VisitExtension(extensionExpression);
        }

            /// <summary>
            /// Checks if the expression is a SqlParameterExpression and if so, replaces it with a SqlConstantExpression
            /// containing the actual parameter value.
            /// </summary>
            private SqlExpression? InlineParameter(SqlExpression? expression)
            {
                // We only care about SqlParameters for OFFSET/FETCH
                if (expression is not SqlParameterExpression parameterExpression)
                {
                    return expression; // Return as-is if it's already a constant or null
                }

                // Look up the value from the dictionary we built in the constructor
                if (_parameterValues.TryGetValue(parameterExpression.Name, out var value))
                {
                    // Create a new SqlConstantExpression with the value.
                    // The SQL Generator will now see this as a literal to be inlined.
                    // Handle null values by using default value (0 for OFFSET/FETCH)
                    return _sqlExpressionFactory.Constant(value ?? 0, parameterExpression.TypeMapping);
                }

                // Should not happen, but as a fallback, return the original parameter
                return parameterExpression;
            }
        }
    }
}