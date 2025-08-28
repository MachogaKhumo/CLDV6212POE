using ABC_Retail.Models;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storage;
        public CustomerController(IAzureStorageService storage) => _storage = storage;

        public async Task<IActionResult> Index()
        {
            var customers = await _storage.GetCustomersAsync();
            return View(customers);
        }

        [HttpGet]
        public IActionResult Create() => View(new Customer());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model)
        {
            if (!ModelState.IsValid) return View(model);
            await _storage.AddCustomerAsync(model);
            TempData["Msg"] = "Customer added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var customer = await _storage.GetCustomerAsync(partitionKey, rowKey); // Now use the parameters
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer model)
        {
            if (!ModelState.IsValid) return View(model);
            await _storage.UpdateCustomerAsync(model);
            TempData["Msg"] = "Customer updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey) // Change to match view
        {
            await _storage.DeleteCustomerAsync(partitionKey, rowKey); // Pass both parameters
            TempData["Msg"] = "Customer deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
