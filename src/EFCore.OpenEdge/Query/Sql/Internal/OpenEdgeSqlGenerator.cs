using System.Linq;
using System.Linq.Expressions;
using EntityFrameworkCore.OpenEdge.Extensions;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Query.Sql.Internal
{
    public class OpenEdgeSqlGenerator : DefaultQuerySqlGenerator
    {
        private bool _existsConditional;

        public OpenEdgeSqlGenerator(QuerySqlGeneratorDependencies dependencies, SelectExpression selectExpression)
            : base(dependencies, selectExpression)
        {
        }
        
        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            var parameterName = SqlGenerator.GenerateParameterName(parameterExpression.Name);

            if (Sql.ParameterBuilder.Parameters
                .All(p => p.InvariantName != parameterExpression.Name))
            {
                var typeMapping
                    = Dependencies.TypeMappingSource.GetMapping(parameterExpression.Type);

                Sql.AddParameter(
                    parameterExpression.Name,
                    parameterName,
                    typeMapping,
                    parameterExpression.Type.IsNullableType());
            }

            // Named parameters not supported in the command text
            // Need to use '?' instead
            Sql.Append("?");

            return parameterExpression;
        }

        protected override Expression VisitConditional(ConditionalExpression conditionalExpression)
        {
            var visitConditional = base.VisitConditional(conditionalExpression);

            // OpenEdge requires that SELECT statements always include a table,
            // so we SELECT from the _File metaschema table that always exists,
            // selecting a single row that we know will always exist; the metaschema
            // record for the _File metaschema table itself.
            if (_existsConditional)
                Sql.Append(@" FROM pub.""_File"" f WHERE f.""_File-Name"" = '_File'");

            _existsConditional = false;

            return visitConditional;
        }

        public override Expression VisitExists(ExistsExpression existsExpression)
        {
            // OpenEdge does not support WHEN EXISTS, only WHERE EXISTS
            // We need to SELECT 1 using WHERE EXISTS, then compare
            // the result to 1 to satisfy the conditional.

            // OpenEdge requires that SELECT statements always include a table,
            // so we SELECT from the _File metaschema table that always exists,
            // selecting a single row that we know will always exist; the metaschema
            // record for the _File metaschema table itself.
            Sql.AppendLine(@"(SELECT 1 FROM pub.""_File"" f WHERE f.""_File-Name"" = '_File' AND EXISTS (");

            using (Sql.Indent())
            {
                Visit(existsExpression.Subquery);
            }

            Sql.Append(")) = 1");

            _existsConditional = true;

            return existsExpression;
        }
    }
}
