using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal.Mapping
{
    public class OpenEdgeBoolTypeMapping : BoolTypeMapping
    {
        public OpenEdgeBoolTypeMapping() 
            : base("bit")
        {
        }

        protected OpenEdgeBoolTypeMapping(RelationalTypeMappingParameters parameters) 
            : base(parameters)
        {
        }
    }
}