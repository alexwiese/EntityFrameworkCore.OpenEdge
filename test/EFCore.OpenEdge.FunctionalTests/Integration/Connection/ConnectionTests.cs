using EFCore.OpenEdge.FunctionalTests.TestUtilities;
using Xunit;

namespace EFCore.OpenEdge.FunctionalTests.Integration.Connection
{
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