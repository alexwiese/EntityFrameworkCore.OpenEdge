using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    /// <summary>
    /// Factory for creating QuerySqlGenerator instances for OpenEdge.
    /// Generates the final SQL from the expression tree.
    /// </summary>
    public class OpenEdgeQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
    {
        private readonly QuerySqlGeneratorDependencies _dependencies;

        public OpenEdgeQuerySqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        public virtual QuerySqlGenerator Create()
            => new OpenEdgeQuerySqlGenerator(_dependencies);
    }
}