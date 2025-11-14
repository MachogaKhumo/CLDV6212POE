using ABC_Retail.Data;
using ABC_Retail.Models;
using ABC_Retail.Models.ViewModels;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ABC_Retail.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly IFunctionsApi _api;

        public CartController(AuthDbContext db, IFunctionsApi api)
        {
            _db = db;
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var cartItems = await _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            var viewModel = new CartPageViewModel();

            foreach (var item in cartItems)
            {
                var product = await _api.GetProductAsync(item.ProductId);
                if (product != null)
                {
                    viewModel.Items.Add(new CartItemViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = product.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = (decimal)product.Price
                    });
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Add(string productId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(productId))
                return RedirectToAction("Index", "Product");

            var product = await _api.GetProductAsync(productId);
            if (product == null)
                return NotFound();

            var existing = await _db.Cart
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.CustomerUsername == username);

            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                _db.Cart.Add(new Cart
                {
                    CustomerUsername = username,
                    ProductId = productId,
                    Quantity = 1
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{product.ProductName} added to cart.";
            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        public async Task<IActionResult> Remove(string productId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index");

            var item = await _db.Cart
                .FirstOrDefaultAsync(c => c.CustomerUsername == username && c.ProductId == productId);

            if (item != null)
            {
                _db.Cart.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Item removed from cart.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantities(List<CartItemViewModel> items)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index");

            foreach (var item in items)
            {
                var cartItem = await _db.Cart
                    .FirstOrDefaultAsync(c => c.CustomerUsername == username && c.ProductId == item.ProductId);

                if (cartItem != null)
                {
                    cartItem.Quantity = item.Quantity;
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Cart updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var customers = await _api.GetCustomersAsync();
            var customer = customers.FirstOrDefault(c => c.Username == username);

            if (customer == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction("Index");
            }

            var cartItems = await _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            foreach (var item in cartItems)
            {
                var product = await _api.GetProductAsync(item.ProductId);
                if (product != null)
                {
                    var order = new Order
                    {
                        PartitionKey = "Order",
                        RowKey = Guid.NewGuid().ToString(),
                        CustomerId = customer.RowKey,
                        Username = customer.Username,
                        ProductId = product.RowKey,
                        ProductName = product.ProductName,
                        OrderDate = DateTime.UtcNow,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = item.Quantity * product.Price,
                        Status = "Pending"
                    };

                    await _api.CreateOrderAsync(order);
                }
            }

            _db.Cart.RemoveRange(cartItems);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "☑ Order placed successfully!";
            return RedirectToAction("Confirmation");
        }

        public IActionResult Confirmation()
        {
            ViewBag.Message = TempData["SuccessMessage"] ?? "Thank you for your purchase!";
            return View();
        }
    }
}