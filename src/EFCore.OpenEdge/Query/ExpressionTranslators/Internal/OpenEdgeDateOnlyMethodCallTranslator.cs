using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Translates DateOnly method calls to OpenEdge SQL functions.
    /// Handles methods like FromDateTime, ToDateTime, AddDays, AddMonths, AddYears, and comparisons.
    /// </summary>
    public class OpenEdgeDateOnlyMethodCallTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMethodCallTranslator
    {
        private readonly ISqlExpressionFactory _sqlExpressionFactory = sqlExpressionFactory;

        private static readonly MethodInfo _fromDateTimeMethod = typeof(DateOnly).GetRuntimeMethod(
            nameof(DateOnly.FromDateTime), [typeof(DateTime)])!;

        private static readonly MethodInfo _toDateTimeMethod = typeof(DateOnly).GetRuntimeMethod(
            nameof(DateOnly.ToDateTime), [typeof(TimeOnly)])!;

        private static readonly MethodInfo _toDateTimeMethodWithKind = typeof(DateOnly).GetRuntimeMethod(
            nameof(DateOnly.ToDateTime), [typeof(TimeOnly), typeof(DateTimeKind)])!;

        private static readonly MethodInfo _addDaysMethod = typeof(DateOnly).GetRuntimeMethod(
            nameof(DateOnly.AddDays), [typeof(int)])!;

        private static readonly MethodInfo _addMonthsMethod = typeof(DateOnly).GetRuntimeMethod(
            nameof(DateOnly.AddMonths), [typeof(int)])!;

        private static readonly MethodInfo _addYearsMethod = typeof(DateOnly).GetRuntimeMethod(
            nameof(DateOnly.AddYears), [typeof(int)])!;

        private static readonly MethodInfo _compareToMethod = typeof(DateOnly).GetRuntimeMethod(
            nameof(DateOnly.CompareTo), [typeof(DateOnly)])!;

        #nullable enable
        public virtual SqlExpression? Translate(
            SqlExpression? instance,
            MethodInfo method,
            IReadOnlyList<SqlExpression> arguments,
            IDiagnosticsLogger<Microsoft.EntityFrameworkCore.DbLoggerCategory.Query> logger)
        {
            // FromDateTime static method - extract date part from DateTime
            if (method.Equals(_fromDateTimeMethod))
            {
                // In OpenEdge, we can use DATE function to extract date part
                return _sqlExpressionFactory.Function(
                    "DATE",
                    [arguments[0]],
                    nullable: true,
                    argumentsPropagateNullability: [true],
                    typeof(DateOnly));
            }

            // Instance methods - check if instance is DateOnly
            if (instance?.Type != typeof(DateOnly) && instance?.Type != typeof(DateOnly?))
            {
                return null;
            }

            // ToDateTime method - combine date with time
            if (method.Equals(_toDateTimeMethod) || method.Equals(_toDateTimeMethodWithKind))
            {
                // For simplicity, cast DateOnly to DateTime (adds 00:00:00 time)
                // If a TimeOnly is provided in arguments[0], we'd need to combine them
                // For now, just cast the date to datetime
                return _sqlExpressionFactory.Convert(instance, typeof(DateTime));
            }

            // AddDays method
            // OpenEdge uses date arithmetic: date + integer (where integer represents days)
            // https://docs.progress.com/bundle/openedge-sql-reference/page/Date-arithmetic-expressions.html
            if (method.Equals(_addDaysMethod))
            {
                return _sqlExpressionFactory.Add(
                    instance,
                    arguments[0]);
            }

            // AddMonths method
            // OpenEdge has ADD_MONTHS function for adding months to a date
            // https://docs.progress.com/bundle/openedge-sql-reference/page/ADD_MONTHS.html
            if (method.Equals(_addMonthsMethod))
            {
                return _sqlExpressionFactory.Function(
                    "ADD_MONTHS",
                    [
                        instance,
                        arguments[0]
                    ],
                    nullable: true,
                    argumentsPropagateNullability: [true, true],
                    typeof(DateOnly));
            }

            // AddYears method
            // OpenEdge doesn't have ADD_YEARS, so we use ADD_MONTHS with years * 12
            if (method.Equals(_addYearsMethod))
            {
                // Multiply years by 12 to get months
                var monthsToAdd = _sqlExpressionFactory.Multiply(
                    arguments[0],
                    _sqlExpressionFactory.Constant(12));

                return _sqlExpressionFactory.Function(
                    "ADD_MONTHS",
                    [
                        instance,
                        monthsToAdd
                    ],
                    nullable: true,
                    argumentsPropagateNullability: [true, true],
                    typeof(DateOnly));
            }

            // CompareTo method - convert to standard comparison
            if (method.Equals(_compareToMethod))
            {
                // DateOnly.CompareTo returns -1, 0, or 1
                // We can use CASE WHEN to simulate this
                var comparison = _sqlExpressionFactory.Case(
                    [
                        new CaseWhenClause(
                            _sqlExpressionFactory.LessThan(instance, arguments[0]),
                            _sqlExpressionFactory.Constant(-1)),
                        new CaseWhenClause(
                            _sqlExpressionFactory.Equal(instance, arguments[0]),
                            _sqlExpressionFactory.Constant(0))
                    ],
                    _sqlExpressionFactory.Constant(1));

                return comparison;
            }

            return null;
        }
    }
}