using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Orders";
        public string RowKey { get; set; } = Guid.NewGuid().ToString(); // OrderId

        public string CustomerId { get; set; } = string.Empty; // RowKey of Customer
        public string Username { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty; // RowKey of Product
        public string ProductName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Cancelled

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
