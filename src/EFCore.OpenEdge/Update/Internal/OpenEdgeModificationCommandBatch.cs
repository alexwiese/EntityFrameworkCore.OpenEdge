using System.Linq;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.OpenEdge.Update.Internal
{
    /// <summary>
    /// OpenEdge-specific modification command batch that ensures parameters are ordered correctly
    /// to match the SQL generation order (write operations first, then condition operations).
    /// </summary>
    public class OpenEdgeModificationCommandBatch : SingularModificationCommandBatch
    {
        public OpenEdgeModificationCommandBatch(ModificationCommandBatchFactoryDependencies dependencies) 
            : base(dependencies)
        {
        }

        /// <summary>
        /// Adds parameters for all column modifications in the given modification command to the relational command
        /// being built for this batch. Parameters are ordered to match OpenEdge SQL generation order:
        /// 1. Write operations (SET clause parameters)
        /// 2. Condition operations (WHERE clause parameters)
        /// </summary>
        /// <param name="modificationCommand">The modification command for which to add parameters.</param>
        protected override void AddParameters(IReadOnlyModificationCommand modificationCommand)
        {
            var modifications = modificationCommand.StoreStoredProcedure is null
                ? modificationCommand.ColumnModifications
                : modificationCommand.ColumnModifications.Where(
                    c => c.Column is IStoreStoredProcedureParameter or IStoreStoredProcedureReturnValue);

            // Order parameters to match OpenEdge SQL generation:
            // 1. Write operations first (IsCondition = false), these go in SET clause
            // 2. Condition operations second (IsCondition = true), these go in WHERE clause
            var orderedModifications = modifications
                .OrderBy(cm => cm.IsCondition) // false (write operations) come before true (condition operations)
                .ToList();

            foreach (var columnModification in orderedModifications)
            {
                AddParameter(columnModification);
            }
        }
    }
}