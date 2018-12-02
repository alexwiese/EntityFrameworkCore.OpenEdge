using System;
using System.Data;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Storage
{
    public class OpenEdgeRelationalTypeMappingSource : SqlServerTypeMappingSource
    {
        private readonly DateTimeTypeMapping _datetime = new DateTimeTypeMapping("datetime", DbType.DateTime);
        private readonly DateTimeTypeMapping _date
            = new DateTimeTypeMapping("date", dbType: DbType.Date);

        public OpenEdgeRelationalTypeMappingSource(TypeMappingSourceDependencies dependencies, RelationalTypeMappingSourceDependencies relationalDependencies) : base(dependencies, relationalDependencies)
        {
        }

        protected override RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
        {
            var storeTypeName = mappingInfo.StoreTypeName;


            if (storeTypeName == "date")
            {
                return _date;
            }

            if (mappingInfo.ClrType == typeof(DateTime))
            {
                return _datetime;
            }

            return base.FindMapping(in mappingInfo);
        }
    }
}