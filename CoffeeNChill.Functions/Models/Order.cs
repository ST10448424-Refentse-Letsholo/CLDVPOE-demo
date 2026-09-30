using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Functions.Models;

// Represents an incoming order message from the order-processing-queue.
public class Order
{
    public string OrderId { get; set; } = default!;
    public DateTimeOffset OrderTimestamp { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public double TotalPrice { get; set; }
}

public class OrderItem
{
    public string Sku { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Quantity { get; set; }
    public double Price { get; set; }
}

// Table Storage representation
public class OrderEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Status { get; set; } = default!;
    public DateTimeOffset OrderTimestamp { get; set; }
    public double TotalPrice { get; set; }
    public string ItemsJson { get; set; } = default!; // serialized list of items, since Table Storage can't store nested objects
}