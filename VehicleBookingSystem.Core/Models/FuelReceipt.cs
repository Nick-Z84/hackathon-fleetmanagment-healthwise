using System.Text.Json.Serialization;

namespace VehicleBookingSystem.Core.Models;

public class FuelReceipt
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    [JsonIgnore] public Vehicle Vehicle { get; set; } = null!;
    public DateOnly Date { get; set; }
    public decimal Litres { get; set; }
    public decimal CostPerLitre { get; set; }
    public decimal TotalCost { get; set; }
    public string? Station { get; set; }
    public string? ReceiptFileName { get; set; }
    public string? ReceiptFilePath { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int? OdometerReading { get; set; }
}
