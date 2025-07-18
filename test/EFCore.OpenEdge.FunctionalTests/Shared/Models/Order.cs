using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFCore.OpenEdge.FunctionalTests.Shared.Models
{
    [Table("ORDERS_TEST_PROVIDER", Schema = "PUB")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        // Navigation properties
        public virtual Customer Customer { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        
        public override string ToString()
        {
            return $"Order {{ Id: {Id}, CustomerId: {CustomerId}, OrderDate: {OrderDate}, TotalAmount: {TotalAmount}, Status: {Status} }}";
        }
    }
}
