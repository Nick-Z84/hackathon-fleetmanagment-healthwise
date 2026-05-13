using System.ComponentModel.DataAnnotations;
using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.Web.Models;

public class CreateVehicleViewModel
{
    [Required] public string Make { get; set; } = string.Empty;
    [Required] public string Model { get; set; } = string.Empty;
    [Required, Range(1900, 2100)] public int Year { get; set; } = DateTime.Today.Year;
    [Required, Display(Name = "License Plate")] public string LicensePlate { get; set; } = string.Empty;
    [Range(0, int.MaxValue)] public int Odometer { get; set; }
    [Range(0, int.MaxValue), Display(Name = "Initial Odometer (km)")] public int InitialOdometer { get; set; }
    [Display(Name = "First Registered")] public DateOnly? FirstRegisteredDate { get; set; }
    public string? Location { get; set; }
    [Display(Name = "Fleet Allocation")] public string? FleetAllocation { get; set; }
    [Display(Name = "Registration Number")] public string? RegistrationNumber { get; set; }
    [Display(Name = "Registration Expiry")] public DateOnly? RegistrationExpiry { get; set; }
    [Display(Name = "Insurance Provider")] public string? InsuranceProvider { get; set; }
    [Display(Name = "Insurance Policy #")] public string? InsurancePolicyNumber { get; set; }
    [Display(Name = "Insurance Expiry")] public DateOnly? InsuranceExpiry { get; set; }
    [Display(Name = "Last Service Date")] public DateOnly? LastServiceDate { get; set; }
    [Display(Name = "Next Service Date")] public DateOnly? NextServiceDate { get; set; }
    public string? Notes { get; set; }
}

public class EditVehicleViewModel
{
    public int Id { get; set; }
    [Required] public string Make { get; set; } = string.Empty;
    [Required] public string Model { get; set; } = string.Empty;
    [Required, Range(1900, 2100)] public int Year { get; set; }
    [Required, Display(Name = "License Plate")] public string LicensePlate { get; set; } = string.Empty;
    public VehicleStatus Status { get; set; }
    [Range(0, int.MaxValue)] public int Odometer { get; set; }
    [Range(0, int.MaxValue), Display(Name = "Initial Odometer (km)")] public int InitialOdometer { get; set; }
    [Display(Name = "First Registered")] public DateOnly? FirstRegisteredDate { get; set; }
    public string? Location { get; set; }
    [Display(Name = "Fleet Allocation")] public string? FleetAllocation { get; set; }
    [Display(Name = "Registration Number")] public string? RegistrationNumber { get; set; }
    [Display(Name = "Registration Expiry")] public DateOnly? RegistrationExpiry { get; set; }
    [Display(Name = "Insurance Provider")] public string? InsuranceProvider { get; set; }
    [Display(Name = "Insurance Policy #")] public string? InsurancePolicyNumber { get; set; }
    [Display(Name = "Insurance Expiry")] public DateOnly? InsuranceExpiry { get; set; }
    [Display(Name = "Last Service Date")] public DateOnly? LastServiceDate { get; set; }
    [Display(Name = "Next Service Date")] public DateOnly? NextServiceDate { get; set; }
    public string? Notes { get; set; }
    public string? ExistingImagePath { get; set; }
    [Display(Name = "Vehicle Photo")] public Microsoft.AspNetCore.Http.IFormFile? VehicleImage { get; set; }
}

public class UpdateServiceViewModel
{
    public int Id { get; set; }
    public string VehicleDisplay { get; set; } = string.Empty;
    [Required, Display(Name = "Last Service Date")] public DateOnly LastServiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Display(Name = "Next Service Date")] public DateOnly? NextServiceDate { get; set; }
    [Display(Name = "Current Odometer")] public int? CurrentOdometer { get; set; }
}
