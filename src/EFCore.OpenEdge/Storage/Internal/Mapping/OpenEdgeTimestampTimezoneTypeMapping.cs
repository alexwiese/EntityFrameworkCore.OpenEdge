using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal.Mapping
{
    /// <summary>
    /// Custom DateTimeOffset type mapping for OpenEdge timestamp_timezone columns.
    ///
    /// OpenEdge returns timestamp_timezone columns as strings via ODBC (e.g., "2025-07-22 05:29:48.197-07:00"),
    /// but EF Core's default DateTimeOffset mapping expects a DateTimeOffset object, causing InvalidCastException.
    ///
    /// This mapping overrides the default behavior to:
    /// 1. Use GetString() to read the string value from the database
    /// 2. Parse the string to DateTimeOffset using DateTimeOffset.Parse()
    /// 3. Handle null values appropriately
    /// </summary>
    public class OpenEdgeTimestampTimezoneTypeMapping : DateTimeOffsetTypeMapping
    {
        private static readonly MethodInfo GetStringMethod
            = typeof(DbDataReader).GetRuntimeMethod(nameof(DbDataReader.GetString), [typeof(int)])!;

        public OpenEdgeTimestampTimezoneTypeMapping(string storeType, DbType? dbType = null)
            : base(storeType, dbType)
        {
        }

        protected OpenEdgeTimestampTimezoneTypeMapping(RelationalTypeMappingParameters parameters)
            : base(parameters)
        {
        }

        public override MethodInfo GetDataReaderMethod()
            => GetStringMethod;

        public override Expression CustomizeDataReaderExpression(Expression expression)
        {
            // OpenEdge returns timestamp_timezone columns as strings via ODBC
            // We read this as a string using 'GetStringMethod' and parse it to DateTimeOffset
            
            // Create the parsing method call: DateTimeOffset.Parse(string, IFormatProvider)
            var parseMethod = typeof(DateTimeOffset).GetMethod(
                nameof(DateTimeOffset.Parse),
                [typeof(string), typeof(IFormatProvider)])!;

            // Create the CultureInfo.InvariantCulture property access.
            // This simply tells us to not take into account different cultures for parsing the date and time.
            var invariantCulture = Expression.Property(
                null,
                typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!);

            // Return: DateTimeOffset.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture)
            return Expression.Call(
                null,
                parseMethod,
                expression,
                invariantCulture);
        }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new OpenEdgeTimestampTimezoneTypeMapping(parameters);

        protected override string GenerateNonNullSqlLiteral(object value)
        {
            // When EF Core needs to embed a literal value in a SQL query
            if (value is DateTimeOffset dateTimeOffset)
            {
                // Format as ISO 8601 with timezone offset that OpenEdge can understand
                return $"'{dateTimeOffset:yyyy-MM-dd HH:mm:ss.fffzzz}'";
            }

            return base.GenerateNonNullSqlLiteral(value);
        }

        protected override void ConfigureParameter(DbParameter parameter)
        {
            base.ConfigureParameter(parameter);
            
            // When DateTimeOffset is used as a parameter, format it as a string for OpenEdge
            if (parameter.Value is DateTimeOffset dateTimeOffset)
            {
                // Convert to ISO 8601 string format that OpenEdge expects
                parameter.Value = dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
                parameter.DbType = System.Data.DbType.String;
            }
        }
    }
}