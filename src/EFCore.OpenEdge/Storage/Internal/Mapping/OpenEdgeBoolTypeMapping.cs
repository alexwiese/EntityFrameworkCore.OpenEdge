using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal.Mapping
{
    public class OpenEdgeBoolTypeMapping : RelationalTypeMapping
    {
        public OpenEdgeBoolTypeMapping() 
            : base(new RelationalTypeMappingParameters(new CoreTypeMappingParameters(typeof(bool)), "logical",
                StoreTypePostfix.None, System.Data.DbType.Boolean))
        {
        }

        protected OpenEdgeBoolTypeMapping(RelationalTypeMappingParameters parameters) 
            : base(parameters)
        {
        }

        ///// <summary>
        /////     This API supports the Entity Framework Core infrastructure and is not intended to be used
        /////     directly from your code. This API may change or be removed in future releases.
        ///// </summary>
        //protected override void ConfigureParameter(DbParameter parameter)
        //{
        //    base.ConfigureParameter(parameter);

        //    if (Size.HasValue
        //        && Size.Value != -1)
        //    {
        //        parameter.Size = Size.Value;
        //    }
        //}

        protected override string GenerateNonNullSqlLiteral(object value)
            => (bool)value ? "1" : "0";
    }
}