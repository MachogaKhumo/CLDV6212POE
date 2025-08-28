using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ABC_Retail.Services;
using ABC_Retail.Models;
using ABC_Retail.Models.ViewModels;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace ABC_Retail.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storage;

        public OrderController(IAzureStorageService storage)
        {
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _storage.GetOrdersAsync();
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new OrderCreateViewModel();
            await PopulateDropDowns(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel vm)
        {
            await PopulateDropDowns(vm);

            if (string.IsNullOrEmpty(vm.CustomerId) || string.IsNullOrEmpty(vm.ProductId))
                ModelState.AddModelError(string.Empty, "Customer and Product are required.");

            if (!ModelState.IsValid)
                return View(vm);

            var customer = await _storage.GetCustomerAsync("Customer", vm.CustomerId);
            var product = await _storage.GetProductAsync("Product", vm.ProductId);

            if (customer == null || product == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid customer or product.");
                return View(vm);
            }

            var order = new Order
            {
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = "Orders",
                CustomerId = customer.RowKey,
                Username = customer.Username,
                ProductId = product.RowKey,
                ProductName = product.ProductName,
                OrderDate = vm.OrderDate.ToUniversalTime(),
                Quantity = vm.Quantity,
                UnitPrice = product.Price,
                TotalPrice = vm.Quantity * product.Price,
                Status = vm.Status
            };

            await _storage.AddOrderAsync(order);
            await _storage.EnqueueOrderMessageAsync($"Processing order {order.RowKey} for {order.Username} – {order.Quantity} x {order.ProductName}");

            TempData["Msg"] = "Order created and queued for processing.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _storage.GetOrderAsync("Orders", id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Convert the OrderDate to UTC before updating
            model.OrderDate = model.OrderDate.ToUniversalTime(); // <-- ADD THIS LINE

            await _storage.UpdateOrderAsync(model);
            TempData["Msg"] = "Order updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _storage.GetOrderAsync("Orders", id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            await _storage.DeleteOrderAsync("Orders", id);
            TempData["Msg"] = "Order deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropDowns(OrderCreateViewModel vm)
        {
            var customers = await _storage.GetCustomersAsync();
            vm.Customers = customers.Select(c => new SelectListItem
            {
                Value = c.RowKey,
                Text = $"{c.Name} {c.Surname} ({c.Username})"
            }).ToList();

            var products = await _storage.GetProductsAsync();
            vm.Products = products.Select(p => new SelectListItem
            {
                Value = p.RowKey,
                Text = $"{p.ProductName} – R{p.Price}"
            }).ToList();
        }

        // AJAX method for real-time pricing
        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return Json(new { price = 0m });

            var product = await _storage.GetProductAsync("Product", productId);
            return Json(new { price = product?.Price ?? 0d });
        }
    }
}