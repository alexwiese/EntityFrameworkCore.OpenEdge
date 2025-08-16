using System;
using System.Data.Common;

namespace EntityFrameworkCore.OpenEdge.Extensions
{
    public static class OpenEdgeDataReaderExtensions
    {
        /*
         * GetValueOrDefault is NOT a native method on DbDataReader.
         * The compiler searches for extension methods in scope that match
         *   a) First parameter type: DbDataReader (matches reader.Object)
         *   b) Method name: GetValueOrDefault 
         * 
         * The compiler transforms this call:
         *   // How it is called:
         *   reader.Object.GetValueOrDefault<int>("TestColumn")
         *
         *   // What the compiler actually calls:
         *   OpenEdgeDataReaderExtensions.GetValueOrDefault<int>(reader.Object, "TestColumn")
         */
        public static T GetValueOrDefault<T>(this DbDataReader reader, string name)
        {
            var idx = reader.GetOrdinal(name);
            return reader.IsDBNull(idx)
                ? default
                : (T)GetValue<T>(reader.GetValue(idx));
        }

        public static T GetValueOrDefault<T>(this DbDataRecord record, string name)
        {
            var idx = record.GetOrdinal(name);
            return record.IsDBNull(idx)
                ? default
                : (T)GetValue<T>(record.GetValue(idx));
        }

        private static object GetValue<T>(object valueRecord)
        {
            switch (typeof(T).Name)
            {
                case nameof(Int32):
                    return Convert.ToInt32(valueRecord);
                case nameof(Boolean):
                    return Convert.ToBoolean(valueRecord);
                default:
                    return valueRecord;
            }
        }
    }
}