using EntityFrameworkCore.OpenEdge.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.OpenEdge.Infrastructure.Internal
{
    public class OpenEdgeOptionsExtension : RelationalOptionsExtension
    {
        public bool IncludeSystemTables { get; private set; }

        protected override RelationalOptionsExtension Clone()
        {
            return new OpenEdgeOptionsExtension();
        }

        public override bool ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkOpenEdge();
            return true;
        }

        public OpenEdgeOptionsExtension IncludeSystemTablesInSchema()
        {
            var clone = (OpenEdgeOptionsExtension)Clone();
            clone.IncludeSystemTables = true;
            return clone;
        }
    }
}