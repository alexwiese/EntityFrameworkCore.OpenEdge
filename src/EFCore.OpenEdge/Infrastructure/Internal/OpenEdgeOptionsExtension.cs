using EntityFrameworkCore.OpenEdge.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.OpenEdge.Infrastructure.Internal
{
    /*
     * This instance gets added to the DbContextOptions internal collection.
     * When DbContext is created, EF Core calls ApplyServices() and all the services are added to the service collection.
     */
    public class OpenEdgeOptionsExtension : RelationalOptionsExtension
    {
        protected override RelationalOptionsExtension Clone()
        {
            return new OpenEdgeOptionsExtension();
        }

        public override bool ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkOpenEdge(); // Registers ALL OpenEdge services
            return true;
        }
    }
}