using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VehicleBookingSystem.Core;
using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;
using VehicleBookingSystem.Web.Models;

namespace VehicleBookingSystem.Web.Controllers;

public class BookingsController(
    IBookingRepository bookingRepo,
    IVehicleRepository vehicleRepo,
    IAuditLogRepository auditRepo,
    IStaffRepository staffRepo) : Controller
{
    public async Task<IActionResult> Index(string? filter)
    {
        var bookings = filter switch
        {
            "active" => await bookingRepo.GetActiveBookingsAsync(),
            _ => await bookingRepo.GetAllAsync()
        };
        ViewBag.Filter = filter ?? "all";
        return View(bookings.OrderByDescending(b => b.StartTime));
    }

    public async Task<IActionResult> Details(int id)
    {
        var booking = await bookingRepo.GetByIdAsync(id);
        if (booking is null) return NotFound();
        var audit = await auditRepo.GetByEntityAsync("Booking", id);
        ViewBag.AuditLogs = audit;
        return View(booking);
    }

    public async Task<IActionResult> Create()
    {
        var vm = await BuildCreateViewModel();
        // Pre-fill from the logged-in user
        if (int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var staffId))
            vm.BookedByStaffId = staffId;
        vm.BookedByName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? string.Empty;
        return View(vm);
    }

    /// <summary>
    /// Returns booked date ranges for a vehicle as JSON so the booking form
    /// can disable those periods in the date pickers.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetVehicleAvailability(int vehicleId)
    {
        var bookings = await bookingRepo.GetByVehicleIdAsync(vehicleId);
        var periods = bookings
            .Where(b => b.Status == BookingStatus.Active)
            .Select(b => new
            {
                from = b.StartTime.ToString("yyyy-MM-dd HH:mm"),
                to = (b.EndTime ?? b.StartTime.AddDays(1)).ToString("yyyy-MM-dd HH:mm")
            });
        return Json(periods);
    }

    /// <summary>
    /// Returns vehicle IDs that are available (no active overlapping booking) for a given window.
    /// Called via AJAX from the booking form when dates are selected.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAvailableVehicles(DateTime start, DateTime? end)
    {
        var endTime = end ?? start.AddDays(1);
        var unavailable = await bookingRepo.GetUnavailableVehicleIdsAsync(start, endTime);
        return Json(unavailable);
    }

    private async Task<CreateBookingViewModel> BuildCreateViewModel(CreateBookingViewModel? existing = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var allVehicles = await vehicleRepo.GetByStatusAsync(VehicleStatus.Available);
        var vehicleOptions = allVehicles.Select(v =>
            new VehicleOption(v.Id, $"{v.Year} {v.Make} {v.Model} — {v.LicensePlate}", v.Location ?? string.Empty));

        var locations = FleetLocations.All
            .Select(l => new SelectListItem(l, l))
            .Prepend(new SelectListItem("— All Locations —", ""));

        var drivers = await staffRepo.GetLicencedDriversAsync(today);
        var driverItems = drivers
            .Select(s => new SelectListItem(s.FullName, s.Id.ToString()))
            .Prepend(new SelectListItem("— Select driver —", ""));

        var allStaff = await staffRepo.GetAllAsync();
        var allStaffItems = allStaff
            .Select(s => new SelectListItem(s.FullName, s.Id.ToString()))
            .Prepend(new SelectListItem("— Select staff member —", ""));

        var vm = existing ?? new CreateBookingViewModel();
        vm.AvailableVehicles = vehicleOptions;
        vm.Locations = locations;
        vm.LicencedDrivers = driverItems;
        vm.AllStaff = allStaffItems;
        return vm;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await BuildCreateViewModel(vm);
            return View(vm);
        }

        var vehicle = await vehicleRepo.GetByIdAsync(vm.VehicleId);
        if (vehicle is null || vehicle.Status != VehicleStatus.Available)
        {
            ModelState.AddModelError("VehicleId", "Selected vehicle is not available.");
            await BuildCreateViewModel(vm);
            return View(vm);
        }
        if (await bookingRepo.HasActiveBookingAsync(vm.VehicleId))
        {
            ModelState.AddModelError("VehicleId", "Vehicle already has an active booking.");
            await BuildCreateViewModel(vm);
            return View(vm);
        }

        var driver = await staffRepo.GetByIdAsync(vm.DriverStaffId);
        if (driver is null)
        {
            ModelState.AddModelError("DriverStaffId", "Please select a valid licensed driver.");
            await BuildCreateViewModel(vm);
            return View(vm);
        }

        var bookedByStaff = await staffRepo.GetByIdAsync(vm.BookedByStaffId);
        if (bookedByStaff is null)
        {
            // Fallback: use logged-in user
            if (int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid))
                bookedByStaff = await staffRepo.GetByIdAsync(uid);
        }
        if (bookedByStaff is null)
        {
            ModelState.AddModelError("", "Could not resolve the booking staff member.");
            await BuildCreateViewModel(vm);
            return View(vm);
        }

        var booking = new Booking
        {
            VehicleId    = vm.VehicleId,
            BookedBy     = bookedByStaff.FullName,
            DriverStaffId = vm.DriverStaffId,
            Driver       = driver.FullName,
            Purpose      = vm.Notes,
            Destination  = vm.Destination,
            IsBusinessUse = vm.IsBusinessUse,
            StartTime    = vm.StartTime,
            EndTime      = vm.EndTime,
            OdometerStart = vehicle.Odometer,
            Status       = BookingStatus.Active
        };
        vehicle.Status = VehicleStatus.Booked;
        await vehicleRepo.UpdateAsync(vehicle);
        var created = await bookingRepo.AddAsync(booking);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Booking", EntityId = created.Id, Action = "Created", PerformedBy = bookedByStaff.FullName,
            Details = $"Vehicle {vehicle.LicensePlate} booked by {bookedByStaff.FullName}, driver: {driver.FullName}"
        });
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOdometerStart(int id, int odometerStart)
    {
        var booking = await bookingRepo.GetByIdAsync(id);
        if (booking is null || booking.Status != BookingStatus.Active)
            return NotFound();

        if (odometerStart < 0)
        {
            TempData["Error"] = "Odometer start cannot be negative.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (booking.OdometerEnd.HasValue && odometerStart > booking.OdometerEnd.Value)
        {
            TempData["Error"] = $"Odometer start ({odometerStart:N0} km) cannot exceed the current end value ({booking.OdometerEnd.Value:N0} km).";
            return RedirectToAction(nameof(Details), new { id });
        }

        var previous = booking.OdometerStart;
        booking.OdometerStart = odometerStart;
        await bookingRepo.UpdateAsync(booking);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Booking", EntityId = id, Action = "OdometerStartUpdated",
            PerformedBy = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? booking.BookedBy,
            Details = $"Odometer start changed from {previous:N0} km to {odometerStart:N0} km"
        });

        TempData["Success"] = $"Odometer start updated to {odometerStart:N0} km.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOdometer(int id, int odometerEnd)
    {
        var booking = await bookingRepo.GetByIdAsync(id);
        if (booking is null || booking.Status != BookingStatus.Active)
            return NotFound();

        if (odometerEnd < booking.OdometerStart)
        {
            TempData["Error"] = $"Odometer end ({odometerEnd:N0} km) cannot be less than start ({booking.OdometerStart:N0} km).";
            return RedirectToAction(nameof(Details), new { id });
        }

        booking.OdometerEnd = odometerEnd;
        await bookingRepo.UpdateAsync(booking);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Booking", EntityId = id, Action = "OdometerUpdated",
            PerformedBy = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? booking.BookedBy,
            Details = $"Odometer end set to {odometerEnd:N0} km (start: {booking.OdometerStart:N0} km)"
        });

        TempData["Success"] = $"Odometer updated to {odometerEnd:N0} km.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Complete(int id)
    {
        var booking = await bookingRepo.GetByIdAsync(id);
        if (booking is null || booking.Status != BookingStatus.Active) return NotFound();
        return View(new CompleteBookingViewModel
        {
            Id = id,
            VehicleDisplay = $"{booking.Vehicle.Year} {booking.Vehicle.Make} {booking.Vehicle.Model} ({booking.Vehicle.LicensePlate})",
            BookedBy = booking.BookedBy,
            Purpose = booking.Purpose,
            Destination = booking.Destination ?? string.Empty,
            OdometerStart = booking.OdometerStart,
            OdometerEnd = booking.OdometerStart
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, CompleteBookingViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var booking = await bookingRepo.GetByIdAsync(id);
        if (booking is null) return NotFound();

        booking.Status = BookingStatus.Completed;
        booking.ActualEndTime = DateTime.UtcNow;
        booking.OdometerEnd = vm.OdometerEnd;
        if (vm.Notes is not null) booking.Notes = vm.Notes;

        var vehicle = await vehicleRepo.GetByIdAsync(booking.VehicleId);
        if (vehicle is not null)
        {
            vehicle.Odometer = vm.OdometerEnd;
            vehicle.Status = VehicleStatus.Available;
            await vehicleRepo.UpdateAsync(vehicle);
        }
        await bookingRepo.UpdateAsync(booking);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Booking", EntityId = id, Action = "Completed", PerformedBy = booking.BookedBy,
            Details = $"Odometer end: {vm.OdometerEnd}, km travelled: {vm.OdometerEnd - booking.OdometerStart}"
        });
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await bookingRepo.GetByIdAsync(id);
        if (booking is null || booking.Status != BookingStatus.Active) return NotFound();

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
            EntityType = "Booking", EntityId = id, Action = "Cancelled", PerformedBy = "WebUser",
            Details = $"Booking for vehicle {vehicle?.LicensePlate} cancelled."
        });
        return RedirectToAction(nameof(Index));
    }
}
