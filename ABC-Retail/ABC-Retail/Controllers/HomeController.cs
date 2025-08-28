using Microsoft.AspNetCore.Mvc;
using ABC_Retail.Services;
using System.Diagnostics;
using ABC_Retail.Models;
using ABC_Retail.Models.ViewModels;

namespace ABC_Retail.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAzureStorageService _storage;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IAzureStorageService storage, ILogger<HomeController> logger)
        {
            _storage = storage;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = (await _storage.GetProductsAsync()).Take(5);
            var vm = new HomeViewModel
            {
                FeaturedProducts = products,
                CustomerCount = (await _storage.GetCustomersAsync()).Count,
                ProductCount = (await _storage.GetProductsAsync()).Count,
                OrderCount = (await _storage.GetOrdersAsync()).Count,
                UploadCount = (await _storage.GetOrdersAsync()).Count
            };
            return View(vm);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ABC_Retail.Models.ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}
