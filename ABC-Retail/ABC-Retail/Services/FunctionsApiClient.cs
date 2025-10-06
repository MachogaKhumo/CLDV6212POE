using ABC_Retail.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ABC_Retail.Services
{
    public class FunctionsApiClient : IFunctionsApi
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FunctionsApiClient> _logger;

        public FunctionsApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<FunctionsApiClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var baseUrl = _configuration["Functions:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                _httpClient.BaseAddress = new Uri(baseUrl);
            }
        }

        public async Task<bool> AddCustomerAsync(Customer customer)
        {
            try
            {
                var customerDto = new
                {
                    RowKey = customer.RowKey,
                    Name = customer.Name,
                    Surname = customer.Surname,
                    Username = customer.Username,
                    Email = customer.Email,
                    ShippingAddress = customer.ShippingAddress
                };

                var json = JsonSerializer.Serialize(customerDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/AddCustomer", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Customer added via Functions API");
                    return true;
                }

                _logger.LogWarning("Functions API call failed, will use direct storage");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Functions API for AddCustomer");
                return false; // Fallback to direct storage
            }
        }

        public async Task<string> UploadProductImageAsync(IFormFile file)
        {
            try
            {
                using var formData = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                formData.Add(fileContent, "image", file.FileName);

                var response = await _httpClient.PostAsync("api/UploadProductImage", formData);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    _logger.LogInformation("Image uploaded via Functions API");
                    return result.GetProperty("url").GetString();
                }

                _logger.LogWarning("Functions API call failed for image upload");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Functions API for UploadProductImage");
                return null; // Fallback to direct storage
            }
        }

        public async Task<bool> QueueOrderMessageAsync(string message)
        {
            try
            {
                var messageDto = new { Message = message };
                var json = JsonSerializer.Serialize(messageDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/QueueOrderMessage", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Message queued via Functions API");
                    return true;
                }

                _logger.LogWarning("Functions API call failed for queue message");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Functions API for QueueOrderMessage");
                return false; // Fallback to direct storage
            }
        }

        public async Task<bool> UploadContractAsync(IFormFile file)
        {
            try
            {
                using var formData = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                formData.Add(fileContent, "file", file.FileName);

                var response = await _httpClient.PostAsync("api/UploadContract", formData);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Contract uploaded via Functions API");
                    return true;
                }

                _logger.LogWarning("Functions API call failed for contract upload");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Functions API for UploadContract");
                return false; // Fallback to direct storage
            }
        }
    }
}
