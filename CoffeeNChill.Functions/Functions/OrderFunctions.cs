using System.Text.Json;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public class OrderFunctions
{
    private readonly OrderService _orderService;
    private readonly ILogger<OrderFunctions> _logger;

    public OrderFunctions(OrderService orderService, ILoggerFactory loggerFactory)
    {
        _orderService = orderService;
        _logger = loggerFactory.CreateLogger<OrderFunctions>();
    }


    [Function("ProcessOrderQueue")]
    public async Task Run(
        [QueueTrigger("order-processing-queue", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation("ProcessOrderQueue triggered with message: {Message}", queueMessage);

        Order? order;
        try
        {
            order = JsonSerializer.Deserialize<Order>(queueMessage,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse queue message as valid JSON. Raw message: {Message}", queueMessage);
            throw;
        }

        if (order is null || string.IsNullOrWhiteSpace(order.OrderId))
        {
            _logger.LogError("Queue message parsed but is missing a required field (OrderId). Raw message: {Message}", queueMessage);
            throw new InvalidOperationException("Queue message could not be parsed into a valid Order (missing OrderId).");
        }

        try
        {
            await _orderService.ProcessOrderAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process order {OrderId}.", order.OrderId);
            throw;
        }
    }
}