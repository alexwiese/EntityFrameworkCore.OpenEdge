using Microsoft.EntityFrameworkCore;

namespace EFCore.OpenEdge.FunctionalTests.TestUtilities
{
    // Minimal DbContext implementation
    public class OpenEdgeContext : DbContext
    {
        public OpenEdgeContext(DbContextOptions options) : base(options)
        {
        }
    }
}