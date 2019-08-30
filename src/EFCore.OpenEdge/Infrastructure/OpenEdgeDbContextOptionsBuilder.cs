using EntityFrameworkCore.OpenEdge.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkCore.OpenEdge.Infrastructure
{
    public class OpenEdgeDbContextOptionsBuilder : RelationalDbContextOptionsBuilder<OpenEdgeDbContextOptionsBuilder, OpenEdgeOptionsExtension>
    {
        public OpenEdgeDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
            : base(optionsBuilder)
        {
        }

        public virtual OpenEdgeDbContextOptionsBuilder IncludeSystemTablesInSchema()
            => base.WithOption(o => o.IncludeSystemTablesInSchema());
    }
}