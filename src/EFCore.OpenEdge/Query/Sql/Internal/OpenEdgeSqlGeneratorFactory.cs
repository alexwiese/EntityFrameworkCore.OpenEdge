using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Query.Sql.Internal
{
    public class OpenEdgeSqlGeneratorFactory : IQuerySqlGeneratorFactory
    {
        private readonly QuerySqlGeneratorDependencies _dependencies;
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        public OpenEdgeSqlGeneratorFactory(
            QuerySqlGeneratorDependencies dependencies,
            IRelationalTypeMappingSource typeMappingSource)
        {
            _dependencies = dependencies;
            _typeMappingSource = typeMappingSource;
        }

        public QuerySqlGenerator Create()
        {
            var result = new OpenEdgeSqlGenerator(_dependencies, _typeMappingSource);

            return result;
        }
    }
}