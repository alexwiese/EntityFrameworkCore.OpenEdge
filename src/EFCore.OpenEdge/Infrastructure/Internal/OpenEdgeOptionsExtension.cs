using System.Collections.Generic;
using EntityFrameworkCore.OpenEdge.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

using EntityFrameworkCore.OpenEdge.Update.Internal;
using Microsoft.EntityFrameworkCore.Update;

namespace EntityFrameworkCore.OpenEdge.Infrastructure.Internal
{
    /// <summary>
    /// This instance gets added to the DbContextOptions internal collection.
    /// When DbContext is created, EF Core calls ApplyServices() and all the services are added to the service collection.
    /// </summary>
    public class OpenEdgeOptionsExtension : RelationalOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;
        
        public override DbContextOptionsExtensionInfo Info 
            => _info ?? (_info = new OpenExtensionInfo(this));

        protected override RelationalOptionsExtension Clone() 
            => new OpenEdgeOptionsExtension();

        public override void ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkOpenEdge();
            services.AddScoped<IModificationCommandBatchFactory, OpenEdgeModificationCommandBatchFactory>();
        }

        // ✅ Required nested class for EF Core 3.0+
        private sealed class OpenExtensionInfo : RelationalExtensionInfo
        {
            public OpenExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            public override bool IsDatabaseProvider => true;

            public override string LogFragment => "using OpenEdge";

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
                => debugInfo["OpenEdge"] = "1";
            
        }
    }
}