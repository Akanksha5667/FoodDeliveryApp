using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using System.Net;
using System.Net.Http.Json;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IHttpClientFactory _clients;

    public OrdersController(OrderDbContext db, IHttpClientFactory clients)
    {
        _db = db;
        _clients = clients;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Orders.AsNoTracking().ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return order is null ? NotFound(new { Message = "Order not found" }) : Ok(order);
    }

    // The request uses the FoodOrder entity directly, as requested (no DTOs).
    // Price and food name are ALWAYS fetched from RestaurantService, not trusted from client.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FoodOrder order)
    {

        if (order.RestaurantId <= 0 || order.MenuItemId <= 0 || order.Quantity <= 0 ||
            string.IsNullOrWhiteSpace(order.CustomerName) ||
            string.IsNullOrWhiteSpace(order.DeliveryAddress))
            return BadRequest(new { Message = "RestaurantId, MenuItemId, positive Quantity, CustomerName and DeliveryAddress are required" });

        var client = _clients.CreateClient("RestaurantClient");
        MenuItemInfo? item;
        try
        {
            using var response = await client.GetAsync($"api/menuitems/{order.MenuItemId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return BadRequest(new { Message = "Menu item does not exist" });
            if (!response.IsSuccessStatusCode)
                return StatusCode(503, new { Message = "Restaurant Service is unavailable" });
            item = await response.Content.ReadFromJsonAsync<MenuItemInfo>();
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new { Message = "Restaurant Service is unavailable" });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(503, new { Message = "Restaurant Service timed out" });
        }
        catch (System.Text.Json.JsonException)
        {
            return StatusCode(503, new { Message = "Invalid response from Restaurant Service" });
        }

        if (item is null || item.RestaurantId != order.RestaurantId || !item.IsAvailable)
            return BadRequest(new { Message = "Menu item is unavailable for this restaurant" });

        order.Id = 0;
        order.FoodName = item.Name;
        order.UnitPrice = item.Price;
        order.TotalPrice = item.Price * order.Quantity;
        order.Status = "Placed";
        order.CreatedAt = DateTime.UtcNow;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] FoodOrder input)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return NotFound(new { Message = "Order not found" });
        if (input.Status is not ("Placed" or "Preparing" or "Ready" or "OutForDelivery" or "Delivered" or "Cancelled"))
            return BadRequest(new { Message = "Invalid order status" });
        order.Status = input.Status;
        await _db.SaveChangesAsync();
        return Ok(order);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return NotFound(new { Message = "Order not found" });
        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private sealed class MenuItemInfo
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}
