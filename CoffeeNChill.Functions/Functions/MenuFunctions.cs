using System.Net;
using System.Text.Json;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;

namespace CoffeeNChill.Functions.Functions;

public class MenuFunctions
{
    private readonly MenuService _menuService;

    public MenuFunctions(MenuService menuService)
    {
        _menuService = menuService;
    }

    [Function("CreateMenuItem")]
    public async Task<HttpResponseData> CreateMenuItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<MenuItemInput>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (input is null || string.IsNullOrWhiteSpace(input.Category) || string.IsNullOrWhiteSpace(input.Name))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Name and Category are required.");
            return badRequest;
        }

        if (input.Price < 0)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Price cannot be negative.");
            return badRequest;
        }

        var item = new MenuItem
        {
            PartitionKey = input.Category,
            RowKey = string.IsNullOrWhiteSpace(input.Sku) ? Guid.NewGuid().ToString() : input.Sku,
            Name = input.Name,
            Description = input.Description ?? "",
            Price = input.Price,
            IsAvailable = input.IsAvailable
        };

        try
        {
            var created = await _menuService.CreateMenuItemAsync(item);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(created);
            return response;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            var conflict = req.CreateResponse(HttpStatusCode.Conflict);
            await conflict.WriteStringAsync($"A menu item with SKU '{item.RowKey}' already exists in category '{item.PartitionKey}'.");
            return conflict;
        }
    }


    [Function("GetAllMenuItems")]
    public async Task<HttpResponseData> GetAllMenuItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")] HttpRequestData req)
    {
        var items = await _menuService.GetAllMenuItemsAsync();
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(items);
        return response;
    }

    [Function("GetMenuItemsByCategory")]
    public async Task<HttpResponseData> GetMenuItemsByCategory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/category/{category}")] HttpRequestData req, string category)
    {
        var items = await _menuService.GetMenuItemsByCategoryAsync(category);

        if (items.Count == 0)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"No menu items found for category '{category}'.");
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(items);
        return response;
    }

    [Function("UpdateMenuItem")]
    public async Task<HttpResponseData> UpdateMenuItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/{category}/{sku}")] HttpRequestData req, string category, string sku)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<MenuItemInput>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (input is null)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Request body is required.");
            return badRequest;
        }

        if (input.Price < 0)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Price cannot be negative.");
            return badRequest;
        }

        var updatedItem = new MenuItem
        {
            Name = input.Name,
            Description = input.Description ?? "",
            Price = input.Price,
            IsAvailable = input.IsAvailable
        };

        var result = await _menuService.UpdateMenuItemAsync(category, sku, updatedItem);

        if (result is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Menu item with SKU '{sku}' not found in category '{category}'.");
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    [Function("DeleteMenuItem")]
    public async Task<HttpResponseData> DeleteMenuItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/{category}/{sku}")] HttpRequestData req, string category, string sku)
    {
        var deleted = await _menuService.DeleteMenuItemAsync(category, sku);

        if (!deleted)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Menu item with SKU '{sku}' not found in category '{category}'.");
            return notFound;
        }

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    private class MenuItemInput
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public double Price { get; set; }
        public bool IsAvailable { get; set; }
        public string Category { get; set; } = default!;
        public string? Sku { get; set; }
    }
}
