using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFCore.OpenEdge.FunctionalTests.Query.Models
{
    [Table("PRODUCTS_TEST_PROVIDER", Schema = "PUB")]
    public class Product
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        
        public int CategoryId { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; }
        
        public bool InStock { get; set; }
    }
}
