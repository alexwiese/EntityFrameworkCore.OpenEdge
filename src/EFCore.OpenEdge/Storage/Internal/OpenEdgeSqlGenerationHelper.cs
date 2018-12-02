using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal
{
    public class OpenEdgeSqlGenerationHelper : RelationalSqlGenerationHelper, IOpenEdgeSqlGenerationHelper
    {
        public OpenEdgeSqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies)
            : base(dependencies)
        {
            
        }

        public override string StatementTerminator { get; } = "";
    }
}