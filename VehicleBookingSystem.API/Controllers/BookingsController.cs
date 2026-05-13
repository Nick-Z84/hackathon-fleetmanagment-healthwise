using Microsoft.AspNetCore.Mvc;
using VehicleBookingSystem.API.DTOs;
using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(
    IBookingRepository bookingRepo,
    IVehicleRepository vehicleRepo,
    IAuditLogRepository auditRepo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await bookingRepo.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var booking = await bookingRepo.GetByIdAsync(id);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive() =>
        Ok(await bookingRepo.GetActiveBookingsAsync());

    [HttpGet("vehicle/{vehicleId:int}")]
    public async Task<IActionResult> GetByVehicle(int vehicleId) =>
        Ok(await bookingRepo.GetByVehicleIdAsync(vehicleId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
    {
        var vehicle = await vehicleRepo.GetByIdAsync(request.VehicleId);
        if (vehicle is null)
            return BadRequest("Vehicle not found.");

        if (vehicle.Status != VehicleStatus.Available)
            return Conflict($"Vehicle is currently {vehicle.Status} and cannot be booked.");

        if (await bookingRepo.HasActiveBookingAsync(request.VehicleId))
            return Conflict("Vehicle already has an active booking.");

        var booking = new Booking
        {
            VehicleId = request.VehicleId,
            BookedBy = request.BookedBy,
            Purpose = request.Purpose,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            OdometerStart = vehicle.Odometer,
            Status = BookingStatus.Active,
            Notes = request.Notes
        };

        vehicle.Status = VehicleStatus.Booked;
        await vehicleRepo.UpdateAsync(vehicle);

        var created = await bookingRepo.AddAsync(booking);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Booking",
            EntityId = created.Id,
            Action = "Created",
            PerformedBy = request.BookedBy,
            Details = $"Vehicle {vehicle.LicensePlate} booked by {request.BookedBy} for: {request.Purpose}"
        });

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, [FromBody] CompleteBookingRequest request)
    {
        var booking = await bookingRepo.GetByIdAsync(id);
        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.Active)
            return BadRequest("Only active bookings can be completed.");

        booking.Status = BookingStatus.Completed;
        booking.ActualEndTime = DateTime.UtcNow;
        booking.OdometerEnd = request.OdometerEnd;
        if (request.Notes is not null) booking.Notes = request.Notes;

        var vehicle = await vehicleRepo.GetByIdAsync(booking.VehicleId);
        if (vehicle is not null)
        {
            vehicle.Odometer = request.OdometerEnd;
            vehicle.Status = VehicleStatus.Available;
            await vehicleRepo.UpdateAsync(vehicle);
        }

        await bookingRepo.UpdateAsync(booking);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Booking",
            EntityId = id,
            Action = "Completed",
            PerformedBy = booking.BookedBy,
            Details = $"Odometer end: {request.OdometerEnd}, km travelled: {request.OdometerEnd - booking.OdometerStart}"
        });

        return Ok(booking);
    }

    [HttpPatch("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromQuery] string cancelledBy)
    {
        var booking = await bookingRepo.GetByIdAsync(id);
        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.Active)
            return BadRequest("Only active bookings can be cancelled.");

        booking.Status = BookingStatus.Cancelled;
        booking.ActualEndTime = DateTime.UtcNow;

        var vehicle = await vehicleRepo.GetByIdAsync(booking.VehicleId);
        if (vehicle is not null)
        {
            vehicle.Status = VehicleStatus.Available;
            await vehicleRepo.UpdateAsync(vehicle);
        }

        await bookingRepo.UpdateAsync(booking);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Booking",
            EntityId = id,
            Action = "Cancelled",
            PerformedBy = cancelledBy,
            Details = $"Booking for vehicle {vehicle?.LicensePlate} cancelled."
        });

        return Ok(booking);
    }
}
