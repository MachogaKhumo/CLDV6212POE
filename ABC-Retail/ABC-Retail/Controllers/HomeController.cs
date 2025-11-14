using Microsoft.AspNetCore.Mvc;
using ABC_Retail.Services;
using System.Diagnostics;
using ABC_Retail.Models;
using ABC_Retail.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace ABC_Retail.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAzureStorageService _storage;
        private readonly IFunctionsApi _api;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IAzureStorageService storage, IFunctionsApi api, ILogger<HomeController> logger)
        {
            _storage = storage;
            _api = api;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _api.GetProductsAsync() ?? new List<Product>();
                var vm = new HomeViewModel
                {
                    FeaturedProducts = products.Take(8).ToList(),
                    ProductCount = products.Count
                };
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load products for Home page.");
                TempData["Error"] = "Could not load products. Please try again later.";
                return View(new HomeViewModel());
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
                var customers = await _api.GetCustomersAsync() ?? new List<Customer>();
                var orders = await _api.GetOrdersAsync() ?? new List<Order>();

                var model = new
                {
                    TotalCustomers = customers.Count,
                    TotalOrders = orders.Count
                };

                ViewBag.AdminSummary = model;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Admin Dashboard data.");
                TempData["Error"] = "Could not load Admin Dashboard data.";
                return View();
            }
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CustomerDashboard()
        {
            try
            {
                var userEmail = User.Identity?.Name;
                ViewBag.UserEmail = userEmail;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Customer Dashboard data.");
                TempData["Error"] = "Could not load your dashboard. Please try again.";
                return View();
            }
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}