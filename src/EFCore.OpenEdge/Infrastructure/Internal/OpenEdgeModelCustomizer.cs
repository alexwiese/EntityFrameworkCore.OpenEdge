using EntityFrameworkCore.OpenEdge.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkCore.OpenEdge.Infrastructure.Internal
{
    /// <summary>
    /// OpenEdge-specific model customizer that applies provider-specific configurations during model building.
    /// </summary>
    public class OpenEdgeModelCustomizer : RelationalModelCustomizer
    {
        public OpenEdgeModelCustomizer(ModelCustomizerDependencies dependencies) 
            : base(dependencies)
        {
        }

        /// <summary>
        /// Customizes the model by applying OpenEdge-specific configurations, including the default schema.
        /// </summary>
        /// <param name="modelBuilder">The model builder to customize.</param>
        /// <param name="context">The context for which the model is being built.</param>
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            // Apply the default schema from options if configured
            var openEdgeOptionsExtension = context.GetService<IDbContextOptions>()
                .FindExtension<OpenEdgeOptionsExtension>();
            
            if (openEdgeOptionsExtension != null)
            {
                var defaultSchema = openEdgeOptionsExtension.DefaultSchema;
                if (!string.IsNullOrEmpty(defaultSchema))
                {
                    modelBuilder.HasDefaultSchema(defaultSchema);
                }
            }

            // Call base implementation to ensure all relational customizations are applied
            base.Customize(modelBuilder, context);
        }
    }
}