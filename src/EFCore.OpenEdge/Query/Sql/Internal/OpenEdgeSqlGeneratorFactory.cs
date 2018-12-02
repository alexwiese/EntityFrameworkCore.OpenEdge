using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;

namespace EntityFrameworkCore.OpenEdge.Query.Sql.Internal
{
    public class OpenEdgeSqlGeneratorFactory : QuerySqlGeneratorFactoryBase
    {
        public OpenEdgeSqlGeneratorFactory(
            QuerySqlGeneratorDependencies dependencies)
            : base(dependencies)
        {
        }

        public override IQuerySqlGenerator CreateDefault(SelectExpression selectExpression)
            => new OpenEdgeSqlGenerator(Dependencies, selectExpression);
    }
}