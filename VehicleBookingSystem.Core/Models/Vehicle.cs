using System.Text.Json.Serialization;

namespace VehicleBookingSystem.Core.Models;

public enum VehicleStatus
{
    Available,
    Booked,
    InService,
    Retired
}

public enum TreadDepth
{
    High,
    Med,
    Low
}

public class Vehicle
{
    public int Id { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleStatus Status { get; set; } = VehicleStatus.Available;
    public int Odometer { get; set; }
    public int InitialOdometer { get; set; }
    public DateOnly? FirstRegisteredDate { get; set; }
    public DateOnly? LastServiceDate { get; set; }
    public DateOnly? NextServiceDate { get; set; }
    public string? Notes { get; set; }
    public string? ImagePath { get; set; }
    public TreadDepth? TreadDepth { get; set; }

    // Fleet location
    public string? Location { get; set; }
    public string? FleetAllocation { get; set; }

    // Registration
    public string? RegistrationNumber { get; set; }
    public DateOnly? RegistrationExpiry { get; set; }

    // Insurance
    public string? InsuranceProvider { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public DateOnly? InsuranceExpiry { get; set; }

    [JsonIgnore] public ICollection<Booking> Bookings { get; set; } = [];
    [JsonIgnore] public ICollection<FuelReceipt> FuelReceipts { get; set; } = [];
}
