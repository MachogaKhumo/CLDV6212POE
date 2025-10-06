using ABC_Retail.Models;

namespace ABC_Retail.Services
{
        public interface IFunctionsApi
        {
            Task<bool> AddCustomerAsync(Customer customer);
            Task<string> UploadProductImageAsync(IFormFile file);
            Task<bool> QueueOrderMessageAsync(string message);
            Task<bool> UploadContractAsync(IFormFile file);
        }
}

