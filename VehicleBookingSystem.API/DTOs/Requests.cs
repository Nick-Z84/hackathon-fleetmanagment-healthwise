using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.API.DTOs;

public record CreateVehicleRequest(
    string Make,
    string Model,
    int Year,
    string LicensePlate,
    int Odometer,
    DateOnly? LastServiceDate,
    DateOnly? NextServiceDate,
    string? Notes
);

public record UpdateVehicleRequest(
    string Make,
    string Model,
    int Year,
    string LicensePlate,
    VehicleStatus Status,
    int Odometer,
    DateOnly? LastServiceDate,
    DateOnly? NextServiceDate,
    string? Notes
);

public record UpdateServiceDateRequest(
    DateOnly LastServiceDate,
    DateOnly? NextServiceDate,
    int? CurrentOdometer
);

public record CreateBookingRequest(
    int VehicleId,
    string BookedBy,
    string Purpose,
    DateTime StartTime,
    DateTime? EndTime,
    string? Notes
);

public record CompleteBookingRequest(
    int OdometerEnd,
    string? Notes
);
