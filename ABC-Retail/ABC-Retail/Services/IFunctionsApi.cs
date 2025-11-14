using ABC_Retail.Models;

namespace ABC_Retail.Services
{
    public interface IFunctionsApi
    {
        // Customers
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string id);
        Task<Customer?> GetCustomerByUsernameAsync(string username); // NEW
        Task<bool> CreateCustomerAsync(Customer customer);
        Task<bool> UpdateCustomerAsync(string id, Customer customer);
        Task<bool> DeleteCustomerAsync(string id);

        // Products
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string id);
        Task<bool> CreateProductAsync(Product product, IFormFile? imageFile = null);
        Task<bool> UpdateProductAsync(string id, Product product, IFormFile? imageFile = null);
        Task<bool> DeleteProductAsync(string id);
        Task<string> UploadProductImageAsync(IFormFile imageFile);

        // Orders
        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string id);
        Task<List<Order>> GetOrdersByCustomerIdAsync(string customerId); // NEW
        Task<bool> CreateOrderAsync(Order order);
        Task<bool> UpdateOrderStatusAsync(string id, string status);
        Task<bool> DeleteOrderAsync(string id);
        Task<bool> QueueOrderMessageAsync(string message);

        // Uploads (proof of payment)
        Task<(bool Success, string? Error)> UploadProofOfPaymentAsync(IFormFile file, string orderId, string customerName);
    }
}