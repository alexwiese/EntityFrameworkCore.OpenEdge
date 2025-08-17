using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Translates string.Length property access to OpenEdge SQL length() function.
    /// </summary>
    public class OpenEdgeStringLengthTranslator : IMemberTranslator
    {
        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        private static readonly PropertyInfo _stringLengthProperty = typeof(string).GetRuntimeProperty(
            nameof(string.Length))!;

        public OpenEdgeStringLengthTranslator(ISqlExpressionFactory sqlExpressionFactory)
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
            // Check if this is a string.Length property access
            if (member.Equals(_stringLengthProperty))
            {
                // Translate to OpenEdge length() function
                return _sqlExpressionFactory.Function(
                    "LENGTH",
                    [instance!],
                    nullable: true,
                    argumentsPropagateNullability: [true],
                    returnType);
            }

            return null;
        }
    }
}