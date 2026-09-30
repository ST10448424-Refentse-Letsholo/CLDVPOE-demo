using System.Text.Json;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Services;

public class OrderService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(string connectionString, ILogger<OrderService> logger)
    {
        _logger = logger;
        _tableClient = new TableClient(connectionString, "Orders");
        _tableClient.CreateIfNotExists();
    }


    public async Task ProcessOrderAsync(Order order)
    {
        var partitionKey = order.OrderTimestamp.ToString("yyyy-MM-dd");

        var entity = new OrderEntity
        {
            PartitionKey = partitionKey,
            RowKey = order.OrderId,
            Status = "Received",
            OrderTimestamp = order.OrderTimestamp,
            TotalPrice = order.TotalPrice,
            ItemsJson = JsonSerializer.Serialize(order.Items)
        };

        await _tableClient.AddEntityAsync(entity);
        _logger.LogInformation("Order {OrderId} written with status Received.", order.OrderId);

        await SimulateLifecycleAsync(partitionKey, order.OrderId);
    }

    private async Task SimulateLifecycleAsync(string partitionKey, string rowKey)
    {
        string[] statuses = { "Preparing", "Ready", "Collected" };

        foreach (var status in statuses)
        {
            await Task.Delay(TimeSpan.FromSeconds(3));

            var existing = await _tableClient.GetEntityAsync<OrderEntity>(partitionKey, rowKey);
            existing.Value.Status = status;
            await _tableClient.UpdateEntityAsync(existing.Value, existing.Value.ETag);

            _logger.LogInformation("Order {OrderId} status updated to {Status}.", rowKey, status);
        }
    }
}