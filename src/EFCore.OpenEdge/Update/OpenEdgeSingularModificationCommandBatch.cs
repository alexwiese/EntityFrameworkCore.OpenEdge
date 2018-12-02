using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace EntityFrameworkCore.OpenEdge.Update
{
    public class OpenEdgeSingularModificationCommandBatch : SingularModificationCommandBatch
    {
        private readonly IRelationalCommandBuilderFactory _commandBuilderFactory;

        public OpenEdgeSingularModificationCommandBatch(IRelationalCommandBuilderFactory commandBuilderFactory, ISqlGenerationHelper sqlGenerationHelper, IUpdateSqlGenerator updateSqlGenerator, IRelationalValueBufferFactoryFactory valueBufferFactoryFactory) : base(commandBuilderFactory, sqlGenerationHelper, updateSqlGenerator, valueBufferFactoryFactory)
        {
            _commandBuilderFactory = commandBuilderFactory;
        }

        protected override RawSqlCommand CreateStoreCommand()
        {
            var commandBuilder = _commandBuilderFactory
                .Create()
                .Append(GetCommandText());

            var parameterValues = new Dictionary<string, object>(GetParameterCount());

            for (var commandIndex = 0; commandIndex < ModificationCommands.Count; commandIndex++)
            {
                var command = ModificationCommands[commandIndex];

                foreach (var columnModification in command.ColumnModifications
                    .OrderBy(cm => cm.IsCondition))
                {
                    if (columnModification.UseCurrentValueParameter)
                    {
                        commandBuilder.AddParameter(columnModification.ParameterName,
                            SqlGenerationHelper.GenerateParameterName(columnModification.ParameterName),
                            columnModification.Property);

                        parameterValues.Add(columnModification.ParameterName, columnModification.Value);
                    }

                    if (columnModification.UseOriginalValueParameter)
                    {
                        commandBuilder.AddParameter(columnModification.OriginalParameterName,
                            SqlGenerationHelper.GenerateParameterName(columnModification.OriginalParameterName),
                            columnModification.Property);

                        parameterValues.Add(columnModification.OriginalParameterName, columnModification.OriginalValue);
                    }
                }
            }

            return new RawSqlCommand(commandBuilder.Build(), parameterValues);
        }
    }
}