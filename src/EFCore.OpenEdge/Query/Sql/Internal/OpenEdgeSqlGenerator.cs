using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EntityFrameworkCore.OpenEdge.Extensions;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Query.Sql.Internal
{
    public class OpenEdgeSqlGenerator : DefaultQuerySqlGenerator
    {
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

            Sql.Append("?");

            return parameterExpression;
        }

        protected override Expression VisitConditional(ConditionalExpression conditionalExpression)
        {
            var visitConditional = base.VisitConditional(conditionalExpression);

            Sql.Append(@" FROM pub.""_File"" f WHERE f.""_File-Name"" = '_File'");

            return visitConditional;
        }

        public override Expression VisitExists(ExistsExpression existsExpression)
        {
            Sql.AppendLine(@"(SELECT 1 FROM pub.""_File"" f WHERE f.""_File-Name"" = '_File' AND EXISTS (");

            using (Sql.Indent())
            {
                Visit(existsExpression.Subquery);
            }

            Sql.Append(")) = 1");

            return existsExpression;
        }

        public override IRelationalCommand GenerateSql(IReadOnlyDictionary<string, object> parameterValues)
        {
            var relationalCommand = base.GenerateSql(parameterValues);
            //Console.WriteLine("===================");
            //Console.WriteLine(relationalCommand.CommandText);
            foreach (var parameterValue in parameterValues)
            {
              //  Console.WriteLine(parameterValue.Key + " " + parameterValue.Value);
            }
           // Console.WriteLine("===================");
            return relationalCommand;
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
                    Sql.Append(value.ToString());
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
