using ABC_Retail.pt2.Functions.Entities;
using ABC_Retail.pt2.Functions.Helpers;
using ABC_Retail.pt2.Functions.Models;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ABC_Retail.pt2.Functions.Functions
{
    public class ProductsFunctions
    {
        private readonly string _conn;
        private readonly string _table;
        private readonly string _images;
        private readonly TableClient _tableClient;
        private readonly BlobContainerClient _blobContainer;
        private readonly ILogger _logger;

        public ProductsFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProductsFunctions>();
            _conn = Environment.GetEnvironmentVariable("connection") ?? throw new InvalidOperationException("connection missing");
            _table = Environment.GetEnvironmentVariable("ProductsTable") ?? "Products";
            _images = Environment.GetEnvironmentVariable("BLOB_PRODUCT_IMAGES") ?? Environment.GetEnvironmentVariable("ProductImagesContainer") ?? "productimages";

            var svc = new TableServiceClient(_conn);
            _tableClient = svc.GetTableClient(_table);

            var blobService = new BlobServiceClient(_conn);
            _blobContainer = blobService.GetBlobContainerClient(_images);
        }

        [Function("Products_List")]
        public async Task<HttpResponseData> List([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
        {
            await _tableClient.CreateIfNotExistsAsync();
            var list = new List<ProductDto>();
            await foreach (var e in _tableClient.QueryAsync<ProductEntity>(x => x.PartitionKey == "Product"))
                list.Add(e.ToDto());
            return await HttpJson.OkAsync(req, list);
        }

        [Function("Products_Get")]
        public async Task<HttpResponseData> Get([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequestData req, string id)
        {
            try
            {
                var res = await _tableClient.GetEntityAsync<ProductEntity>("Product", id);
                return await HttpJson.OkAsync(req, res.Value.ToDto());
            }
            catch
            {
                return await HttpJson.NotFoundAsync(req, "Product not found");
            }
        }

        [Function("Products_Create")]
        public async Task<HttpResponseData> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req)
        {
            var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
            await _tableClient.CreateIfNotExistsAsync();
            await _blobContainer.CreateIfNotExistsAsync();

            string name = "", desc = "", imageUrl = "";
            double price = 0;
            int stock = 0;

            if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var form = await MultipartHelper.ParseAsync(req.Body, contentType);
                name = form.Text.GetValueOrDefault("ProductName") ?? "";
                desc = form.Text.GetValueOrDefault("Description") ?? "";
                double.TryParse(form.Text.GetValueOrDefault("Price") ?? "0", out price);
                int.TryParse(form.Text.GetValueOrDefault("AvailableStock") ?? "0", out stock);

                var file = form.Files.FirstOrDefault(f => f.FieldName == "ImageFile");
                if (file is not null && file.Data.Length > 0)
                {
                    var blobName = $"{Guid.NewGuid():N}-{file.FileName}";
                    var blob = _blobContainer.GetBlobClient(blobName);
                    file.Data.Position = 0;
                    await blob.UploadAsync(file.Data, overwrite: true);
                    imageUrl = blob.Uri.ToString();
                }
                else
                {
                    imageUrl = form.Text.GetValueOrDefault("ImageURL") ?? "";
                }
            }
            else
            {
                var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req) ?? new();
                name = body.TryGetValue("ProductName", out var pn) ? pn?.ToString() ?? "" : "";
                desc = body.TryGetValue("Description", out var d) ? d?.ToString() ?? "" : "";
                price = body.TryGetValue("Price", out var pr) ? Convert.ToDouble(pr) : 0;
                stock = body.TryGetValue("AvailableStock", out var st) ? Convert.ToInt32(st) : 0;
                imageUrl = body.TryGetValue("ImageURL", out var iu) ? iu?.ToString() ?? "" : "";
            }

            if (string.IsNullOrWhiteSpace(name))
                return await HttpJson.BadAsync(req, "ProductName is required");

            var e = new ProductEntity
            {
                PartitionKey = "Product",
                RowKey = Guid.NewGuid().ToString(),
                ProductName = name,
                Description = desc,
                Price = price,
                AvailableStock = stock,
                ImageURL = imageUrl
            };

            await _tableClient.AddEntityAsync(e);
            return await HttpJson.CreatedAsync(req, e.ToDto());
        }

        [Function("Products_Update")]
        public async Task<HttpResponseData> Update([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id}")] HttpRequestData req, string id)
        {
            var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
            try
            {
                var resp = await _tableClient.GetEntityAsync<ProductEntity>("Product", id);
                var e = resp.Value;

                if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                {
                    var form = await MultipartHelper.ParseAsync(req.Body, contentType);

                    if (form.Text.TryGetValue("ProductName", out var name)) e.ProductName = name;
                    if (form.Text.TryGetValue("Description", out var desc)) e.Description = desc;
                    if (form.Text.TryGetValue("Price", out var priceTxt) && double.TryParse(priceTxt, out var price)) e.Price = price;
                    if (form.Text.TryGetValue("AvailableStock", out var stockTxt) && int.TryParse(stockTxt, out var stock)) e.AvailableStock = stock;
                    if (form.Text.TryGetValue("ImageURL", out var iu)) e.ImageURL = iu;

                    var file = form.Files.FirstOrDefault(f => f.FieldName == "ImageFile");
                    if (file is not null && file.Data.Length > 0)
                    {
                        var blobName = $"{Guid.NewGuid():N}-{file.FileName}";
                        var blob = _blobContainer.GetBlobClient(blobName);
                        file.Data.Position = 0;
                        await blob.UploadAsync(file.Data, overwrite: true);
                        e.ImageURL = blob.Uri.ToString();
                    }
                }
                else
                {
                    var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req) ?? new();
                    if (body.TryGetValue("ProductName", out var pn)) e.ProductName = pn?.ToString() ?? e.ProductName;
                    if (body.TryGetValue("Description", out var d)) e.Description = d?.ToString() ?? e.Description;
                    if (body.TryGetValue("Price", out var pr) && double.TryParse(pr.ToString(), out var price)) e.Price = price;
                    if (body.TryGetValue("AvailableStock", out var st) && int.TryParse(st.ToString(), out var stock)) e.AvailableStock = stock;
                    if (body.TryGetValue("ImageUrl", out var iu)) e.ImageURL = iu?.ToString() ?? e.ImageURL;
                }

                await _tableClient.UpdateEntityAsync(e, e.ETag, TableUpdateMode.Replace);
                return await HttpJson.OkAsync(req, e.ToDto());
            }
            catch
            {
                return await HttpJson.NotFoundAsync(req, "Product not found");
            }
        }

        [Function("Products_Delete")]
        public async Task<HttpResponseData> Delete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "products/{id}")] HttpRequestData req, string id)
        {
            await _tableClient.DeleteEntityAsync("Product", id);
            return await HttpJson.NoContent(req);
        }
    }
}
