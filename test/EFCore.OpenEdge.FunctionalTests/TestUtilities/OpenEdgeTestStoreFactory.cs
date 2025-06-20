using EntityFrameworkCore.OpenEdge.Extensions;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.OpenEdge.FunctionalTests.TestUtilities
{
    /// <summary>
    /// Factory for creating OpenEdge test stores.
    /// 
    /// DESIGN NOTE: This follows the standardized EF Core provider pattern where
    /// all database providers implement a factory, even if the factory is currently simple.
    ///
    /// This factory mostly just passes calls through to OpenEdgeTestStore.
    /// The real work happens in the TestStore class itself.
    /// </summary>
    public class OpenEdgeTestStoreFactory : RelationalTestStoreFactory
    {
        public static OpenEdgeTestStoreFactory Instance { get; } = new OpenEdgeTestStoreFactory();

        protected OpenEdgeTestStoreFactory()
        {
        }

        public override TestStore Create(string storeName)
            => OpenEdgeTestStore.Create(storeName);

        public override TestStore GetOrCreate(string storeName)
            => OpenEdgeTestStore.GetOrCreate(storeName);

        public override IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
            => serviceCollection.AddEntityFrameworkOpenEdge();
    }
}