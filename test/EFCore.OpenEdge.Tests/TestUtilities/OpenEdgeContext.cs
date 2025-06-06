using Microsoft.EntityFrameworkCore;

namespace EFCore.OpenEdge.Tests.TestUtilities
{
    // Minimal DbContext implementation
    public class OpenEdgeContext : DbContext
    {
        public OpenEdgeContext(DbContextOptions options) : base(options)
        {
        }
    }
}