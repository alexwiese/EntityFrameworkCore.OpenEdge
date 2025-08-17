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
        private string _defaultSchema;
        
        public OpenEdgeOptionsExtension()
        {
        }
        
        protected OpenEdgeOptionsExtension(OpenEdgeOptionsExtension copyFrom) : base(copyFrom)
        {
            _defaultSchema = copyFrom._defaultSchema;
        }
        
        /// <summary>
        /// The default schema to use for tables when not explicitly specified.
        /// Defaults to "pub" if not set.
        /// </summary>
        public virtual string DefaultSchema => _defaultSchema ?? "pub";
        
        public override DbContextOptionsExtensionInfo Info 
            => _info ?? (_info = new OpenExtensionInfo(this));

        protected override RelationalOptionsExtension Clone() 
            => new OpenEdgeOptionsExtension(this);
        
        /// <summary>
        /// Returns a new instance with the specified default schema.
        /// </summary>
        /// <param name="defaultSchema">The default schema name to use.</param>
        /// <returns>A new OpenEdgeOptionsExtension instance with the specified default schema.</returns>
        public virtual OpenEdgeOptionsExtension WithDefaultSchema(string defaultSchema)
        {
            var clone = new OpenEdgeOptionsExtension(this);
            clone._defaultSchema = defaultSchema;
            return clone;
        }

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
            {
                debugInfo["OpenEdge"] = "1";
                
                var openEdgeExtension = (OpenEdgeOptionsExtension)Extension;
                debugInfo["OpenEdge:DefaultSchema"] = openEdgeExtension.DefaultSchema;
            }
            
        }
    }
}