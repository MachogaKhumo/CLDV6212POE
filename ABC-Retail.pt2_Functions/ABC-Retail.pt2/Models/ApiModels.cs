using System;

namespace ABC_Retail.pt2.Functions.Models
{
    public class CustomerDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? ShippingAddress { get; set; }
    }

    public class ProductDto
    {
        public string? Id { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public double Price { get; set; }
        public int AvailableStock { get; set; }
        public string? ImageURL { get; set; }
    }

    public class CreateOrderRequest
    {
        public string? CustomerId { get; set; }
        public string? ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Details { get; set; }
    }

    public class OrderDto
    {
        public string? Id { get; set; }
        public string? CustomerId { get; set; }
        public string? ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset OrderDate { get; set; }
    }
}
