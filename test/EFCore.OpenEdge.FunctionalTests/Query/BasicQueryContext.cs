using Microsoft.EntityFrameworkCore;
using EFCore.OpenEdge.FunctionalTests.Query.Models;

namespace EFCore.OpenEdge.FunctionalTests.Query
{
    public class BasicQueryContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }

        public BasicQueryContext(DbContextOptions<BasicQueryContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure entities for OpenEdge
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("CUSTOMERS_TEST_PROVIDER", "PUB");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.City).HasMaxLength(50);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("PRODUCTS_TEST_PROVIDER", "PUB");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Description).HasMaxLength(500);
            });
        }
    }
}
