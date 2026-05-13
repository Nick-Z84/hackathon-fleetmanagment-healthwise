using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VehicleBookingSystem.Web.Models;

public class AddFuelReceiptViewModel
{
    [Required, Display(Name = "Vehicle")] public int VehicleId { get; set; }
    [Required] public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required, Range(0.01, double.MaxValue)] public decimal Litres { get; set; }
    [Required, Display(Name = "Cost Per Litre"), Range(0.01, double.MaxValue)] public decimal CostPerLitre { get; set; }
    [Required, Display(Name = "Total Cost"), Range(0.01, double.MaxValue)] public decimal TotalCost { get; set; }
    public string? Station { get; set; }
    [Display(Name = "Odometer Reading")] public int? OdometerReading { get; set; }
    [Required, Display(Name = "Uploaded By")] public string UploadedBy { get; set; } = string.Empty;
    [Display(Name = "Receipt File")] public Microsoft.AspNetCore.Http.IFormFile? ReceiptFile { get; set; }
    public IEnumerable<SelectListItem> Vehicles { get; set; } = [];

    // Set when vehicle is pre-selected — used to render QR code and lock the vehicle field
    public string? PreSelectedVehicleDisplay { get; set; }
    public string? QrCodeUrl { get; set; }
    /// <summary>Non-zero when navigated from a Vehicle Details page; drives Cancel link and post-save redirect.</summary>
    public int SourceVehicleId { get; set; }
}
