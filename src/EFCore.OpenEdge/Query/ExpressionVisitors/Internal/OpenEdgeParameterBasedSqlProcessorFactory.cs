using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    /// <summary>
    /// Factory for creating OpenEdgeParameterBasedSqlProcessor instances.
    /// Follows the standard EF Core factory pattern for dependency injection.
    /// </summary>
    public class OpenEdgeParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
    {
        private readonly RelationalParameterBasedSqlProcessorDependencies _dependencies;

        public OpenEdgeParameterBasedSqlProcessorFactory(RelationalParameterBasedSqlProcessorDependencies dependencies)
            => _dependencies = dependencies;

        /// <summary>
        /// Creates a new OpenEdgeParameterBasedSqlProcessor instance
        /// </summary>
        public RelationalParameterBasedSqlProcessor Create(RelationalParameterBasedSqlProcessorParameters parameters)
            => new OpenEdgeParameterBasedSqlProcessor(_dependencies, parameters);
    }
}