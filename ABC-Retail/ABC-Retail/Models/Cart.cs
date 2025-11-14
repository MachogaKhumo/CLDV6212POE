using Azure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABC_Retail.Models
{
    [Table("Cart")]
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerUsername { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ProductId { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        // Azure Table Storage properties - EXCLUDE from Entity Framework
        [NotMapped]
        public string PartitionKey { get; set; } = "Cart";

        [NotMapped]
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        [NotMapped]
        public DateTimeOffset? Timestamp { get; set; }

        [NotMapped]
        public ETag ETag { get; set; }
    }
}