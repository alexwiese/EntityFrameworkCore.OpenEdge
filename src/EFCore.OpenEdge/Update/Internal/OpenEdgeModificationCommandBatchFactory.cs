using System.Linq;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace EntityFrameworkCore.OpenEdge.Update.Internal
{
    public class OpenEdgeModificationCommandBatchFactory : IModificationCommandBatchFactory
    {
        private readonly ModificationCommandBatchFactoryDependencies _dependencies;

        public OpenEdgeModificationCommandBatchFactory(ModificationCommandBatchFactoryDependencies dependencies)
        {
            _dependencies = dependencies;
        }
        
        public virtual ModificationCommandBatch Create()
            => new OpenEdgeModificationCommandBatch(_dependencies);
    }
}