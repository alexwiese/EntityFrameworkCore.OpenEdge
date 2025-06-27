using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFCore.OpenEdge.FunctionalTests.Query.Models
{
    [Table("CUSTOMERS_TEST_PROVIDER", Schema = "PUB")]
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string Email { get; set; }

        public int Age { get; set; }

        [MaxLength(50)]
        public string City { get; set; }

        public bool IsActive { get; set; }

        public override string ToString()
        {
            return $"Customer {{ Id: {Id}, Name: {Name}, Email: {Email}, Age: {Age}, City: {City}, IsActive: {IsActive} }}";
        }

    }
}
