using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.OpenEdge.Extensions
{
    public class OpenEdgeOptionsExtension : RelationalOptionsExtension
    {
        protected override RelationalOptionsExtension Clone()
        {
            return new OpenEdgeOptionsExtension();
        }

        public override bool ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkOpenEdge();
            return true;
        }
    }
}