using ABC_Retail.Models;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ABC_Retail.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storage;
        private readonly IFunctionsApi _functionsApi;

        public ProductController(IAzureStorageService storage, IFunctionsApi functionsApi)
        {
            _storage = storage;
            _functionsApi = functionsApi;
        }

        [Authorize(Roles = "Admin, Customer")]
        public async Task<IActionResult> Index()
        {
            var products = await _storage.GetProductsAsync();
            return View(products);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create() => View(new Product());

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? image)
        {
            if (image != null)
            {
                var imageUrl = await _functionsApi.UploadProductImageAsync(image);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    model.ImageUrl = imageUrl;
                    TempData["Msg"] = "Product added via Functions API.";
                }
                else
                {
                    var url = await _storage.UploadProductImageAsync(image);
                    model.ImageUrl = url;
                    TempData["Msg"] = "Product added (direct storage).";
                }
            }

            if (!ModelState.IsValid) return View(model);

            await _storage.AddProductAsync(model);
            TempData["Msg"] = "Product added.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var product = await _storage.GetProductAsync("Product", id);
            if (product == null) return NotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product model, IFormFile? image)
        {
            if (image != null)
            {
                var imageUrl = await _functionsApi.UploadProductImageAsync(image);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    model.ImageUrl = imageUrl;
                    TempData["Msg"] = "Product updated via Functions API.";
                }
                else
                {
                    var url = await _storage.UploadProductImageAsync(image);
                    model.ImageUrl = url;
                    TempData["Msg"] = "Product updated (direct storage).";
                }
            }

            if (!ModelState.IsValid) return View(model);

            await _storage.UpdateProductAsync(model);
            TempData["Msg"] = "Product updated.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
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