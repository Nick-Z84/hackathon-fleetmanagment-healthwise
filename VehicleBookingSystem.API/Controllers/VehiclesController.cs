using Microsoft.AspNetCore.Mvc;
using VehicleBookingSystem.API.DTOs;
using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController(
    IVehicleRepository vehicleRepo,
    IAuditLogRepository auditRepo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await vehicleRepo.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var vehicle = await vehicleRepo.GetByIdAsync(id);
        return vehicle is null ? NotFound() : Ok(vehicle);
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(VehicleStatus status) =>
        Ok(await vehicleRepo.GetByStatusAsync(status));

    [HttpGet("due-for-service")]
    public async Task<IActionResult> GetDueForService([FromQuery] DateOnly? asOf)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        return Ok(await vehicleRepo.GetDueForServiceAsync(date));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleRequest request)
    {
        var vehicle = new Vehicle
        {
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            LicensePlate = request.LicensePlate,
            Odometer = request.Odometer,
            LastServiceDate = request.LastServiceDate,
            NextServiceDate = request.NextServiceDate,
            Notes = request.Notes
        };

        var created = await vehicleRepo.AddAsync(vehicle);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Vehicle",
            EntityId = created.Id,
            Action = "Created",
            PerformedBy = "System",
            Details = $"{created.Year} {created.Make} {created.Model} ({created.LicensePlate})"
        });

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleRequest request)
    {
        var vehicle = await vehicleRepo.GetByIdAsync(id);
        if (vehicle is null) return NotFound();

        vehicle.Make = request.Make;
        vehicle.Model = request.Model;
        vehicle.Year = request.Year;
        vehicle.LicensePlate = request.LicensePlate;
        vehicle.Status = request.Status;
        vehicle.Odometer = request.Odometer;
        vehicle.LastServiceDate = request.LastServiceDate;
        vehicle.NextServiceDate = request.NextServiceDate;
        vehicle.Notes = request.Notes;

        var updated = await vehicleRepo.UpdateAsync(vehicle);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Vehicle",
            EntityId = id,
            Action = "Updated",
            PerformedBy = "System",
            Details = $"Status: {updated.Status}, Odometer: {updated.Odometer}"
        });

        return Ok(updated);
    }

    [HttpPatch("{id:int}/service")]
    public async Task<IActionResult> UpdateServiceDate(int id, [FromBody] UpdateServiceDateRequest request)
    {
        var vehicle = await vehicleRepo.GetByIdAsync(id);
        if (vehicle is null) return NotFound();

        vehicle.LastServiceDate = request.LastServiceDate;
        vehicle.NextServiceDate = request.NextServiceDate;
        if (request.CurrentOdometer.HasValue)
            vehicle.Odometer = request.CurrentOdometer.Value;

        if (vehicle.Status == VehicleStatus.InService)
            vehicle.Status = VehicleStatus.Available;

        await vehicleRepo.UpdateAsync(vehicle);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Vehicle",
            EntityId = id,
            Action = "ServiceDateUpdated",
            PerformedBy = "System",
            Details = $"Last service: {request.LastServiceDate}, Next service: {request.NextServiceDate}"
        });

        return Ok(vehicle);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var vehicle = await vehicleRepo.GetByIdAsync(id);
        if (vehicle is null) return NotFound();

        await vehicleRepo.DeleteAsync(id);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Vehicle",
            EntityId = id,
            Action = "Deleted",
            PerformedBy = "System",
            Details = $"{vehicle.Year} {vehicle.Make} {vehicle.Model} ({vehicle.LicensePlate})"
        });

        return NoContent();
    }
}
