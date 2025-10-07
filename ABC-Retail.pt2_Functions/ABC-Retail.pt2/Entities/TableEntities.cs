using Azure;
using Azure.Data.Tables;
using System;

namespace ABC_Retail.pt2.Functions.Entities
{
    public class CustomerEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public string? Name { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? ShippingAddress { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }

    public class ProductEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public double Price { get; set; }
        public int AvailableStock { get; set; }
        public string? ImageURL { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }

    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public string? CustomerId { get; set; }
        public string? ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Details { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
