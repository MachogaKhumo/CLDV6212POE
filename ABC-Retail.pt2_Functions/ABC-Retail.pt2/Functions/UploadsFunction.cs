using ABC_Retail.pt2.Functions.Helpers;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace ABC_Retail.pt2.Functions.Functions
{
    public class UploadsFunctions
    {
        private readonly string _conn;
        private readonly string _proofs;
        private readonly string _share;
        private readonly string _shareDir;
        private readonly ILogger _logger;

        public UploadsFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadsFunctions>();
            _conn = Environment.GetEnvironmentVariable("connection") ?? throw new InvalidOperationException("connection missing");
            _proofs = Environment.GetEnvironmentVariable("BLOB_PAYMENT_PROOFS") ?? "payment-proofs";
            _share = Environment.GetEnvironmentVariable("ContractsShare") ?? "contracts";
            _shareDir = Environment.GetEnvironmentVariable("FILESHARE_DIR_PAYMENTS") ?? "payments";
        }

        [Function("Uploads_ProofOfPayment")]
        public async Task<HttpResponseData> Proof(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "uploads/proof-of-payment")] HttpRequestData req)
        {
            var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
            if (!contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                return await HttpJson.BadAsync(req, "Expected multipart/form-data");

            var form = await MultipartHelper.ParseAsync(req.Body, contentType);
            var file = form.Files.FirstOrDefault(f => f.FieldName == "ProofOfPayment");
            if (file is null || file.Data.Length == 0) return await HttpJson.BadAsync(req, "ProofOfPayment file is required");

            var orderId = form.Text.GetValueOrDefault("OrderID");
            var customerName = form.Text.GetValueOrDefault("CustomerName");

            // Blob: upload proof
            var blobContainer = new BlobContainerClient(_conn, _proofs)
            await blobContainer.CreateIfNotExistsAsync();
            var blobName = $"{Guid.NewGuid():N}-{file.FileName}";
            var blobClient = blobContainer.GetBlobClient(blobName);
            file.Data.Position = 0;
            await blobClient.UploadAsync(file.Data, overwrite: true);

            // Azure Files: write a small metadata file
            var share = new ShareClient(_conn, _share);
            await share.CreateIfNotExistsAsync();
            var root = share.GetRootDirectoryClient();
            var dir = root.GetSubdirectoryClient(_shareDir);
            await dir.CreateIfNotExistsAsync();

            var fileClient = dir.GetFileClient(blobName + ".meta.txt");
            var meta = $"UploadedAtUtc: {DateTimeOffset.UtcNow:O}\nOrderId: {orderId}\nCustomerName: {customerName}\nBlobUrl: {blobClient.Uri}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(meta);
            using var ms = new MemoryStream(bytes);
            await fileClient.CreateAsync(ms.Length);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, ms.Length), ms);

            _logger.LogInformation($"Uploaded proof {blobName} and metadata to file share");

            return await HttpJson.OkAsync(req, new { fileName = blobName, blobUrl = blobClient.Uri.ToString() });
        }
    }
}
