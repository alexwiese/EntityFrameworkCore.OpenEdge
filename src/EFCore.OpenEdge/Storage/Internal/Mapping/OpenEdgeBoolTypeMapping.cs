using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal.Mapping
{
    /// <summary>
    /// Custom boolean type mapping for OpenEdge databases.
    /// 
    /// OpenEdge stores boolean values as integers (0 = false, 1 = true) rather than 
    /// native boolean types. This causes issues when EF Core tries to read boolean 
    /// values using DbDataReader.GetBoolean(), which expects actual boolean values
    /// and throws InvalidCastException when encountering integers.
    /// 
    /// This mapping overrides the default behavior to:
    /// 1. Use GetInt32() to read the integer value from the database
    /// 2. Convert the integer (0/1) to boolean (false/true) in the generated expression
    /// </summary>
    public class OpenEdgeBoolTypeMapping : BoolTypeMapping
    {
        /// <summary>
        /// Method info for DbDataReader.GetInt32() used to read integer values
        /// instead of the default GetBoolean() method.
        /// </summary>
        private static readonly MethodInfo GetInt32Method
            = typeof(DbDataReader).GetRuntimeMethod(nameof(DbDataReader.GetInt32), [typeof(int)])!;

        public OpenEdgeBoolTypeMapping() 
            : base("bit")
        {
        }

        protected OpenEdgeBoolTypeMapping(RelationalTypeMappingParameters parameters) 
            : base(parameters)
        {
        }

        /// <summary>
        /// Overrides the default data reader method to use GetInt32() instead of GetBoolean().
        /// This is necessary because OpenEdge returns integer values (0/1) for boolean fields.
        /// </summary>
        /// <returns>MethodInfo for DbDataReader.GetInt32()</returns>
        public override MethodInfo GetDataReaderMethod()
            => GetInt32Method;

        /// <summary>
        /// Customizes the expression used to read boolean values from the database.
        /// Converts the integer value (0/1) returned by OpenEdge to a boolean value.
        /// 
        /// Generated expression: (int_value != 0)
        /// - 0 becomes false
        /// - Any non-zero value becomes true (following C convention)
        /// </summary>
        /// <param name="expression">The expression that reads the integer value</param>
        /// <returns>Expression that converts integer to boolean</returns>
        public override Expression CustomizeDataReaderExpression(Expression expression)
        {
            // Convert integer (0/1) to boolean (false/true)
            // Expression: (int_value != 0)
            return Expression.NotEqual(
                expression,
                Expression.Constant(0, typeof(int)));
        }

        /// <summary>
        /// Overrides the Clone method to ensure that cloned instances maintain the custom
        /// OpenEdge boolean behavior. Without this, EF Core would create a base BoolTypeMapping
        /// instance during cloning, losing the custom GetDataReaderMethod and CustomizeDataReaderExpression overrides.
        /// </summary>
        /// <param name="parameters">The mapping parameters for the cloned instance</param>
        /// <returns>A new OpenEdgeBoolTypeMapping instance with the same configuration</returns>
        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new OpenEdgeBoolTypeMapping(parameters);
    }
}