using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal
{
    public class OpenEdgeRelationalCommandBuilderFactory : RelationalCommandBuilderFactory
    {
        public OpenEdgeRelationalCommandBuilderFactory(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, IRelationalTypeMappingSource typeMappingSource) : base(logger, typeMappingSource)
        {
        }

        protected override IRelationalCommandBuilder CreateCore(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger,
            IRelationalTypeMappingSource relationalTypeMappingSource)
        {
            return new OpenEdgeRelationalCommandBuilder(logger, relationalTypeMappingSource);
        }
    }
}