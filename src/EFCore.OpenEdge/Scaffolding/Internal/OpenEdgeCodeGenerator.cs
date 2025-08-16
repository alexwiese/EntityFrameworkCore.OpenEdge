using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkCore.OpenEdge.Scaffolding.Internal
{
    /// <summary>
    /// Code generator for OpenEdge database provider scaffolding.
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
            // Get the MethodInfo for the UseOpenEdge extension method
            var useOpenEdgeMethod = typeof(OpenEdgeDbContextOptionsBuilderExtensions)
                .GetMethod(nameof(OpenEdgeDbContextOptionsBuilderExtensions.UseOpenEdge), 
                    new[] { typeof(DbContextOptionsBuilder), typeof(string), typeof(System.Action<>).MakeGenericType(typeof(object)) });
            
            if (useOpenEdgeMethod == null)
            {
                throw new InvalidOperationException("Could not find UseOpenEdge method");
            }
            
            return new MethodCallCodeFragment(
                useOpenEdgeMethod, 
                connectionString,
                providerOptions);
        }
    }
}