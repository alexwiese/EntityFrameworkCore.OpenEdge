using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace EntityFrameworkCore.OpenEdge.ValueGeneration
{
    public class OpenEdgeValueGeneratorSelector : RelationalValueGeneratorSelector
    {
        public OpenEdgeValueGeneratorSelector(ValueGeneratorSelectorDependencies dependencies) : base(dependencies)
        {
        }
    }
}