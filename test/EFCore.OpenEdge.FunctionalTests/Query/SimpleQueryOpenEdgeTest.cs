using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using EFCore.OpenEdge.FunctionalTests.TestUtilities;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public class
        OpenSimpleQueryOpenEdgeTest : SimpleQueryTestBase<OpenSimpleQueryOpenEdgeTest.OpenSimpleQueryOpenEdgeFixture>
    {
        public OpenSimpleQueryOpenEdgeTest(OpenSimpleQueryOpenEdgeFixture fixture) : base(fixture)
        {
        }

        public class OpenSimpleQueryOpenEdgeFixture : NorthwindQueryFixtureBase<NoopModelCustomizer>
        {
            protected override ITestStoreFactory TestStoreFactory => OpenEdgeTestStoreFactory.Instance;
        }
    }
}
