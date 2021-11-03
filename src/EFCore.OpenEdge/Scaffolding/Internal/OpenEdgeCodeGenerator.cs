﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkCore.OpenEdge.Scaffolding.Internal
{
    public class OpenEdgeCodeGenerator : ProviderCodeGenerator
    {
        public OpenEdgeCodeGenerator(ProviderCodeGeneratorDependencies dependencies)
            : base(dependencies)
        {
        }

        public override MethodCallCodeFragment GenerateUseProvider(string connectionString, MethodCallCodeFragment providerOptions)
        {
            return new MethodCallCodeFragment(nameof(OpenEdgeDbContextOptionsBuilderExtensions.UseOpenEdge), connectionString);
        }
    }
}