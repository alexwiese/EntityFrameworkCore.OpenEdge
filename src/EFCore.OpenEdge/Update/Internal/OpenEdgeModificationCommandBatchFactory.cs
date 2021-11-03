using Microsoft.EntityFrameworkCore.Update;

namespace EntityFrameworkCore.OpenEdge.Update.Internal
{
    public class OpenEdgeModificationCommandBatchFactory : IModificationCommandBatchFactory
    {
        private readonly ModificationCommandBatchFactoryDependencies _dependencies;

        public OpenEdgeModificationCommandBatchFactory(ModificationCommandBatchFactoryDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        public virtual ModificationCommandBatch Create() => new OpenEdgeSingularModificationCommandBatch(_dependencies);
    }
}
