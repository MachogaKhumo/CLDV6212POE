using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ABC_Retail.pt2.Functions.Entities;

namespace ABC_Retail.pt2.Functions.Functions
{
    public class QueueProcessorFunctions
    {
        private readonly ILogger _logger;
        private readonly TableClient _ordersTable;

        public QueueProcessorFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueProcessorFunctions>();
            var conn = Environment.GetEnvironmentVariable("connection");
            var svc = new TableServiceClient(conn);
            _ordersTable = svc.GetTableClient(Environment.GetEnvironmentVariable("OrdersTable") ?? "Orders");
        }

        // Primary queue trigger: process orders and write to Orders table
        [Function("OrderQueueProcessor")]
        public async Task OrderQueueProcessor([QueueTrigger("%OrderQueue%", Connection = "connection")] string queueMessage)
        {
            _logger.LogInformation("OrderQueueProcessor triggered.");
            _logger.LogInformation($"Raw message: {queueMessage}");

            await _ordersTable.CreateIfNotExistsAsync();

            try
            {
                var orderEntity = JsonSerializer.Deserialize<OrderEntity>(queueMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (orderEntity == null)
                {
                    var createReq = JsonSerializer.Deserialize<ABC_Retail.pt2.Functions.Models.CreateOrderRequest>(queueMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (createReq == null)
                    {
                        _logger.LogError("Failed to deserialize queue message into known type.");
                        return;
                    }
                    orderEntity = new OrderEntity
                    {
                        PartitionKey = "Order",
                        RowKey = Guid.NewGuid().ToString(),
                        CustomerId = createReq.CustomerId,
                        ProductId = createReq.ProductId,
                        Quantity = createReq.Quantity,
                        Details = createReq.Details,
                        Status = "Processed",
                        OrderDate = DateTimeOffset.UtcNow
                    };
                }
                else
                {
                    orderEntity.RowKey = Guid.NewGuid().ToString();
                    orderEntity.PartitionKey = "Order";
                    orderEntity.Status ??= "Processed";
                    orderEntity.OrderDate = DateTimeOffset.UtcNow;
                }

                await _ordersTable.AddEntityAsync(orderEntity);
                _logger.LogInformation($"Order saved to table: {orderEntity.RowKey}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Exception while processing queue message.");
                throw;
            }
        }

        // Optional: other queue processors (notifications, stock updates)
        [Function("OrderNotifications_Processor")]
        public void OrderNotificationsProcessor(
            [QueueTrigger("%QUEUE_ORDER_NOTIFICATIONS%", Connection = "connection")] string message,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("OrderNotifications_Processor");
            log.LogInformation($"OrderNotifications message: {message}");
        }

        [Function("StockUpdates_Processor")]
        public void StockUpdatesProcessor(
            [QueueTrigger("%QUEUE_STOCK_UPDATES%", Connection = "connection")] string message,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("StockUpdates_Processor");
            log.LogInformation($"StockUpdates message: {message}");
        }
    }
}
