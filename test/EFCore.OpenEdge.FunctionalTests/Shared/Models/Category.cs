using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFCore.OpenEdge.FunctionalTests.Shared.Models
{
    [Table("CATEGORIES_TEST_PROVIDER", Schema = "PUB")]
    public class Category
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; }

        // Navigation property for products
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
