using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;

namespace EntityFrameworkCore.OpenEdge.Metadata.Conventions.Internal
{
    public class OpenEdgeRelationalConventionSetBuilder : RelationalConventionSetBuilder
    {
        public OpenEdgeRelationalConventionSetBuilder(RelationalConventionSetBuilderDependencies dependencies) : base(dependencies)
        {
        }
    }
}