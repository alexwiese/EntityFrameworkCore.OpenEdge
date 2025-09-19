using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

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

            // Store the original query to check if we modified it
            var originalQuery = queryExpression;

            // Inline OFFSET/FETCH values. We must inline these, because that's what OpenEdge expects... not possible to parameterize.
            queryExpression = new OffsetValueInliningExpressionVisitor(Dependencies.SqlExpressionFactory, parametersValues).Visit(queryExpression);
            
            // If we inlined any OFFSET/FETCH parameters, we CANNOT cache this query
            // because it now contains hard-coded constants specific to this execution
            if (!ReferenceEquals(originalQuery, queryExpression))
            {
                canCache = false;
            }
            
            // Store the query before boolean conversion to check if it changes
            var beforeBooleanConversion = queryExpression;
            
            // Convert boolean parameters to integer values for OpenEdge compatibility
            queryExpression = new BooleanParameterConversionVisitor(Dependencies.SqlExpressionFactory, Dependencies.TypeMappingSource, parametersValues).Visit(queryExpression);
            
            // If we converted any boolean parameters, we also cannot cache
            if (!ReferenceEquals(beforeBooleanConversion, queryExpression))
            {
                canCache = false;
            }
            
            return queryExpression;
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

                    // By default, simple queries are generated with a single SelectExpression
                    // For example:
                    /**
                      SelectExpression
                        ├── Tables
                        ├── Offset: SqlParameterExpression("p0")
                        └── Limit: SqlParameterExpression("p1")
                    */

                    // However, when EF generates complex queries, it can create nested SelectExpressions
                    // This is a common pattern when using Skip/Take with nested queries
                    // For example:
                    /**
                      SelectExpression (outer)
                        ├── Tables
                        │   └── SelectExpression (inner subquery)
                        │       ├── Tables (could have more nested levels)
                        │       ├── Offset: SqlParameterExpression("p0")
                        │       └── Limit: SqlParameterExpression("p1")
                        ├── Offset: SqlParameterExpression("p2")
                        └── Limit: SqlParameterExpression("p3")
                    */

                    // Therefore, the following code ensures that all cases are handled by recursively visiting all tables to 
                    // make sure that all OFFSET/FETCH parameters are inlined as constants
                    
                    // First, recursively visit all tables to handle nested SelectExpressions
                    var tables = selectExpression.Tables.ToList();
                    var newTables = new List<TableExpressionBase>();
                    var tablesChanged = false;

                    foreach (var table in tables)
                    {
                        var visitedTable = Visit(table);
                        newTables.Add((TableExpressionBase) visitedTable);

                        if (visitedTable != table)
                        {
                            tablesChanged = true;
                        }
                    }

                    // Then, inline parameters for this SelectExpression's OFFSET/FETCH
                    var newOffset = InlineParameter(selectExpression.Offset);
                    var newLimit = InlineParameter(selectExpression.Limit);

                    // 'Re-build' if something has changed
                    if (tablesChanged || newOffset != selectExpression.Offset || newLimit != selectExpression.Limit)
                    {
                        // Create a new SelectExpression with the updated tables and inlined values
                        return selectExpression.Update(
                            tablesChanged ? newTables : selectExpression.Tables,
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

                // Look up the value from the dictionary of parameter values
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

        /// <summary>
        /// Visitor that converts boolean parameters in comparisons to integer values for OpenEdge compatibility.
        /// OpenEdge stores booleans as integers (0/1), so we need to convert boolean parameters accordingly.
        /// </summary>
        private sealed class BooleanParameterConversionVisitor : ExpressionVisitor
        {
            private readonly ISqlExpressionFactory _sqlExpressionFactory;
            private readonly IRelationalTypeMappingSource _typeMappingSource;
            #nullable enable
            private readonly IReadOnlyDictionary<string, object?> _parameterValues;
            private readonly RelationalTypeMapping? _intTypeMapping;

            public BooleanParameterConversionVisitor(
                ISqlExpressionFactory sqlExpressionFactory,
                IRelationalTypeMappingSource typeMappingSource,
                IReadOnlyDictionary<string, object?> parameterValues)
            {
                _sqlExpressionFactory = sqlExpressionFactory;
                _typeMappingSource = typeMappingSource;
                _parameterValues = parameterValues;
                _intTypeMapping = _typeMappingSource.FindMapping(typeof(int));
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

                // Handle binary expressions (comparisons)
                if (extensionExpression is SqlBinaryExpression sqlBinary)
                {
                    var transformed = TryTransformBooleanComparison(sqlBinary);
                    if (transformed != null)
                        return transformed;
                }

                return base.VisitExtension(extensionExpression);
            }

            /// <summary>
            /// Attempts to transform a boolean comparison to use integer values.
            /// This is needed because OpenEdge doesn't have proper boolean data type.
            /// We convert boolean values to integers (0/1) and then perform the comparison.
            /// </summary>
            private SqlExpression? TryTransformBooleanComparison(SqlBinaryExpression sqlBinary)
            {
                if (sqlBinary.OperatorType != ExpressionType.Equal && 
                    sqlBinary.OperatorType != ExpressionType.NotEqual)
                    return null;

                var left = sqlBinary.Left;
                var right = sqlBinary.Right;
                
                // Try to convert right side if left is boolean
                if (left.Type == typeof(bool))
                {
                    var intConstant = TryConvertBooleanToInteger(right);
                    if (intConstant != null)
                    {
                        return CreateComparison(sqlBinary.OperatorType, left, intConstant);
                    }
                }
                
                // Try to convert left side if right is boolean
                if (right.Type == typeof(bool))
                {
                    var intConstant = TryConvertBooleanToInteger(left);
                    if (intConstant != null)
                    {
                        return CreateComparison(sqlBinary.OperatorType, intConstant, right);
                    }
                }

                return null;
            }

            /// <summary>
            /// Creates a comparison expression based on the operator type.
            /// </summary>
            private SqlBinaryExpression CreateComparison(ExpressionType operatorType, SqlExpression left, SqlExpression right)
            {
                return operatorType == ExpressionType.Equal
                    ? (SqlBinaryExpression)_sqlExpressionFactory.Equal(left, right)
                    : (SqlBinaryExpression)_sqlExpressionFactory.NotEqual(left, right);
            }

            /// <summary>
            /// Converts a boolean SqlExpression (parameter or constant) to an integer SqlConstantExpression.
            /// Returns null if the expression cannot be converted.
            /// </summary>
            private SqlConstantExpression? TryConvertBooleanToInteger(SqlExpression expression)
            {
                // Handle boolean parameter
                if (expression is SqlParameterExpression paramExpr)
                {
                    if (_parameterValues.TryGetValue(paramExpr.Name, out var value) && value is bool boolValue)
                    {
                        return CreateIntegerConstant(boolValue);
                    }
                }
                // Handle boolean constant
                else if (expression is SqlConstantExpression { Value: bool constBoolValue })
                {
                    return CreateIntegerConstant(constBoolValue);
                }

                return null;
            }

            /// <summary>
            /// Creates an integer SqlConstantExpression from a boolean value.
            /// </summary>
            private SqlConstantExpression CreateIntegerConstant(bool boolValue)
            {
                var intValue = boolValue ? 1 : 0;
                return (SqlConstantExpression)_sqlExpressionFactory.Constant(intValue, _intTypeMapping);
            }
        }
    }
}