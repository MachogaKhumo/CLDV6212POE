using ABC_Retail.pt2.Functions.Helpers;
using ABC_Retail.pt2.Functions.Models;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABC_Retail.pt2.Functions.Functions
{
    public class OrdersFunction
    {
        private readonly ILogger _logger;
        private readonly string _queueName;

        public OrdersFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OrdersFunction>();
            _queueName = Environment.GetEnvironmentVariable("OrderQueue")
                         ?? Environment.GetEnvironmentVariable("QUEUE_ORDER_NOTIFICATIONS")
                         ?? "orderprocessing";
        }

        [Function("Orders_Create")]
        public async Task<HttpResponseData> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
        {
            var order = await HttpJson.ReadAsync<CreateOrderRequest>(req);
            if (order == null) return await HttpJson.BadAsync(req, "Invalid body");

            var conn = Environment.GetEnvironmentVariable("connection");
            var queueClient = new QueueClient(conn, _queueName);
            await queueClient.CreateIfNotExistsAsync();

            var payload = JsonSerializer.Serialize(order);
            await queueClient.SendMessageAsync(payload);

            _logger.LogInformation("Order message sent to queue.");
            return await HttpJson.Text(req, HttpStatusCode.Accepted, "Order queued");
        }

        [Function("Orders_List")]
        public async Task<HttpResponseData> List([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
        {
            var conn = Environment.GetEnvironmentVariable("connection");
            var svc = new Azure.Data.Tables.TableServiceClient(conn);
            var table = svc.GetTableClient(Environment.GetEnvironmentVariable("OrdersTable") ?? "Orders");
            await table.CreateIfNotExistsAsync();

            var list = new System.Collections.Generic.List<ABC_Retail.pt2.Functions.Models.OrderDto>();
            await foreach (var e in table.QueryAsync<ABC_Retail.pt2.Functions.Entities.OrderEntity>(x => x.PartitionKey == "Order"))
            {
                list.Add(e.ToDto());
            }
            return await HttpJson.OkAsync(req, list);
        }
    }
}
