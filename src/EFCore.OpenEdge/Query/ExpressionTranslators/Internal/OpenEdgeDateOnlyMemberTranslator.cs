using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Translates DateOnly member access to OpenEdge SQL functions.
    /// Handles properties like Year, Month, Day, DayOfYear, and DayOfWeek.
    /// </summary>
    public class OpenEdgeDateOnlyMemberTranslator : IMemberTranslator
    {
        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        private static readonly PropertyInfo _yearProperty = typeof(DateOnly).GetRuntimeProperty(nameof(DateOnly.Year))!;
        private static readonly PropertyInfo _monthProperty = typeof(DateOnly).GetRuntimeProperty(nameof(DateOnly.Month))!;
        private static readonly PropertyInfo _dayProperty = typeof(DateOnly).GetRuntimeProperty(nameof(DateOnly.Day))!;
        private static readonly PropertyInfo _dayOfYearProperty = typeof(DateOnly).GetRuntimeProperty(nameof(DateOnly.DayOfYear))!;
        private static readonly PropertyInfo _dayOfWeekProperty = typeof(DateOnly).GetRuntimeProperty(nameof(DateOnly.DayOfWeek))!;

        public OpenEdgeDateOnlyMemberTranslator(ISqlExpressionFactory sqlExpressionFactory)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        #nullable enable
        public virtual SqlExpression? Translate(
            SqlExpression? instance,
            MemberInfo member,
            Type returnType,
            IDiagnosticsLogger<Microsoft.EntityFrameworkCore.DbLoggerCategory.Query> logger)
        {
            if (instance?.Type != typeof(DateOnly) && instance?.Type != typeof(DateOnly?))
            {
                return null;
            }

            // Year property
            if (member.Equals(_yearProperty))
            {
                return _sqlExpressionFactory.Function(
                    "YEAR",
                    [instance],
                    nullable: true,
                    argumentsPropagateNullability: [true],
                    returnType);
            }

            // Month property
            if (member.Equals(_monthProperty))
            {
                return _sqlExpressionFactory.Function(
                    "MONTH",
                    [instance],
                    nullable: true,
                    argumentsPropagateNullability: [true],
                    returnType);
            }

            // Day property - OpenEdge uses DAYOFMONTH instead of DAY
            if (member.Equals(_dayProperty))
            {
                return _sqlExpressionFactory.Function(
                    "DAYOFMONTH",
                    [instance],
                    nullable: true,
                    argumentsPropagateNullability: [true],
                    returnType);
            }

            // DayOfYear property
            if (member.Equals(_dayOfYearProperty))
            {
                return _sqlExpressionFactory.Function(
                    "DAYOFYEAR",
                    [instance],
                    nullable: true,
                    argumentsPropagateNullability: [true],
                    returnType);
            }

            // DayOfWeek property - OpenEdge uses DAYOFWEEK which returns 1-7 (Sunday=1)
            // .NET DayOfWeek enum is 0-6 (Sunday=0), so we need to subtract 1
            if (member.Equals(_dayOfWeekProperty))
            {
                var dayOfWeekFunc = _sqlExpressionFactory.Function(
                    "DAYOFWEEK",
                    [instance],
                    nullable: true,
                    argumentsPropagateNullability: [true],
                    typeof(int));

                // Subtract 1 to match .NET DayOfWeek enum values
                return _sqlExpressionFactory.Subtract(
                    dayOfWeekFunc,
                    _sqlExpressionFactory.Constant(1));
            }

            return null;
        }
    }
}