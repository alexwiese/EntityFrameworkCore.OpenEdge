using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.OpenEdge.Query.Sql.Internal
{
    public class OpenEdgeSqlGeneratorFactory : IQuerySqlGeneratorFactory
    {
        public QuerySqlGeneratorDependencies Dependencies { get; }

        public OpenEdgeSqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies) => Dependencies = dependencies;

        public QuerySqlGenerator Create()
        {
            var result = new OpenEdgeSqlGenerator(Dependencies);

            return result;
        }
    }
}
