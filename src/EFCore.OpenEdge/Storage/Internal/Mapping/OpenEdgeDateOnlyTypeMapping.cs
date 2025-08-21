using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;

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
            // Convert DateTime to DateOnly using DateOnly.FromDateTime
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
    }
}