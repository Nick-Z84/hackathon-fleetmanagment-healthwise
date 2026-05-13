using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.Web.Models;

public record DriverUsageSummary(
    string    DriverName,
    int       TotalTrips,
    int       TotalKm,
    int       BusinessKm,
    int       PrivateKm,
    DateTime? LastTrip
);

public class DashboardViewModel
{
    public int TotalVehicles { get; set; }

    // Metric card counts
    public int HighMileageCount  { get; set; }   // Odometer > 100,000 km
    public int HighAgeCount      { get; set; }   // Model year > 5 years old
    public int OverdueRegCount   { get; set; }   // Registration expired
    public int OverdueInsCount   { get; set; }   // Insurance expired
    public int OverdueSvcCount   { get; set; }   // Service overdue

    public IEnumerable<Booking>            ActiveBookings   { get; set; } = [];
    public IEnumerable<Vehicle>            ComplianceAlerts { get; set; } = [];
    public IEnumerable<DriverUsageSummary> DriverUsage      { get; set; } = [];
}
