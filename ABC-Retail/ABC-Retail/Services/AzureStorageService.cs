using Microsoft.Extensions.Logging; // <- ADD THIS LINE
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Configuration;
using ABC_Retail.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace ABC_Retail.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ShareServiceClient _shareServiceClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureStorageService> _logger;

        public AzureStorageService(IConfiguration configuration, ILogger<AzureStorageService> logger)
        {
            logger = logger;
            _configuration = configuration;
            var connectionString = configuration.GetConnectionString("AzureStorage");
            _tableServiceClient = new TableServiceClient(connectionString);
            _blobServiceClient = new BlobServiceClient(connectionString);
            _queueServiceClient = new QueueServiceClient(connectionString);
            _shareServiceClient = new ShareServiceClient(connectionString);
        }

        // Customer methods
        public async Task<List<Customer>> GetCustomersAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("Customers");
            await tableClient.CreateIfNotExistsAsync();
            var customers = new List<Customer>();
            await foreach (var customer in tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }
            return customers;
        }

        public async Task<Customer?> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("Customers");
                var response = await tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return response.Value;
            }
            catch (Exception ex)
            {
                // This will show the real error in the Output window
                Console.WriteLine($"ERROR in GetCustomerAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            var tableClient = _tableServiceClient.GetTableClient("Customers");
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(customer);
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            var tableClient = _tableServiceClient.GetTableClient("Customers");
            await tableClient.UpdateEntityAsync(customer, Azure.ETag.All);
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient("Customers");
            await tableClient.DeleteEntityAsync(partitionKey, rowKey); // USE PARAMETERS
        }

        // Product methods
        public async Task<List<Product>> GetProductsAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("Products");
            await tableClient.CreateIfNotExistsAsync();
            var products = new List<Product>();
            await foreach (var product in tableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }
            return products;
        }

        public async Task<Product?> GetProductAsync(string partitionKey, string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient("Products");
            try
            {             
                var response = await tableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task AddProductAsync(Product product)
        {
            var tableClient = _tableServiceClient.GetTableClient("Products");
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            var tableClient = _tableServiceClient.GetTableClient("Products");
            await tableClient.UpdateEntityAsync(product, Azure.ETag.All);
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient("Products");
            await tableClient.DeleteEntityAsync(partitionKey, rowKey); // USE PARAMETERS
        }

        // Order methods
        public async Task<List<Order>> GetOrdersAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("Orders");
            await tableClient.CreateIfNotExistsAsync();
            var orders = new List<Order>();
            await foreach (var order in tableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }
            return orders;
        }

        public async Task<Order?> GetOrderAsync(string partitionKey, string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient("Orders");
            try
            {
                var response = await tableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task AddOrderAsync(Order order)
        {
            var tableClient = _tableServiceClient.GetTableClient("Orders");
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(order);
        }

        public async Task UpdateOrderAsync(Order order)
        {
            var tableClient = _tableServiceClient.GetTableClient("Orders");
            await tableClient.UpdateEntityAsync(order, Azure.ETag.All);
        }

        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient("Orders");
            await tableClient.DeleteEntityAsync(partitionKey, rowKey); // USE PARAMETERS
        }

        // Blob methods (IFormFile only - no Stream+string overloads)
        public async Task<string> UploadProductImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var containerName = _configuration["StorageSettings:ProductImagesContainer"];
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true);

            return blobClient.Uri.ToString();
        }

        public async Task<List<string>> ListProductImagesAsync()
        {
            var containerName = _configuration["StorageSettings:ProductImagesContainer"];
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            var images = new List<string>();
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                images.Add(blobItem.Name);
            }
            return images;
        }

        public async Task<byte[]> DownloadBlobAsync(string blobName)
        {
            var containerName = _configuration["StorageSettings:ProductImagesContainer"];
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadContentAsync();
            return response.Value.Content.ToArray();
        }

        // Queue methods
        public async Task EnqueueOrderMessageAsync(string message)
        {
            var queueName = _configuration["StorageSettings:OrderQueue"];
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(message);
        }

        public async Task<List<string>> PeekQueueMessagesAsync(int maxMessages = 5)
        {
            var queueName = _configuration["StorageSettings:OrderQueue"];
            var queueClient = _queueServiceClient.GetQueueClient(queueName);

            var messages = new List<string>();
            var response = await queueClient.PeekMessagesAsync(maxMessages);

            foreach (var message in response.Value)
            {
                messages.Add(message.MessageText);
            }
            return messages;
        }

        // File Share methods
        public async Task UploadContractAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return;

            var shareName = _configuration["StorageSettings:ContractsShare"];
            var shareClient = _shareServiceClient.GetShareClient(shareName);

            // Check if share exists first, don't just try to create it
            if (!await shareClient.ExistsAsync())
            {
                // If it doesn't exist, try to create it
                await shareClient.CreateAsync();
            }

            // Get root directory client
            var directoryClient = shareClient.GetRootDirectoryClient();

            // Check if directory exists first
            if (!await directoryClient.ExistsAsync())
            {
                // If it doesn't exist, try to create it
                await directoryClient.CreateAsync();
            }

            // Create a unique filename to avoid overwrites
            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{file.FileName}";
            var fileClient = directoryClient.GetFileClient(fileName);

            using var stream = file.OpenReadStream();

            // Create the file with the correct length
            await fileClient.CreateAsync(stream.Length);

            // Upload the content
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);
        }


        public async Task<List<string>> ListContractsAsync()
        {
            var shareName = _configuration["StorageSettings:ContractsShare"];
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            var files = new List<string>();

            // Check if the share exists before trying to list files
            if (await shareClient.ExistsAsync())
            {
                var directoryClient = shareClient.GetRootDirectoryClient();

                // Check if the directory exists
                if (await directoryClient.ExistsAsync())
                {
                    await foreach (var fileItem in directoryClient.GetFilesAndDirectoriesAsync())
                    {
                        if (!fileItem.IsDirectory)
                        {
                            files.Add(fileItem.Name);
                        }
                    }
                }
            }
            return files;
        }
    }
}