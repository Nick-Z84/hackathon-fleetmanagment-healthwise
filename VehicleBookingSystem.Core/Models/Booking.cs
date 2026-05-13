using System.Text.Json.Serialization;

namespace VehicleBookingSystem.Core.Models;

public enum BookingStatus
{
    Active,
    Completed,
    Cancelled
}

public class Booking
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    [JsonIgnore] public Vehicle Vehicle { get; set; } = null!;
    public string BookedBy { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public int OdometerStart { get; set; }
    public int? OdometerEnd { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Active;
    public int? DriverStaffId { get; set; }
    [JsonIgnore] public Staff? DriverStaff { get; set; }
    public string? Driver { get; set; }
    public string? Destination { get; set; }
    public bool IsBusinessUse { get; set; } = true;
    public string? Notes { get; set; }
}
