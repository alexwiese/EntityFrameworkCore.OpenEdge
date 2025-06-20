using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityFrameworkCore.OpenEdge.Extensions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace EntityFrameworkCore.OpenEdge.Update
{
    public class OpenEdgeUpdateSqlGenerator : UpdateSqlGenerator, IOpenEdgeUpdateSqlGenerator
    {
        public OpenEdgeUpdateSqlGenerator(UpdateSqlGeneratorDependencies dependencies) : base(dependencies)
        {
        }

        // OpenEdge workaround for limited concurrency support?
        protected override void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected)
        {
            commandStringBuilder
                .Append("1 = 1");
        }

        protected override void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, IColumnModification columnModification)
        {
            commandStringBuilder
                .Append("1 = 1");
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
            var mapping = property != null
                ? Dependencies.TypeMappingSource.FindMapping(property)
                : null;
            mapping = mapping ?? Dependencies.TypeMappingSource.GetMappingForValue(value);
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
        public override ResultSetMapping AppendInsertOperation(StringBuilder commandStringBuilder, IReadOnlyModificationCommand command,
            int commandPosition)
        {

            var name = command.TableName;
            var schema = command.Schema;
            var operations = command.ColumnModifications;

            var writeOperations = operations.Where(o => o.IsWrite)
                .Where(o => o.ColumnName != "rowid")
                .ToList();
                     
            AppendInsertCommand(commandStringBuilder, name, schema, writeOperations);

            // No RETURNING clause, because there's no way to get the generated id?
            return ResultSetMapping.NoResultSet;
        }

        // Update SQL Generation
        public override ResultSetMapping AppendUpdateOperation(StringBuilder commandStringBuilder, IReadOnlyModificationCommand command,
            int commandPosition)
        {
            var name = command.TableName;
            var schema = command.Schema;
            var operations = command.ColumnModifications;

            var writeOperations = operations.Where(o => o.IsWrite).ToList();
            var conditionOperations = operations.Where(o => o.IsCondition).ToList();

            AppendUpdateCommand(commandStringBuilder, name, schema, writeOperations, conditionOperations);

            return AppendSelectAffectedCountCommand(commandStringBuilder, name, schema, commandPosition);
        }
    }
}