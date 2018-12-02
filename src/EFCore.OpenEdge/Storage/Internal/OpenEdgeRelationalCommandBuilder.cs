using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal
{
    public class OpenEdgeRelationalCommandBuilder : RelationalCommandBuilder
    {
        public OpenEdgeRelationalCommandBuilder(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, IRelationalTypeMappingSource typeMappingSource) : base(logger, typeMappingSource)
        {
        }

        protected override IRelationalCommand BuildCore(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, string commandText, IReadOnlyList<IRelationalParameter> parameters)
        {
            return new OpenEdgeCommand(logger, commandText, parameters);
        }
    }
}