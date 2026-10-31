namespace DeliveryService.Models;

public class Delivery
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "Assigned";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
