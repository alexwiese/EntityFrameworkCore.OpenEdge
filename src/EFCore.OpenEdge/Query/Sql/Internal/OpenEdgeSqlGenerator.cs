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
        private readonly IRelationalTypeMappingSource _typeMappingSource;
            
        public OpenEdgeSqlGenerator(
            QuerySqlGeneratorDependencies dependencies,
            IRelationalTypeMappingSource typeMappingSource
            ) : base(dependencies)
        {
            _typeMappingSource = typeMappingSource;
        }

        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            var parameterName = Dependencies.SqlGenerationHelper.GenerateParameterName(parameterExpression.Name);

            // Register the parameter for later binding
            if (Sql.Parameters
                .All(p => p.InvariantName != parameterExpression.Name))
            {
                var typeMapping
                    = _typeMappingSource.GetMapping(parameterExpression.Type);

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

        // TODO: Double check that this is still needed and create this functionality in an appropriate location
        // protected override Expression VisitExists(ExistsExpression existsExpression)
        // {
        //     // Your OpenEdge-specific EXISTS logic here
        //     // OpenEdge does not support WHEN EXISTS, only WHERE EXISTS
        //     // We need to SELECT 1 using WHERE EXISTS, then compare
        //     // the result to 1 to satisfy the conditional.
        //
        //     // OpenEdge requires that SELECT statements always include a table,
        //     // so we SELECT from the _File metaschema table that always exists,
        //     // selecting a single row that we know will always exist; the metaschema
        //     // record for the _File metaschema table itself.
        //     Sql.AppendLine(@"(SELECT 1 FROM pub.""_File"" f WHERE f.""_File-Name"" = '_File' AND EXISTS (");
        //
        //     using (Sql.Indent())
        //     {
        //         Visit(existsExpression.Subquery);
        //     }
        //
        //     Sql.Append(")) = 1");
        //
        //     _existsConditional = true;
        //
        //     return existsExpression;
        // }

        protected override void GenerateTop(SelectExpression selectExpression)
        {
            // OpenEdge: TOP clause cannot be combined with OFFSET/FETCH clauses
            // Only use TOP if there's no limit/offset that will be handled by GenerateLimitOffset
            // TOP is only used when there's a limit but no offset, and we're not using OFFSET/FETCH
            
            // Don't generate TOP - let GenerateLimitOffset handle all limit/offset cases
            // This avoids the conflict between TOP and FETCH clauses
        }

        protected override void GenerateLimitOffset(SelectExpression selectExpression)
        {
            // https://docs.progress.com/bundle/openedge-sql-reference/page/OFFSET-and-FETCH-clauses.html
            if (selectExpression.Offset != null || selectExpression.Limit != null)
            {
                if (selectExpression.Offset != null)
                {
                    Sql.AppendLine()
                        .Append("OFFSET ");

                    // OpenEdge requires literal values in OFFSET/FETCH, not parameters
                    Visit(selectExpression.Offset);

                    Sql.Append(" ROWS");
                }

                if (selectExpression.Limit != null)
                {
                    if (selectExpression.Offset == null)
                    {
                        Sql.AppendLine();
                    }
                    else
                    {
                        Sql.Append(" ");
                    }

                    // Use FETCH FIRST when no offset, FETCH NEXT when there is an offset
                    if (selectExpression.Offset == null)
                    {
                        Sql.Append("FETCH FIRST ");
                    }
                    else
                    {
                        Sql.Append("FETCH NEXT ");
                    }

                    // OpenEdge requires literal values in OFFSET/FETCH, not parameters
                    Visit(selectExpression.Limit);

                    Sql.Append(" ROWS ONLY");
                }
            }
        }

        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression)
        {
            // Handle COUNT(*) to cast result to INT to match EF Core expectations. This ensures that 'COUNT(*)' function is wrapped inside 'CAST (... AS INT)'.
            // The generated SQL will now be 'CAST(COUNT(*) AS INT)'
            // if (string.Equals(sqlFunctionExpression.Name, "COUNT", StringComparison.OrdinalIgnoreCase))
            // {
            //     Sql.Append("CAST(");
            //     base.VisitSqlFunction(sqlFunctionExpression);
            //     Sql.Append(" AS INT)");
            //     return sqlFunctionExpression;
            // }
            
            return base.VisitSqlFunction(sqlFunctionExpression);
        }

        protected override Expression VisitConstant(ConstantExpression constantExpression)
        {
            // Handle DateTime values with OpenEdge-specific format
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

        protected override Expression VisitProjection(ProjectionExpression projectionExpression)
        {
            // OpenEdge doesn't support boolean expressions directly in SELECT clauses (e.g., SELECT c.IsActive).
            // They must be wrapped in CASE statements: CASE WHEN condition THEN 1 ELSE 0 END
            if (projectionExpression.Expression.Type == typeof(bool) && 
                projectionExpression.Expression is SqlBinaryExpression binaryExpression)
            {
                Sql.Append("CASE WHEN ");
                Visit(binaryExpression);
                Sql.Append(" THEN 1 ELSE 0 END");
                
                if (!string.IsNullOrEmpty(projectionExpression.Alias))
                {
                    Sql.Append(" AS ");
                    Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(projectionExpression.Alias));
                }
                
                return projectionExpression;
            }
            
            // For non-boolean expressions, use the base implementation
            return base.VisitProjection(projectionExpression);
        }
    }
}
