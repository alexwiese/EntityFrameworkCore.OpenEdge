using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityFrameworkCore.OpenEdge.Extensions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace EntityFrameworkCore.OpenEdge.Update
{
    public class OpenEdgeUpdateSqlGenerator : UpdateSqlGenerator
    {
        public OpenEdgeUpdateSqlGenerator(UpdateSqlGeneratorDependencies dependencies) : base(dependencies)
        {
        }

        private bool ShouldSkipConcurrencyCheck(IColumnModification columnModification)
        {
            // Add logic here to determine when to skip concurrency checks
            // This might depend on column type, table configuration, etc.
            // For now returning this placeholder value
            return false; 
        }


        // VALUES Clause Generation
        protected override void AppendValues(StringBuilder commandStringBuilder, string name, string schema, IReadOnlyList<IColumnModification> operations)
        {
            // OpenEdge preference for literals over parameters
            bool useLiterals = true;

            if (operations.Count > 0)
            {
                commandStringBuilder
                    .Append("(")
                    .AppendJoin(
                        operations,
                        SqlGenerationHelper,
                     
                        (sb, o, helper) =>
                        {
                            if (useLiterals)
                            {
                                // Direct value embedding
                                AppendSqlLiteral(sb, o.Value, o.Property);
                            }
                            else
                            {
                                // Use '?' rather than named parameters
                                AppendParameter(sb, o);
                            }
                        })
                    .Append(")");
            }
        }

        private void AppendParameter(StringBuilder commandStringBuilder, IColumnModification modification)
        {
            commandStringBuilder.Append(modification.IsWrite ? "?" : "DEFAULT");
        }

        private void AppendSqlLiteral(StringBuilder commandStringBuilder, object value, IProperty property)
        {
            // Handle DateTime values with OpenEdge-specific format
            if (value is DateTime dateTime)
            {
                commandStringBuilder.Append($"{{ ts '{dateTime:yyyy-MM-dd HH:mm:ss}' }}");
                return;
            }
            
            var mapping = property != null
                ? Dependencies.TypeMappingSource.FindMapping(property)
                : null;
                
            mapping ??= Dependencies.TypeMappingSource.GetMappingForValue(value);
            commandStringBuilder.Append(mapping.GenerateProviderValueSqlLiteral(value));
        }


        protected override void AppendUpdateCommandHeader(StringBuilder commandStringBuilder, string name, string schema,
            IReadOnlyList<IColumnModification> operations)
        {
            commandStringBuilder.Append("UPDATE ");
            SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, name, schema);
            commandStringBuilder.Append(" SET ")
                .AppendJoin(
                    operations,
                    SqlGenerationHelper,
                    (sb, o, helper) =>
                    {
                        helper.DelimitIdentifier(sb, o.ColumnName);
                        sb.Append(" = ");
                        if (!o.UseCurrentValueParameter)
                        {
                            AppendSqlLiteral(sb, o.Value, o.Property);
                        }
                        else
                        {
                            sb.Append("?");
                        }
                    });
        }

        // WHERE Clause Generation
        protected override void AppendWhereCondition(StringBuilder commandStringBuilder, IColumnModification columnModification,
            bool useOriginalValue)
        {
            // OpenEdge workaround for limited concurrency support
            // TODO: Check if this condition should be disabled (replaces the old AppendRowsAffectedWhereCondition and AppendIdentityWhereCondition logic)
            if (ShouldSkipConcurrencyCheck(columnModification))
            {
                commandStringBuilder.Append("1 = 1");
                return;
            }
            
            SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, columnModification.ColumnName);

            var parameterValue = useOriginalValue
                ? columnModification.OriginalValue
                : columnModification.Value;

            if (parameterValue == null)
            {
                base.AppendWhereCondition(commandStringBuilder, columnModification, useOriginalValue);
            }
            else
            {
                commandStringBuilder.Append(" = ");
                if (!columnModification.UseCurrentValueParameter
                    && !columnModification.UseOriginalValueParameter)
                {
                    base.AppendWhereCondition(commandStringBuilder, columnModification, useOriginalValue);
                }
                else
                {
                    commandStringBuilder.Append("?");
                }
            }
        }

        // Insert SQL Generation
        public override ResultSetMapping AppendInsertOperation(
            StringBuilder commandStringBuilder, 
            IReadOnlyModificationCommand command,
            int commandPosition,
            out bool requiresTransaction)
        {
            // TODO: Double check this?!
            requiresTransaction = false;

            var name = command.TableName;
            var schema = command.Schema;
            var operations = command.ColumnModifications;

            var writeOperations = operations.Where(o => o.IsWrite)
                .Where(o => o.ColumnName != "rowid")
                .ToList();
                     
            AppendInsertCommand(commandStringBuilder, name, schema, writeOperations, new List<IColumnModification>());
            return ResultSetMapping.NoResults;
        }

        // Update SQL Generation
        public override ResultSetMapping AppendUpdateOperation(
            StringBuilder commandStringBuilder, 
            IReadOnlyModificationCommand command,
            int commandPosition,
            out bool requiresTransaction)
        {
            // TODO: Double check this?!
            requiresTransaction = false;
            
            var name = command.TableName;
            var schema = command.Schema;
            var operations = command.ColumnModifications;

            var writeOperations = operations.Where(o => o.IsWrite).ToList();
            var conditionOperations = operations.Where(o => o.IsCondition).ToList();

            // Generate UPDATE command without RETURNING clause. EF Core internally uses sql statement like 'RETURNING 1' to verify that such operation succeeds, for example,
            // a query would look like: 'UPDATE products SET name = 'New Name' WHERE id = 123 RETURNING 1', however, OpenEdge does not support RETURNING clause, so we need to use a workaround to omit it
            AppendUpdateCommandHeader(commandStringBuilder, name, schema, writeOperations); // "UPDATE table SET column = ?"
            AppendWhereClause(commandStringBuilder, conditionOperations); // "WHERE condition"

            return ResultSetMapping.NoResults;
        }

        // Delete SQL Generation
        public override ResultSetMapping AppendDeleteOperation(
            StringBuilder commandStringBuilder, 
            IReadOnlyModificationCommand command,
            int commandPosition,
            out bool requiresTransaction)
        {
            // TODO: Double check this?!
            requiresTransaction = false;
            
            var name = command.TableName;
            var schema = command.Schema;
            var conditionOperations = command.ColumnModifications.Where(o => o.IsCondition).ToList();

            // Generate DELETE command without RETURNING clause. EF Core internally uses sql statement like 'RETURNING 1' to verify that such operation succeeds, for example,
            // a query would look like: 'UPDATE products SET name = 'New Name' WHERE id = 123 RETURNING 1', however, OpenEdge does not support RETURNING clause, so we need to use a workaround to omit it
            commandStringBuilder.Append("DELETE FROM ");
            SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, name, schema);
            AppendWhereClause(commandStringBuilder, conditionOperations);

            return ResultSetMapping.NoResults;
        }
    }
}