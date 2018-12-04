using System;
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

        protected override void GenerateTop(SelectExpression selectExpression)
        {
            if (selectExpression.Limit != null
                && selectExpression.Offset == null)
            {
                // OpenEdge doesn't allow braces around the limit
                Sql.Append("TOP ");

                if (selectExpression.Limit is ParameterExpression limitParameter
                    && ParameterValues.TryGetValue(limitParameter.Name, out var value))
                {
                    var typeMapping = Dependencies.TypeMappingSource.GetMapping(limitParameter.Type);

                    // OpenEdge does not support the user of parameters for TOP, so we use literal instead
                    Sql.Append(GenerateSqlLiteral(typeMapping, value));
                }
                else
                {
                    Visit(selectExpression.Limit);
                }

                Sql.Append(" ");
            }
        }

        protected override void GenerateLimitOffset(SelectExpression selectExpression)
        {
            if (selectExpression.Limit != null
                && selectExpression.Offset == null)
            {
                return;
            }

            var limit = selectExpression.Limit;
            var offset = selectExpression.Offset;

            // OpenEdge does not support the use of parameters for LIMIT/OFFSET
            // Map the parameter expressions to constant expressions instead
            if (selectExpression.Offset is ParameterExpression offsetParameter
                && ParameterValues.TryGetValue(offsetParameter.Name, out var value))
            {
                offset = Expression.Constant(value);
            }

            if (selectExpression.Limit is ParameterExpression limitParameter
                && ParameterValues.TryGetValue(limitParameter.Name, out value))
            {
                limit = Expression.Constant(value);
            }

            // Need to set limit to null first, to get around
            // the push subquery logic in the setters
            selectExpression.Limit = null;
            selectExpression.Offset = null;

            selectExpression.Offset = offset;
            selectExpression.Limit = limit;

            base.GenerateLimitOffset(selectExpression);
        }

        private string GenerateSqlLiteral(RelationalTypeMapping relationalTypeMapping, object value)
        {
            var mappingClrType = relationalTypeMapping?.ClrType.UnwrapNullableType();

            if (mappingClrType != null
                && (value == null
                    || mappingClrType.IsInstanceOfType(value)
                    || value.GetType().IsInteger()
                    && (mappingClrType.IsInteger()
                        || mappingClrType.IsEnum)))
            {
                if (value?.GetType().IsInteger() == true
                    && mappingClrType.IsEnum)
                {
                    value = Enum.ToObject(mappingClrType, value);
                }
            }
            else
            {
                relationalTypeMapping = Dependencies.TypeMappingSource.GetMappingForValue(value);
            }

            return relationalTypeMapping.GenerateSqlLiteral(value);
        }

    }
}
