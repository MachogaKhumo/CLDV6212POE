using ABC_Retail.Models;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storage;
        public ProductController(IAzureStorageService storage) => _storage = storage;

        public async Task<IActionResult> Index()
        {
            var products = await _storage.GetProductsAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult Create() => View(new Product());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? image)
        {
            if (image != null)
            {
                var url = await _storage.UploadProductImageAsync(image);
                model.ImageUrl = url;
            }
            if (!ModelState.IsValid) return View(model);
            await _storage.AddProductAsync(model);
            TempData["Msg"] = "Product added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var product = await _storage.GetProductAsync("Product", id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product model, IFormFile? image)
        {
            if (image != null)
            {
                var url = await _storage.UploadProductImageAsync(image);
                model.ImageUrl = url;
            }
            if (!ModelState.IsValid) return View(model);
            await _storage.UpdateProductAsync(model);
            TempData["Msg"] = "Product updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _storage.DeleteProductAsync("Product", id);
            TempData["Msg"] = "Product deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
