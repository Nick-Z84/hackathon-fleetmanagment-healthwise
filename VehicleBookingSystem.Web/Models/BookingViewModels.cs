using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VehicleBookingSystem.Web.Models;

/// <summary>Carries vehicle data to the booking form so JS can filter by location and date.</summary>
public record VehicleOption(int Id, string Text, string Location);

public class CreateBookingViewModel
{
    [Required, Display(Name = "Vehicle")] public int VehicleId { get; set; }
    [Display(Name = "Filter by Location")] public string? LocationFilter { get; set; }

    [Required, Display(Name = "Start Date & Time")] public DateTime StartTime { get; set; } = DateTime.Now.AddHours(1);
    [Display(Name = "Expected End Date & Time")] public DateTime? EndTime { get; set; }

    // Set server-side from the logged-in user; hidden in the form
    [Required] public int BookedByStaffId { get; set; }
    public string BookedByName { get; set; } = string.Empty;

    [Required, Display(Name = "Driver")]
    public int DriverStaffId { get; set; }

    [Required, Display(Name = "Destination")]
    public string Destination { get; set; } = string.Empty;

    [Display(Name = "Trip Type")] public bool IsBusinessUse { get; set; } = true;

    // Notes is now the purpose field displayed to the user
    [Required, Display(Name = "Purpose")]
    public string Notes { get; set; } = string.Empty;

    // Lists for dropdowns
    public IEnumerable<VehicleOption> AvailableVehicles { get; set; } = [];
    public IEnumerable<SelectListItem> Locations { get; set; } = [];
    public IEnumerable<SelectListItem> AllStaff { get; set; } = [];
    public IEnumerable<SelectListItem> LicencedDrivers { get; set; } = [];
}

public class CompleteBookingViewModel
{
    public int Id { get; set; }
    public string VehicleDisplay { get; set; } = string.Empty;
    public string BookedBy { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int OdometerStart { get; set; }
    [Required, Display(Name = "Final Odometer")] public int OdometerEnd { get; set; }
    public string? Notes { get; set; }
}
