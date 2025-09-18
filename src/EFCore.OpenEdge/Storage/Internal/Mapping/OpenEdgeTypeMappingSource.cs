using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal.Mapping
{
    public class OpenEdgeTypeMappingSource : RelationalTypeMappingSource
    {
        private const int VarcharMaxSize = 32000;

        /**
         * DbType is a .NET enumeration that represents database data types in a provider-agnostic way.
         * It's used by ADO.NET - a modern data access model - (which EF Core uses under the hood)
         * to create database parameters and handle data conversion. When EF Core generates SQL and creates parameters,
         * it needs to tell the underlying ADO.NET provider what type of parameter it is.
         *
         *  // When EF Core generates SQL like this:
         *   SELECT * FROM Customers WHERE Age > @p0
         *
         *   // It needs to create an ADO.NET parameter:
         *  var parameter = command.CreateParameter();
         *  parameter.ParameterName = "@p0";
         *  parameter.Value = 25;
         *  parameter.DbType = DbType.Int32; // ← This tells ADO.NET how to handle the parameter
         *
         * Then the underlying provider (in this case ODBC) will:
         *  a) Format the parameter value correctly for the database
         *  b) Set the appropriate native database type
         *  c) Handle data conversion between .NET and the database
         */
        
        /*
         * Essentially, EF Core is the one calling type mapping methods, forming EF Core → ADO.NET → ODBC chain.
         *
         * When executing this query:
         *   var customer = context.Customers.Find(42);
         * 
         * EF Core:
         *   1) Calls FindMapping(typeof(int)) to get IntTypeMapping
         *   2) Uses mapping.DbType (DbType.Int32) to create parameter
         *   3) ADO.NET creates OdbcParameter with correct OdbcType
         *   4) ODBC driver sends properly formatted value to OpenEdge
         */
        private readonly DateTimeTypeMapping _datetime = new DateTimeTypeMapping("datetime", DbType.DateTime);
        private readonly DateTimeOffsetTypeMapping _datetimeOffset = new DateTimeOffsetTypeMapping("datetime-tz", DbType.DateTimeOffset);
        private readonly OpenEdgeTimestampTimezoneTypeMapping _timestampTimezone = new OpenEdgeTimestampTimezoneTypeMapping("timestamp_timezone", DbType.DateTimeOffset);
        private readonly DateTimeTypeMapping _timeStamp = new DateTimeTypeMapping("timestamp", DbType.DateTime);
        private readonly TimeSpanTypeMapping _time = new TimeSpanTypeMapping("time", DbType.Time);
        private readonly OpenEdgeDateOnlyTypeMapping _dateOnly = new OpenEdgeDateOnlyTypeMapping("date", DbType.Date);

        private readonly OpenEdgeBoolTypeMapping _boolean = new OpenEdgeBoolTypeMapping();
        private readonly ShortTypeMapping _smallint = new ShortTypeMapping("smallint", DbType.Int16);
        private readonly ByteTypeMapping _tinyint = new ByteTypeMapping("tinyint", DbType.Byte);
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
            // Mapping for code first scenarios.
            // Used when EF Core knows the .NET type and needs to determine the database type
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
                    { typeof(DateTimeOffset), _datetimeOffset  },
                    { typeof(short), _smallint },
                    { typeof(float), _float },
                    { typeof(decimal), _decimal },
                    { typeof(TimeSpan), _time },
                    { typeof(DateOnly), _dateOnly },
                };

            // Mapping for database first scenarios or explicit column types ([Column(TypeName = "decimal(18,2)")]).
            // Used when EF Core knows the database type and needs to determine the .NET type
            _storeTypeMappings
                = new Dictionary<string, RelationalTypeMapping>(StringComparer.OrdinalIgnoreCase)
                {
                    { "bigint", _bigint },
                    { "int64", _bigint },
                    { "binary varying", _binary },
                    { "raw", _binary },
                    { "binary", _binary },
                    { "blob", _binary },
                    { "bit", _boolean },
                    { "logical", _boolean },
                    { "char varying", _char },
                    { "char", _char },
                    { "character varying", _char },
                    { "character", _char },
                    { "clob", _char },
                    { "date", _dateOnly },
                    { "datetime", _datetime },
                    { "datetime2", _datetime },
                    { "datetimeoffset", _datetimeOffset },
                    { "datetime-tz", _datetimeOffset },
                    { "dec", _decimal },
                    { "decimal", _decimal },
                    { "double precision", _double },
                    { "double", _double },
                    { "float", _double },
                    { "image", _binary },
                    { "int", _integer },
                    { "integer", _integer },
                    { "money", _decimal },
                    { "numeric", _decimal },
                    { "real", _float },
                    { "recid", _char },
                    { "smalldatetime", _datetime },
                    { "smallint", _smallint},
                    { "short", _smallint},
                    { "smallmoney", _decimal },
                    { "text", _char},
                    { "time", _time },
                    { "timestamp", _timeStamp },
                    { "timestamp_timezone", _timestampTimezone },
                    { "timestamp-timezone", _timestampTimezone },
                    { "tinyint", _tinyint },
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
            // The .NET type (e.g., typeof(string))
            var clrType = mappingInfo.ClrType;
            
            // Full database type name (e.g., "varchar(100)")
            var storeTypeName = mappingInfo.StoreTypeName;
            
            // Base type name (e.g., "varchar")
            var storeTypeNameBase = mappingInfo.StoreTypeNameBase;

            // Database first or explicit column type scenario
            if (storeTypeName != null)
            {
                /*
                 * Handle float logic. Example case:
                 * 
                 * Handling case with explicit column type annotation
                 *   public class Product
                 *   {
                 *       [Column(TypeName = "float(24)")] // ← Store type specified
                 *       public float Precision { get; set; } // ← CLR type known
                 *   }
                 *
                 * EF Core calls FindRawMapping with:
                 *   clrType = typeof(float)
                 *   storeTypeName = "float(24)"
                 *   storeTypeNameBase = "float"
                 *   mappingInfo.Size = 24
                 */
                if (clrType == typeof(float)
                    && mappingInfo.Size != null
                    && mappingInfo.Size <= 24
                    && (storeTypeNameBase.Equals("float", StringComparison.OrdinalIgnoreCase)
                        || storeTypeNameBase.Equals("double precision", StringComparison.OrdinalIgnoreCase)))
                {
                    return _float; // Use single precision
                }
                
                // Otherwise it would use _double (double precision)

                // Try full name first: "varchar(100)"
                if (_storeTypeMappings.TryGetValue(storeTypeName, out var mapping)
                    // Then try base name: "varchar" 
                    || _storeTypeMappings.TryGetValue(storeTypeNameBase, out mapping))
                {
                    // Some kind of incompatibility check
                    return clrType == null
                           || mapping.ClrType == clrType
                        ? mapping
                        : null;
                }
            }

            // Code first scenario
            if (clrType != null)
            {
                if (_clrTypeMappings.TryGetValue(clrType, out var mapping))
                {
                    return mapping;
                }

                // Special handling for string types with size/length specifications
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