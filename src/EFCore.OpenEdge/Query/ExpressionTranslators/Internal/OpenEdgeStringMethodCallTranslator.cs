using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Translates string method calls to OpenEdge SQL equivalents.
    /// </summary>
    public class OpenEdgeStringMethodCallTranslator : IMethodCallTranslator
    {
        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        private static readonly MethodInfo _stringContainsMethod = typeof(string).GetRuntimeMethod(
            nameof(string.Contains), [typeof(string)])!;

        private static readonly MethodInfo _stringStartsWithMethod = typeof(string).GetRuntimeMethod(
            nameof(string.StartsWith), [typeof(string)])!;

        private static readonly MethodInfo _stringEndsWithMethod = typeof(string).GetRuntimeMethod(
            nameof(string.EndsWith), [typeof(string)])!;

        public OpenEdgeStringMethodCallTranslator(ISqlExpressionFactory sqlExpressionFactory)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        #nullable enable
        public virtual SqlExpression? Translate(
            SqlExpression? instance,
            MethodInfo method,
            IReadOnlyList<SqlExpression> arguments,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (instance == null)
            {
                return null;
            }

            // Handle string.Contains(string)
            if (method.Equals(_stringContainsMethod))
            {
                return TranslateContains(instance, arguments[0]);
            }

            // Handle string.StartsWith(string)
            if (method.Equals(_stringStartsWithMethod))
            {
                return TranslateStartsWith(instance, arguments[0]);
            }

            // Handle string.EndsWith(string)
            if (method.Equals(_stringEndsWithMethod))
            {
                return TranslateEndsWith(instance, arguments[0]);
            }

            return null;
        }

        private SqlExpression TranslateContains(SqlExpression instance, SqlExpression argument)
        {
            // OpenEdge CONCAT only accepts 2 arguments, so we need to chain them
            // CONCAT('%', CONCAT(argument, '%')) to get '%argument%'
            var innerConcat = _sqlExpressionFactory.Function(
                "CONCAT",
                [argument, _sqlExpressionFactory.Constant("%")],
                nullable: true,
                argumentsPropagateNullability: [true, false],
                typeof(string));

            var pattern = _sqlExpressionFactory.Function(
                "CONCAT",
                [_sqlExpressionFactory.Constant("%"), innerConcat],
                nullable: true,
                argumentsPropagateNullability: [false, true],
                typeof(string));

            return _sqlExpressionFactory.Like(instance, pattern);
        }

        private SqlExpression TranslateStartsWith(SqlExpression instance, SqlExpression argument)
        {
            // For StartsWith, we only need one CONCAT: argument + '%'
            var pattern = _sqlExpressionFactory.Function(
                "CONCAT",
                [argument, _sqlExpressionFactory.Constant("%")],
                nullable: true,
                argumentsPropagateNullability: [true, false],
                typeof(string));

            return _sqlExpressionFactory.Like(instance, pattern);
        }

        private SqlExpression TranslateEndsWith(SqlExpression instance, SqlExpression argument)
        {
            // For EndsWith, we only need one CONCAT: '%' + argument
            var pattern = _sqlExpressionFactory.Function(
                "CONCAT",
                [_sqlExpressionFactory.Constant("%"), argument],
                nullable: true,
                argumentsPropagateNullability: [false, true],
                typeof(string));

            return _sqlExpressionFactory.Like(instance, pattern);
        }
    }
}