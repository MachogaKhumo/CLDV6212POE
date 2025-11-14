using ABC_Retail.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ABC_Retail.Services
{
    public class FunctionsApiClient : IFunctionsApi
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public FunctionsApiClient(HttpClient http)
        {
            _http = http;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // ---------- Customers ----------
        public async Task<List<Customer>> GetCustomersAsync()
        {
            var resp = await _http.GetAsync("customers");
            if (!resp.IsSuccessStatusCode) return new List<Customer>();
            var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<Customer>>(stream, _jsonOptions) ?? new();
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            var resp = await _http.GetAsync($"customers/{id}");
            if (!resp.IsSuccessStatusCode) return null;
            var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Customer>(stream, _jsonOptions);
        }

        // NEW: Get customer by username
        public async Task<Customer?> GetCustomerByUsernameAsync(string username)
        {
            try
            {
                var customers = await GetCustomersAsync();
                return customers.FirstOrDefault(c => c.Username == username);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> CreateCustomerAsync(Customer customer)
        {
            var resp = await _http.PostAsJsonAsync("customers", customer, _jsonOptions);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateCustomerAsync(string id, Customer customer)
        {
            var resp = await _http.PutAsJsonAsync($"customers/{id}", customer, _jsonOptions);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteCustomerAsync(string id)
        {
            var resp = await _http.DeleteAsync($"customers/{id}");
            return resp.IsSuccessStatusCode;
        }

        // ---------- Products ----------
        public async Task<List<Product>> GetProductsAsync()
        {
            var resp = await _http.GetAsync("products");
            if (!resp.IsSuccessStatusCode) return new List<Product>();
            var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<Product>>(stream, _jsonOptions) ?? new();
        }

        public async Task<Product?> GetProductAsync(string id)
        {
            var resp = await _http.GetAsync($"products/{id}");
            if (!resp.IsSuccessStatusCode) return null;
            var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Product>(stream, _jsonOptions);
        }

        public async Task<bool> CreateProductAsync(Product product, IFormFile? imageFile = null)
        {
            if (imageFile == null)
            {
                var resp = await _http.PostAsJsonAsync("products", product, _jsonOptions);
                return resp.IsSuccessStatusCode;
            }

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(product.ProductName ?? ""), "ProductName");
            content.Add(new StringContent(product.Description ?? ""), "Description");
            content.Add(new StringContent(product.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
            content.Add(new StringContent(product.StockAvailable.ToString()), "AvailableStock");

            var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms);
            ms.Position = 0;
            var fileContent = new StreamContent(ms);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "ImageFile", imageFile.FileName);

            var respMultipart = await _http.PostAsync("products", content);
            return respMultipart.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateProductAsync(string id, Product product, IFormFile? imageFile = null)
        {
            if (imageFile == null)
            {
                var resp = await _http.PutAsJsonAsync($"products/{id}", product, _jsonOptions);
                return resp.IsSuccessStatusCode;
            }

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(product.ProductName ?? ""), "ProductName");
            content.Add(new StringContent(product.Description ?? ""), "Description");
            content.Add(new StringContent(product.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
            content.Add(new StringContent(product.StockAvailable.ToString()), "AvailableStock");

            var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms);
            ms.Position = 0;
            var fileContent = new StreamContent(ms);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "ImageFile", imageFile.FileName);

            var respMultipart = await _http.PutAsync($"products/{id}", content);
            return respMultipart.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            var resp = await _http.DeleteAsync($"products/{id}");
            return resp.IsSuccessStatusCode;
        }

        public async Task<string> UploadProductImageAsync(IFormFile imageFile)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms);
                ms.Position = 0;
                var fileContent = new StreamContent(ms);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
                content.Add(fileContent, "image", imageFile.FileName);

                var response = await _http.PostAsync("uploads/product-image", content);

                if (response.IsSuccessStatusCode)
                {
                    var imageUrl = await response.Content.ReadAsStringAsync();
                    return imageUrl.Trim('"');
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading product image via Functions API: {ex.Message}");
            }

            return string.Empty;
        }

        // ---------- Orders ----------
        public async Task<List<Order>> GetOrdersAsync()
        {
            var resp = await _http.GetAsync("orders");
            if (!resp.IsSuccessStatusCode) return new List<Order>();
            var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<Order>>(stream, _jsonOptions) ?? new();
        }

        public async Task<Order?> GetOrderAsync(string id)
        {
            var resp = await _http.GetAsync($"orders/{id}");
            if (!resp.IsSuccessStatusCode) return null;
            var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Order>(stream, _jsonOptions);
        }

        // NEW: Get orders by customer ID
        public async Task<List<Order>> GetOrdersByCustomerIdAsync(string customerId)
        {
            try
            {
                var orders = await GetOrdersAsync();
                return orders.Where(o => o.CustomerId == customerId).ToList();
            }
            catch
            {
                return new List<Order>();
            }
        }

        public async Task<bool> CreateOrderAsync(Order order)
        {
            var resp = await _http.PostAsJsonAsync("orders", order, _jsonOptions);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateOrderStatusAsync(string id, string status)
        {
            var payload = new { status };
            var resp = await _http.PutAsJsonAsync($"orders/{id}/status", payload, _jsonOptions);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteOrderAsync(string id)
        {
            var resp = await _http.DeleteAsync($"orders/{id}");
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> QueueOrderMessageAsync(string message)
        {
            try
            {
                var payload = new { message };
                var response = await _http.PostAsJsonAsync("queue-order", payload, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error queueing message: {ex.Message}");
                return false;
            }
        }

        // ---------- Uploads (proof of payment) ----------
        public async Task<(bool Success, string? Error)> UploadProofOfPaymentAsync(IFormFile file, string orderId, string customerName)
        {
            using var content = new MultipartFormDataContent();
            var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            var streamContent = new StreamContent(ms);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            content.Add(streamContent, "ProofOfPayment", file.FileName);
            content.Add(new StringContent(orderId ?? ""), "OrderID");
            content.Add(new StringContent(customerName ?? ""), "CustomerName");

            var resp = await _http.PostAsync("uploads/proof-of-payment", content);
            if (!resp.IsSuccessStatusCode)
                return (false, $"Upload failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
            return (true, null);
        }
    }
}