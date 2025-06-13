using EntityFrameworkCore.OpenEdge.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkCore.OpenEdge.Scaffolding.Internal
{
    /// <summary>
    /// Code generator for OpenEdge database provider scaffolding.
    /// Updated for EF Core 3.0+ with new method signatures.
    /// </summary>
    public class OpenEdgeCodeGenerator : ProviderCodeGenerator
    {
        public OpenEdgeCodeGenerator(ProviderCodeGeneratorDependencies dependencies)
            : base(dependencies)
        {
        }
        
        public override MethodCallCodeFragment GenerateUseProvider(
            string connectionString, 
            MethodCallCodeFragment providerOptions)
        {
            return new MethodCallCodeFragment(
                nameof(OpenEdgeDbContextOptionsBuilderExtensions.UseOpenEdge), 
                connectionString,
                providerOptions);
        }
    }
}