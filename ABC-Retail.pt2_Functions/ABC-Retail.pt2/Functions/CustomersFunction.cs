using ABC_Retail.pt2.Functions.Entities;
using ABC_Retail.pt2.Functions.Helpers;
using ABC_Retail.pt2.Functions.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace ABC_Retail.pt2.Functions.Functions
{
    public class CustomersFunctions
    {
        private readonly TableClient _table;
        private readonly ILogger _logger;

        public CustomersFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CustomersFunctions>();
            var conn = Environment.GetEnvironmentVariable("connection");
            var svc = new TableServiceClient(conn);
            _table = svc.GetTableClient(Environment.GetEnvironmentVariable("CustomerTable") ?? "Customers");
        }

        [Function("Customers_List")]
        public async Task<HttpResponseData> List([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")] HttpRequestData req)
        {
            await _table.CreateIfNotExistsAsync();
            var list = new List<CustomerDto>();
            await foreach (var e in _table.QueryAsync<CustomerEntity>(x => x.PartitionKey == "Customer"))
                list.Add(e.ToDto());
            return await HttpJson.OkAsync(req, list);
        }

        [Function("Customers_Get")]
        public async Task<HttpResponseData> Get([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{id}")] HttpRequestData req, string id)
        {
            try
            {
                var res = await _table.GetEntityAsync<CustomerEntity>("Customer", id);
                return await HttpJson.OkAsync(req, res.Value.ToDto());
            }
            catch
            {
                return await HttpJson.NotFoundAsync(req, "Customer not found");
            }
        }

        [Function("Customers_Create")]
        public async Task<HttpResponseData> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")] HttpRequestData req)
        {
            var model = await HttpJson.ReadAsync<CustomerDto>(req);
            if (model == null) return await HttpJson.BadAsync(req, "Invalid body");

            var e = new CustomerEntity
            {
                PartitionKey = "Customer",
                RowKey = Guid.NewGuid().ToString(),
                Name = model.Name,
                Username = model.Username,
                Email = model.Email,
                ShippingAddress = model.ShippingAddress
            };

            await _table.CreateIfNotExistsAsync();
            await _table.AddEntityAsync(e);

            return await HttpJson.CreatedAsync(req, e.ToDto());
        }

        [Function("Customers_Update")]
        public async Task<HttpResponseData> Update([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customers/{id}")] HttpRequestData req, string id)
        {
            var model = await HttpJson.ReadAsync<CustomerDto>(req);
            if (model == null) return await HttpJson.BadAsync(req, "Invalid body");

            try
            {
                var existing = await _table.GetEntityAsync<CustomerEntity>("Customer", id);
                var e = existing.Value;
                e.Name = model.Name ?? e.Name;
                e.Username = model.Username ?? e.Username;
                e.Email = model.Email ?? e.Email;
                e.ShippingAddress = model.ShippingAddress ?? e.ShippingAddress;
                await _table.UpdateEntityAsync(e, e.ETag, TableUpdateMode.Replace);
                return await HttpJson.OkAsync(req, e.ToDto());
            }
            catch
            {
                return await HttpJson.NotFoundAsync(req, "Customer not found");
            }
        }

        [Function("Customers_Delete")]
        public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customers/{id}")] HttpRequestData req, string id)
        {
            await _table.DeleteEntityAsync("Customer", id);
            return await HttpJson.NoContent(req);
        }
    }
}
