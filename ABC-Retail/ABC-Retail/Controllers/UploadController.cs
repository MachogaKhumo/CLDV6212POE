using Microsoft.AspNetCore.Mvc;
using ABC_Retail.Services;
using ABC_Retail.Models;
using System.Threading.Tasks;
using System.Linq;

namespace ABC_Retail.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _storage;

        public UploadController(IAzureStorageService storage)
        {
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            // Get existing contracts to display
            var contracts = await _storage.ListContractsAsync();
            return View(contracts);
        }

        [HttpPost]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
            {
                await _storage.UploadContractAsync(model.ProofOfPayment);
                TempData["Message"] = "File uploaded successfully!";
            }
            else
            {
                TempData["Error"] = "Please select a file to upload.";
            }

            var contracts = await _storage.ListContractsAsync();
            return View(contracts);
        }
    }
}