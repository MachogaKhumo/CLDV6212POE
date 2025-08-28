using ABC_Retail.Models;

namespace ABC_Retail.Services
{
    public interface IAzureStorageService
    {
        // Tables (Customer)
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string partitionKey, string rowKey);
        Task AddCustomerAsync(Customer customer);
        Task UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(string partitionKey, string rowKey);

        // Tables (Product)
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string partitionKey, string rowKey);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(string partitionKey, string rowKey);

        // Tables (Order)
        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string partitionKey, string rowKey);
        Task AddOrderAsync(Order order);
        Task UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(string partitionKey, string rowKey);

        // Blob (images & uploads)
        Task<string> UploadProductImageAsync(IFormFile file);
        Task<List<string>> ListProductImagesAsync();
        Task<byte[]> DownloadBlobAsync(string blobName);

        // Queue (order processing log)
        Task EnqueueOrderMessageAsync(string message);
        Task<List<string>> PeekQueueMessagesAsync(int maxMessages = 5);

        // File Share (contracts)
        Task UploadContractAsync(IFormFile file);
        Task<List<string>> ListContractsAsync();
    }
}
