using EntityFrameworkCore.OpenEdge.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;
using System.Linq.Expressions;

namespace EntityFrameworkCore.OpenEdge.Query.Sql.Internal
{
    public class OpenEdgeSqlGenerator : QuerySqlGenerator
    {
        private bool _existsConditional;

        public OpenEdgeSqlGenerator(QuerySqlGeneratorDependencies dependencies) : base(dependencies)
        {
        }

        protected override Expression VisitTable(TableExpression tableExpression)
        {
            var result = base.VisitTable(tableExpression);
            Sql.Append(" WITH (NOLOCK)");
            return result;
        }

        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            var parameterName = Dependencies.SqlGenerationHelper.GenerateParameterName(parameterExpression.Name);

            if (Sql.Parameters.All(p => p.InvariantName != parameterExpression.Name))
            {
                var typeMapping = Sql.TypeMappingSource.GetMapping(parameterExpression.Type);

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

        protected override Expression VisitExists(ExistsExpression existsExpression)
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

        protected override void GenerateTop(SelectExpression selectExpression)
        {
            if (selectExpression.Limit != null
                && selectExpression.Offset == null)
            {
                // OpenEdge doesn't allow braces around the limit
                Sql.Append("TOP ");

                Visit(selectExpression.Limit);

                Sql.Append(" ");
            }
        }
    }
}
