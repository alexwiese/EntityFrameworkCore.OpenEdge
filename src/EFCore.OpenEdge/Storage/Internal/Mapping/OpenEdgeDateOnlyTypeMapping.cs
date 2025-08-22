using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal.Mapping
{
    /// <summary>
    /// Custom DateOnly type mapping for OpenEdge databases.
    ///
    /// OpenEdge returns DATE columns as DateTime objects via ODBC, but EF Core's
    /// default DateOnly mapping may try to read them as strings, causing InvalidCastException.
    ///
    /// This mapping overrides the default behavior to:
    /// 1. Use GetDateTime() to read the DateTime value from the database
    /// 2. Convert the DateTime to DateOnly using DateOnly.FromDateTime()
    /// 3. Convert DateOnly parameters to DateTime for ODBC compatibility
    /// </summary>
    public class OpenEdgeDateOnlyTypeMapping : DateOnlyTypeMapping
    {
        private static readonly MethodInfo GetDateTimeMethod
            = typeof(DbDataReader).GetRuntimeMethod(nameof(DbDataReader.GetDateTime), [typeof(int)])!;

        public OpenEdgeDateOnlyTypeMapping(string storeType, DbType? dbType = null)
            : base(storeType, dbType)
        {
        }

        protected OpenEdgeDateOnlyTypeMapping(RelationalTypeMappingParameters parameters)
            : base(parameters)
        {
        }

        public override MethodInfo GetDataReaderMethod()
            => GetDateTimeMethod;

        public override Expression CustomizeDataReaderExpression(Expression expression)
        {
            // OpenEdge returns DATE columns as DateTime objects via ODBC, 
            // we read this as a DateTime using 'GetDateTimeMethod' and convert it to a DateOnly using DateOnly.FromDateTime()
            var fromDateTimeMethod = typeof(DateOnly).GetMethod(
                nameof(DateOnly.FromDateTime),
                [typeof(DateTime)])!;

            return Expression.Call(
                null,
                fromDateTimeMethod,
                expression);
        }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new OpenEdgeDateOnlyTypeMapping(parameters);

        protected override string GenerateNonNullSqlLiteral(object value)
        {
            // When EF Core needs to embed a literal value in a SQL query, it calls this method.
            // When DateOnly is used as a parameter, we need to handle the conversion.
            // We convert to DateTime for ODBC compatibility and format as ISO date string that ODBC can understand and process
            if (value is DateOnly dateOnly)
            {
                var dateTime = dateOnly.ToDateTime(TimeOnly.MinValue);
                return $"{{ ts '{dateTime:yyyy-MM-dd HH:mm:ss}' }}";
            }

            return base.GenerateNonNullSqlLiteral(value);
        }

        protected override void ConfigureParameter(DbParameter parameter)
        {
            base.ConfigureParameter(parameter);
            
            // When EF Core needs to prepare a parameter for execution against a database, it calls this method.
            // When DateOnly is used as a parameter, we need to handle the conversion.
            // We convert to DateTime for ODBC compatibility and format as ISO date string that ODBC can understand and process
            if (parameter.Value is DateOnly dateOnly)
            {
                parameter.Value = dateOnly.ToDateTime(TimeOnly.MinValue);
                parameter.DbType = System.Data.DbType.Date;
            }
        }
    }
}