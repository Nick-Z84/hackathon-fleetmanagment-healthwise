using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;
using VehicleBookingSystem.Web.Models;
namespace VehicleBookingSystem.Web.Controllers;

public class VehiclesController(
    IVehicleRepository vehicleRepo,
    IAuditLogRepository auditRepo,
    IFuelReceiptRepository fuelRepo,
    IWebHostEnvironment env) : Controller
{
    public async Task<IActionResult> Index(string? status)
    {
        IEnumerable<Vehicle> vehicles;
        if (Enum.TryParse<VehicleStatus>(status, out var parsed))
        {
            vehicles = await vehicleRepo.GetByStatusAsync(parsed);
            ViewBag.FilterStatus = status;
        }
        else
        {
            vehicles = await vehicleRepo.GetAllAsync();
        }
        return View(vehicles);
    }

    public async Task<IActionResult> Details(int id)
    {
        var vehicle = await vehicleRepo.GetByIdAsync(id);
        if (vehicle is null) return NotFound();
        ViewBag.AuditLogs    = await auditRepo.GetByEntityAsync("Vehicle", id);
        ViewBag.FuelReceipts = await fuelRepo.GetByVehicleIdAsync(id);
        // Absolute URL for the fuel-receipt QR code placed inside this vehicle
        ViewBag.FuelQrUrl = $"https://localhost:44399/FuelReceipts/Create?vehicles={id}";
        return View(vehicle);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new CreateVehicleViewModel());

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateVehicleViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var vehicle = new Vehicle
        {
            Make = vm.Make, Model = vm.Model, Year = vm.Year, LicensePlate = vm.LicensePlate,
            Odometer = vm.Odometer, InitialOdometer = vm.InitialOdometer,
            FirstRegisteredDate = vm.FirstRegisteredDate,
            LastServiceDate = vm.LastServiceDate, NextServiceDate = vm.NextServiceDate,
            Location = vm.Location, FleetAllocation = vm.FleetAllocation,
            RegistrationNumber = vm.RegistrationNumber, RegistrationExpiry = vm.RegistrationExpiry,
            InsuranceProvider = vm.InsuranceProvider, InsurancePolicyNumber = vm.InsurancePolicyNumber,
            InsuranceExpiry = vm.InsuranceExpiry, Notes = vm.Notes
        };
        var created = await vehicleRepo.AddAsync(vehicle);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Vehicle", EntityId = created.Id,
            Action = "Created", PerformedBy = "WebUser",
            Details = $"{created.Year} {created.Make} {created.Model} ({created.LicensePlate})"
        });
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var v = await vehicleRepo.GetByIdAsync(id);
        if (v is null) return NotFound();
        var vm = new EditVehicleViewModel
        {
            Id = v.Id, Make = v.Make, Model = v.Model, Year = v.Year,
            LicensePlate = v.LicensePlate, Status = v.Status,
            Odometer = v.Odometer, InitialOdometer = v.InitialOdometer,
            FirstRegisteredDate = v.FirstRegisteredDate,
            Location = v.Location, FleetAllocation = v.FleetAllocation,
            RegistrationNumber = v.RegistrationNumber, RegistrationExpiry = v.RegistrationExpiry,
            InsuranceProvider = v.InsuranceProvider, InsurancePolicyNumber = v.InsurancePolicyNumber,
            InsuranceExpiry = v.InsuranceExpiry,
            LastServiceDate = v.LastServiceDate, NextServiceDate = v.NextServiceDate, Notes = v.Notes,
            ExistingImagePath = v.ImagePath
        };
        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditVehicleViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var vehicle = await vehicleRepo.GetByIdAsync(id);
        if (vehicle is null) return NotFound();

        vehicle.Make = vm.Make; vehicle.Model = vm.Model; vehicle.Year = vm.Year;
        vehicle.LicensePlate = vm.LicensePlate; vehicle.Status = vm.Status;
        vehicle.Odometer = vm.Odometer; vehicle.InitialOdometer = vm.InitialOdometer;
        vehicle.FirstRegisteredDate = vm.FirstRegisteredDate;
        vehicle.Location = vm.Location; vehicle.FleetAllocation = vm.FleetAllocation;
        vehicle.RegistrationNumber = vm.RegistrationNumber; vehicle.RegistrationExpiry = vm.RegistrationExpiry;
        vehicle.InsuranceProvider = vm.InsuranceProvider; vehicle.InsurancePolicyNumber = vm.InsurancePolicyNumber;
        vehicle.InsuranceExpiry = vm.InsuranceExpiry;
        vehicle.LastServiceDate = vm.LastServiceDate; vehicle.NextServiceDate = vm.NextServiceDate; vehicle.Notes = vm.Notes;

        // Handle vehicle photo upload
        if (vm.VehicleImage is { Length: > 0 })
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext     = Path.GetExtension(vm.VehicleImage.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError("VehicleImage", "Only JPG, PNG, or WEBP images are accepted.");
                return View(vm);
            }
            var dir  = Path.Combine(env.WebRootPath, "uploads", "vehicles");
            Directory.CreateDirectory(dir);
            // Delete old image if present
            if (!string.IsNullOrEmpty(vehicle.ImagePath))
            {
                var old = Path.Combine(env.WebRootPath, vehicle.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
            }
            var fileName = $"vehicle_{id}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, fileName);
            using var stream = System.IO.File.Create(fullPath);
            await vm.VehicleImage.CopyToAsync(stream);
            vehicle.ImagePath = $"/uploads/vehicles/{fileName}";
        }

        await vehicleRepo.UpdateAsync(vehicle);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Vehicle", EntityId = id, Action = "Updated", PerformedBy = "WebUser",
            Details = $"Status: {vehicle.Status}, Odometer: {vehicle.Odometer}"
        });
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> UpdateService(int id)
    {
        var v = await vehicleRepo.GetByIdAsync(id);
        if (v is null) return NotFound();
        return View(new UpdateServiceViewModel
        {
            Id = id,
            VehicleDisplay = $"{v.Year} {v.Make} {v.Model} ({v.LicensePlate})",
            LastServiceDate = DateOnly.FromDateTime(DateTime.Today),
            NextServiceDate = v.NextServiceDate,
            CurrentOdometer = v.Odometer
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateService(int id, UpdateServiceViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var vehicle = await vehicleRepo.GetByIdAsync(id);
        if (vehicle is null) return NotFound();

        vehicle.LastServiceDate = vm.LastServiceDate;
        vehicle.NextServiceDate = vm.NextServiceDate;
        if (vm.CurrentOdometer.HasValue) vehicle.Odometer = vm.CurrentOdometer.Value;
        if (vehicle.Status == VehicleStatus.InService) vehicle.Status = VehicleStatus.Available;

        await vehicleRepo.UpdateAsync(vehicle);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Vehicle", EntityId = id, Action = "ServiceDateUpdated", PerformedBy = "WebUser",
            Details = $"Last service: {vm.LastServiceDate}, Next service: {vm.NextServiceDate}"
        });
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var vehicle = await vehicleRepo.GetByIdAsync(id);
        if (vehicle is null) return NotFound();
        await vehicleRepo.DeleteAsync(id);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Vehicle", EntityId = id, Action = "Deleted", PerformedBy = "WebUser",
            Details = $"{vehicle.Year} {vehicle.Make} {vehicle.Model} ({vehicle.LicensePlate})"
        });
        return RedirectToAction(nameof(Index));
    }
}
