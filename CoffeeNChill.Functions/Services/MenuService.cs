using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Services;

// Handles CRUD operations for menu items against Azure Table Storage.
public class MenuService
{
    private readonly TableClient _tableClient;

    public MenuService(string connectionString)
    {
        _tableClient = new TableClient(connectionString, "MenuItems");
        _tableClient.CreateIfNotExists();
    }

    public async Task<MenuItem> CreateMenuItemAsync(MenuItem item)
    {
        await _tableClient.AddEntityAsync(item);
        return item;
    }

    public async Task<List<MenuItem>> GetAllMenuItemsAsync()
    {
        var items = new List<MenuItem>();
        await foreach (var item in _tableClient.QueryAsync<MenuItem>())
        {
            items.Add(item);
        }
        return items;
    }

    public async Task<List<MenuItem>> GetMenuItemsByCategoryAsync(string category)
    {
        var items = new List<MenuItem>();
        await foreach (var item in _tableClient.QueryAsync<MenuItem>(x => x.PartitionKey == category))
        {
            items.Add(item);
        }
        return items;
    }

    public async Task<MenuItem?> UpdateMenuItemAsync(string category, string sku, MenuItem updated)
    {
        try
        {
            var existing = await _tableClient.GetEntityAsync<MenuItem>(category, sku);
            updated.PartitionKey = category;
            updated.RowKey = sku;
            updated.ETag = existing.Value.ETag;
            await _tableClient.UpdateEntityAsync(updated, updated.ETag);
            return updated;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> DeleteMenuItemAsync(string category, string sku)
    {
        try
        {
            await _tableClient.DeleteEntityAsync(category, sku);
            return true;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }
}
