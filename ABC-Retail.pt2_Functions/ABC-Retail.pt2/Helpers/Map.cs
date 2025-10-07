using ABC_Retail.pt2.Functions.Entities;
using ABC_Retail.pt2.Functions.Models;

namespace ABC_Retail.pt2.Functions.Helpers
{
    public static class Map
    {
        public static CustomerDto ToDto(this CustomerEntity e) => new()
        {
            Id = e.RowKey,
            Name = e.Name,
            Username = e.Username,
            Email = e.Email,
            ShippingAddress = e.ShippingAddress
        };

        public static ProductDto ToDto(this ProductEntity e) => new()
        {
            Id = e.RowKey,
            ProductName = e.ProductName,
            Description = e.Description,
            Price = e.Price,
            AvailableStock = e.AvailableStock,
            ImageURL = e.ImageURL
        };

        public static OrderDto ToDto(this OrderEntity e) => new()
        {
            Id = e.RowKey,
            CustomerId = e.CustomerId,
            ProductId = e.ProductId,
            Quantity = e.Quantity,
            Status = e.Status,
            OrderDate = e.OrderDate
        };
    }
}
