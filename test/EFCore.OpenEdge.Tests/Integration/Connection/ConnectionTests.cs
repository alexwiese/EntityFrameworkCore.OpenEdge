using EFCore.OpenEdge.Tests.TestUtilities;
using Xunit;

namespace EFCore.OpenEdge.Tests.Integration.Connection
{
    [Trait("Category", TestCategories.Connection)]
    public class OpenConnectionTests : OpenEdgeTestBase
    {
        [Fact]
        public void Connection_CanOpenConnection()
        {
            // Arrange & Act & Assert
            TestBasicConnection(); // Uses the base class method
        }
    }
}