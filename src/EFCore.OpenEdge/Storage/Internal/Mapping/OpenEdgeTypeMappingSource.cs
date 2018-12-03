using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal.Mapping
{
    public class OpenEdgeTypeMappingSource : RelationalTypeMappingSource
    {
        private const int VarcharMaxSize = 32000;

        private readonly DateTimeTypeMapping _datetime = new DateTimeTypeMapping("datetime", DbType.DateTime);
        private readonly DateTimeOffsetTypeMapping _datetimeOffset = new DateTimeOffsetTypeMapping("datetime-tz", DbType.DateTimeOffset);
        private readonly DateTimeTypeMapping _date = new DateTimeTypeMapping("date", DbType.Date);
        private readonly DateTimeTypeMapping _timeStamp = new DateTimeTypeMapping("timestamp", DbType.DateTime);
        private readonly TimeSpanTypeMapping _time = new TimeSpanTypeMapping("time", DbType.Time);

        private readonly OpenEdgeBoolTypeMapping _boolean = new OpenEdgeBoolTypeMapping();
        private readonly ShortTypeMapping _smallint = new ShortTypeMapping("smallint", DbType.Int16);
        private readonly ShortTypeMapping _tinyint = new ShortTypeMapping("tinyint", DbType.Byte);
        private readonly IntTypeMapping _integer = new IntTypeMapping("integer", DbType.Int32);
        private readonly LongTypeMapping _bigint = new LongTypeMapping("bigint");

        private readonly StringTypeMapping _char = new StringTypeMapping("char", DbType.String);
        private readonly StringTypeMapping _varchar = new StringTypeMapping("varchar", DbType.AnsiString);

        private readonly ByteArrayTypeMapping _binary = new ByteArrayTypeMapping("binary", DbType.Binary);

        private readonly FloatTypeMapping _float = new FloatTypeMapping("real");
        private readonly DoubleTypeMapping _double = new DoubleTypeMapping("double precision");
        private readonly DecimalTypeMapping _decimal = new DecimalTypeMapping("decimal");

        private readonly Dictionary<string, RelationalTypeMapping> _storeTypeMappings;
        private readonly Dictionary<Type, RelationalTypeMapping> _clrTypeMappings;

        public OpenEdgeTypeMappingSource(TypeMappingSourceDependencies dependencies, RelationalTypeMappingSourceDependencies relationalDependencies)
            : base(dependencies, relationalDependencies)
        {
            _clrTypeMappings
                = new Dictionary<Type, RelationalTypeMapping>
                {
                    { typeof(int), _integer },
                    { typeof(long), _bigint },
                    { typeof(DateTime), _datetime },
                    { typeof(bool), _boolean },
                    { typeof(byte), _tinyint },
                    { typeof(byte[]), _binary},
                    { typeof(double), _double },
                    { typeof(DateTimeOffset), _datetime },
                    { typeof(short), _smallint },
                    { typeof(float), _float },
                    { typeof(decimal), _decimal },
                    { typeof(TimeSpan), _time }
                };

            _storeTypeMappings
                = new Dictionary<string, RelationalTypeMapping>(StringComparer.OrdinalIgnoreCase)
                {
                    { "bigint", _bigint },
                    { "int64", _bigint },
                    { "binary varying", _binary },
                    { "raw", _binary },
                    { "binary", _binary },
                    { "bit", _boolean},
                    { "logical", _boolean},
                    { "char varying", _char },
                    { "char", _char },
                    { "character varying", _char },
                    { "character", _char },
                    { "date", _date },
                    { "datetime", _datetime },
                    { "datetime2", _datetime },
                    { "datetimeoffset", _datetimeOffset },
                    { "datetime-tz", _datetimeOffset },
                    { "dec", _decimal },
                    { "decimal", _decimal },
                    { "double precision", _double },
                    { "float", _double },
                    { "image", _binary },
                    { "int", _integer },
                    { "integer", _integer },
                    { "money", _decimal },
                    { "numeric", _decimal },
                    { "real", _float },
                    { "smalldatetime", _datetime },
                    { "smallint", _smallint},
                    { "short", _smallint},
                    { "smallmoney", _decimal },
                    { "text", _char},
                    { "time", _time },
                    { "timestamp", _timeStamp },
                    { "tinyint", _tinyint},
                    { "varbinary", _binary },
                    { "varchar", _varchar }
                };
        }

        protected override RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
        {
            return FindRawMapping(mappingInfo)?.Clone(mappingInfo);
        }

        private RelationalTypeMapping FindRawMapping(RelationalTypeMappingInfo mappingInfo)
        {
            var clrType = mappingInfo.ClrType;
            var storeTypeName = mappingInfo.StoreTypeName;
            var storeTypeNameBase = mappingInfo.StoreTypeNameBase;

            if (storeTypeName != null)
            {
                if (clrType == typeof(float)
                    && mappingInfo.Size != null
                    && mappingInfo.Size <= 24
                    && (storeTypeNameBase.Equals("float", StringComparison.OrdinalIgnoreCase)
                        || storeTypeNameBase.Equals("double precision", StringComparison.OrdinalIgnoreCase)))
                {
                    return _float;
                }

                if (_storeTypeMappings.TryGetValue(storeTypeName, out var mapping)
                    || _storeTypeMappings.TryGetValue(storeTypeNameBase, out mapping))
                {
                    return clrType == null
                           || mapping.ClrType == clrType
                        ? mapping
                        : null;
                }
            }

            if (clrType != null)
            {
                if (_clrTypeMappings.TryGetValue(clrType, out var mapping))
                {
                    return mapping;
                }

                if (clrType == typeof(string))
                {
                    var isAnsi = mappingInfo.IsUnicode == false;
                    var isFixedLength = mappingInfo.IsFixedLength == true;
                    var baseName = (isFixedLength ? "CHAR" : "VARCHAR");
                    var maxSize = VarcharMaxSize;

                    var size = (int?)(mappingInfo.Size ?? maxSize);
                    if (size > maxSize)
                    {
                        size = null;
                    }

                    var dbType = isAnsi
                        ? (isFixedLength ? DbType.AnsiStringFixedLength : DbType.AnsiString)
                        : (isFixedLength ? DbType.StringFixedLength : (DbType?)null);


                    return new StringTypeMapping(
                        baseName + "(" + (size == null ? "max" : size.ToString()) + ")",
                        dbType,
                        !isAnsi,
                        size);
                }
            }

            return null;
        }
    }
}