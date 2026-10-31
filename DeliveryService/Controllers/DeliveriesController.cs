using DeliveryService.Data;
using DeliveryService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveriesController : ControllerBase
{
    private readonly DeliveryDbContext _db;
    public DeliveriesController(DeliveryDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Deliveries.AsNoTracking().ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var delivery = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return delivery is null ? NotFound(new { Message = "Delivery not found" }) : Ok(delivery);
    }

    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetByOrder(int orderId)
    {
        var delivery = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId);
        return delivery is null ? NotFound(new { Message = "Delivery not found" }) : Ok(delivery);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Delivery delivery)
    {
        if (delivery.OrderId <= 0 || string.IsNullOrWhiteSpace(delivery.DriverName) ||
            string.IsNullOrWhiteSpace(delivery.DeliveryAddress))
            return BadRequest(new { Message = "OrderId, DriverName and DeliveryAddress are required" });
        if (await _db.Deliveries.AnyAsync(x => x.OrderId == delivery.OrderId))
            return Conflict(new { Message = "Delivery already exists for this order" });

        // Initial demo: creates a delivery manually. No cross-service order check yet.
        delivery.Id = 0;
        delivery.Status = "Assigned";
        delivery.CreatedAt = DateTime.UtcNow;
        _db.Deliveries.Add(delivery);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { Message = "Unable to create delivery; check OrderId uniqueness" }); }
        return CreatedAtAction(nameof(GetById), new { id = delivery.Id }, delivery);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Delivery input)
    {
        var delivery = await _db.Deliveries.FindAsync(id);
        if (delivery is null) return NotFound(new { Message = "Delivery not found" });
        if (string.IsNullOrWhiteSpace(input.DriverName) || string.IsNullOrWhiteSpace(input.DeliveryAddress))
            return BadRequest(new { Message = "DriverName and DeliveryAddress are required" });
        delivery.DriverName = input.DriverName;
        delivery.DeliveryAddress = input.DeliveryAddress;
        await _db.SaveChangesAsync();
        return Ok(delivery);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] Delivery input)
    {
        var delivery = await _db.Deliveries.FindAsync(id);
        if (delivery is null) return NotFound(new { Message = "Delivery not found" });
        if (input.Status is not ("Assigned" or "PickedUp" or "Delivered"))
            return BadRequest(new { Message = "Status must be Assigned, PickedUp or Delivered" });
        delivery.Status = input.Status;
        await _db.SaveChangesAsync();
        return Ok(delivery);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var delivery = await _db.Deliveries.FindAsync(id);
        if (delivery is null) return NotFound(new { Message = "Delivery not found" });
        _db.Deliveries.Remove(delivery);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
