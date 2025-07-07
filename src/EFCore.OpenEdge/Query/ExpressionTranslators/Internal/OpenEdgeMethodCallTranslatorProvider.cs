using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Provides method call translators for OpenEdge.
    /// </summary>
    public class OpenEdgeMethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
    {
        public OpenEdgeMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies)
            : base(dependencies)
        {
            AddTranslators([
                new OpenEdgeStringMethodCallTranslator(dependencies.SqlExpressionFactory)
            ]);
        }
    }
}