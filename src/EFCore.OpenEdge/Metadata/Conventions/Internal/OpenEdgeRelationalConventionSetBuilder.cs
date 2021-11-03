using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace EntityFrameworkCore.OpenEdge.Metadata.Conventions.Internal
{
    public class OpenEdgeRelationalConventionSetBuilder : RelationalConventionSetBuilder, IConventionSetBuilder
    {
        public OpenEdgeRelationalConventionSetBuilder(ProviderConventionSetBuilderDependencies dependencies, 
                                                      RelationalConventionSetBuilderDependencies relationalDependencies) : 
            base(dependencies, relationalDependencies)
        {
        }
    }
}
