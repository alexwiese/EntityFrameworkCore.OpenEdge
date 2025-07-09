using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Provides member translators for OpenEdge.
    /// </summary>
    public class OpenEdgeMemberTranslatorProvider : RelationalMemberTranslatorProvider
    {
        public OpenEdgeMemberTranslatorProvider(RelationalMemberTranslatorProviderDependencies dependencies)
            : base(dependencies)
        {
            AddTranslators([
                new OpenEdgeStringLengthTranslator(dependencies.SqlExpressionFactory)
            ]);
        }
    }
}