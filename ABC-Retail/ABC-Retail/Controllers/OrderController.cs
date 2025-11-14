using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ABC_Retail.Services;
using ABC_Retail.Models;
using ABC_Retail.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ABC_Retail.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storage;
        private readonly IFunctionsApi _functionsApi;

        public OrderController(IAzureStorageService storage, IFunctionsApi functionsApi)
        {
            _storage = storage;
            _functionsApi = functionsApi;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var orders = await _functionsApi.GetOrdersAsync();
            return View(orders?.OrderByDescending(o => o.OrderDate).ToList() ?? new List<Order>());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _functionsApi.GetOrderAsync(id);
            return order == null ? NotFound() : View(order);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order posted)
        {
            if (!ModelState.IsValid)
                return View(posted);

            try
            {
                await _functionsApi.UpdateOrderStatusAsync(posted.RowKey, posted.Status);
                TempData["Success"] = "Order updated successfully!";
                return RedirectToAction(nameof(Manage));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating order: {ex.Message}");
                return View(posted);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _functionsApi.DeleteOrderAsync(id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                await _functionsApi.UpdateOrderStatusAsync(id, newStatus);
                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Customer")]
        public async Task<IActionResult> Index()
        {
            var orders = await _functionsApi.GetOrdersAsync();
            return View(orders?.OrderByDescending(o => o.OrderDate).ToList() ?? new List<Order>());
        }

        [Authorize(Roles = "Admin, Customer")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _functionsApi.GetOrderAsync(id);
            return order == null ? NotFound() : View(order);
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyOrders()
        {
            var customerId = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerId))
            {
                TempData["Error"] = "Customer not found in session.";
                return RedirectToAction("Index", "Login");
            }

            var orders = await _functionsApi.GetOrdersAsync();
            var customerOrders = orders?.Where(o => o.CustomerId == customerId).ToList() ?? new List<Order>();
            return View("Index", customerOrders.OrderByDescending(o => o.OrderDate).ToList());
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var customers = await _functionsApi.GetCustomersAsync() ?? new List<Customer>();
            var products = await _functionsApi.GetProductsAsync() ?? new List<Product>();

            var vm = new OrderCreateViewModel
            {
                Customers = customers.Select(c => new SelectListItem
                {
                    Value = c.RowKey,
                    Text = $"{c.Name} {c.Surname} ({c.Username})"
                }).ToList(),
                Products = products.Select(p => new SelectListItem
                {
                    Value = p.RowKey,
                    Text = $"{p.ProductName} - R{p.Price}"
                }).ToList()
            };
            return View(vm);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            await PopulateDropDowns(model);

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // FIXED: Use single parameter methods from IFunctionsApi
                var customer = await _functionsApi.GetCustomerAsync(model.CustomerId);
                var product = await _functionsApi.GetProductAsync(model.ProductId);

                if (customer == null || product == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid customer or product selected.");
                    return View(model);
                }

                if (product.StockAvailable < model.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                    return View(model);
                }

                var order = new Order
                {
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = "Orders",
                    CustomerId = customer.RowKey,
                    Username = customer.Username,
                    ProductId = product.RowKey,
                    ProductName = product.ProductName,
                    OrderDate = model.OrderDate.ToUniversalTime(),
                    Quantity = model.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = model.Quantity * product.Price,
                    Status = model.Status
                };

                await _storage.AddOrderAsync(order);

                var message = $"Processing order {order.RowKey} for {order.Username} - {order.Quantity} x {order.ProductName}";
                var queued = await _functionsApi.QueueOrderMessageAsync(message);
                if (!queued)
                {
                    await _storage.EnqueueOrderMessageAsync(message);
                }

                TempData["Msg"] = "Order created and queued for processing.";
                return RedirectToAction(nameof(MyOrders));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error creating order: {ex.Message}");
                await PopulateDropDowns(model);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _functionsApi.GetProductAsync(productId);
                if (product != null)
                {
                    return Json(new { success = true, price = product.Price, stock = product.StockAvailable, productName = product.ProductName });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        private async Task PopulateDropDowns(OrderCreateViewModel vm)
        {
            var customers = await _functionsApi.GetCustomersAsync() ?? new List<Customer>();
            var products = await _functionsApi.GetProductsAsync() ?? new List<Product>();

            vm.Customers = customers.Select(c => new SelectListItem
            {
                Value = c.RowKey,
                Text = $"{c.Name} {c.Surname} ({c.Username})"
            }).ToList();

            vm.Products = products.Select(p => new SelectListItem
            {
                Value = p.RowKey,
                Text = $"{p.ProductName} - R{p.Price}"
            }).ToList();
        }
    }
}