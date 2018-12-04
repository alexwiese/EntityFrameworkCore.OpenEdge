using System;
using System.Collections.Generic;
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

        protected override void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected)
        {
        }

        protected override void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, ColumnModification columnModification)
        {
        }
        

        protected override void AppendValues(StringBuilder commandStringBuilder, IReadOnlyList<ColumnModification> operations)
        {
            if (operations.Count > 0)
            {
                commandStringBuilder
                    .Append("(")
                    .AppendJoin(
                        operations,
                        SqlGenerationHelper,
                        // Use '?' rather than named parameters
                        (sb, o, helper) => { sb.Append(o.IsWrite ? "?" : "DEFAULT"); })
                    .Append(")");
            }
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
            IReadOnlyList<ColumnModification> operations)
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

        protected override void AppendWhereCondition(StringBuilder commandStringBuilder, ColumnModification columnModification,
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
    }
}