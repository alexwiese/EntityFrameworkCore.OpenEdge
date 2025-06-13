using System;
using System.Linq;
using System.Linq.Expressions;
using EntityFrameworkCore.OpenEdge.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Query.Sql.Internal
{
    public class OpenEdgeSqlGenerator : QuerySqlGenerator
    {
        private bool _existsConditional;

        public OpenEdgeSqlGenerator(QuerySqlGeneratorDependencies dependencies) : base(dependencies)
        {
        }

        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            var parameterName = Dependencies.SqlGenerationHelper.GenerateParameterName(parameterExpression.Name);

            // Register the parameter for later binding
            if (Sql.Parameters
                .All(p => p.InvariantName != parameterExpression.Name))
            {
                var typeMapping
                    = Sql.TypeMappingSource.GetMapping(parameterExpression.Type);

                /*
                 * What this essentially means is that a standard SQL query like this:
                 *   WHERE Name = @p0 AND Age = @p1
                 *
                 * Needs to be converted to this (for OpenEdge): 
                 *   WHERE Name = ? AND Age = ?
                 *
                 * The parameters are still tracked internally, but the SQL uses positional placeholders.
                 */
                Sql.AddParameter(
                    parameterExpression.Name,
                    parameterName,
                    typeMapping,
                    parameterExpression.Type.IsNullableType());
            }

            // Named parameters not supported in the command text
            // Need to use '?' instead
            Sql.Append("?"); // This appears to be OpenEdge specific!

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

        protected override Expression VisitConstant(ConstantExpression constantExpression)
        {
            if ((constantExpression.Type == typeof(DateTime) || constantExpression.Type == typeof(DateTime?))
                && constantExpression.Value != null)
            {
                var dateTime = (DateTime)constantExpression.Value;
                Sql.Append($"{{ ts '{dateTime:yyyy-MM-dd HH:mm:ss}' }}");
            }
            else
                base.VisitConstant(constantExpression);
            
            return constantExpression;
        }
    }
}
