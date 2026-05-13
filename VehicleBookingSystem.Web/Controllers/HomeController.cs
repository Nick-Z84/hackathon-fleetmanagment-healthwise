using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Web.Models;

namespace VehicleBookingSystem.Web.Controllers;

public class HomeController(
    IVehicleRepository vehicleRepo,
    IBookingRepository bookingRepo) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today     = DateOnly.FromDateTime(DateTime.Today);
        var windowEnd = today.AddDays(60);
        var now       = DateTime.Now;

        var allVehicles = (await vehicleRepo.GetAllAsync()).ToList();
        var allBookings = (await bookingRepo.GetAllAsync()).ToList();
        var compliance  = await vehicleRepo.GetComplianceAlertsAsync(today, windowEnd);

        // Metric card counts
        var highMileage  = allVehicles.Count(v => v.Odometer > 100_000);
        var highAge      = allVehicles.Count(v => today.Year - v.Year >= 5);
        var overdueReg   = allVehicles.Count(v => v.RegistrationExpiry.HasValue  && v.RegistrationExpiry.Value  <= today);
        var overdueIns   = allVehicles.Count(v => v.InsuranceExpiry.HasValue     && v.InsuranceExpiry.Value     <= today);
        var overdueSvc   = allVehicles.Count(v => v.NextServiceDate.HasValue     && v.NextServiceDate.Value     <= today);

        // Upcoming and current: active bookings whose end time is still in the future
        // Current bookings (already started) ? sort by EndTime asc so soonest-ending appears first
        // Upcoming bookings (not yet started)  ? sort by StartTime asc so next-starting appears first
        var activeBookings = allBookings
            .Where(b => b.Status == Core.Models.BookingStatus.Active
                     && (b.EndTime == null || b.EndTime > now))
            .ToList();

        var currentBookings  = activeBookings
            .Where(b => b.StartTime <= now)
            .OrderBy(b => b.EndTime ?? DateTime.MaxValue);

        var upcomingBookings = activeBookings
            .Where(b => b.StartTime > now)
            .OrderBy(b => b.StartTime);

        var sortedActiveBookings = currentBookings.Concat(upcomingBookings).ToList();

        // Driver usage — computed from all bookings that have odometer data
        var driverUsage = allBookings
            .Where(b => !string.IsNullOrEmpty(b.Driver ?? b.BookedBy) && b.OdometerEnd.HasValue)
            .GroupBy(b => b.Driver ?? b.BookedBy)
            .Select(g =>
            {
                int TripKm(bool business) => g
                    .Where(b => b.IsBusinessUse == business && b.OdometerEnd.HasValue)
                    .Sum(b => b.OdometerEnd!.Value - b.OdometerStart);

                return new DriverUsageSummary(
                    DriverName:  g.Key,
                    TotalTrips:  g.Count(),
                    TotalKm:     g.Where(b => b.OdometerEnd.HasValue).Sum(b => b.OdometerEnd!.Value - b.OdometerStart),
                    BusinessKm:  TripKm(true),
                    PrivateKm:   TripKm(false),
                    LastTrip:    g.Max(b => (DateTime?)b.StartTime)
                );
            })
            .OrderByDescending(d => d.TotalKm)
            .ToList();

        var vm = new DashboardViewModel
        {
            TotalVehicles    = allVehicles.Count,
            HighMileageCount = highMileage,
            HighAgeCount     = highAge,
            OverdueRegCount  = overdueReg,
            OverdueInsCount  = overdueIns,
            OverdueSvcCount  = overdueSvc,
            ActiveBookings   = sortedActiveBookings,
            ComplianceAlerts = compliance,
            DriverUsage      = driverUsage
        };
        return View(vm);
    }

    /// <summary>Returns available-vehicle count per calendar day for a given month.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMonthAvailability(int year, int month)
    {
        var allVehicles = (await vehicleRepo.GetAllAsync()).ToList();
        var total       = allVehicles.Count;
        var firstDay    = new DateTime(year, month, 1);
        var lastDay     = firstDay.AddMonths(1);

        var bookings = (await bookingRepo.GetAllAsync())
            .Where(b => b.Status == Core.Models.BookingStatus.Active
                     && b.StartTime < lastDay
                     && (b.EndTime ?? b.StartTime.AddDays(1)) > firstDay)
            .ToList();

        var result = new Dictionary<string, int>();
        for (var d = firstDay; d < lastDay; d = d.AddDays(1))
        {
            var dayEnd      = d.AddDays(1);
            var bookedCount = bookings.Count(b =>
                b.StartTime < dayEnd && (b.EndTime ?? b.StartTime.AddDays(1)) > d);
            result[d.ToString("yyyy-MM-dd")] = total - bookedCount;
        }
        return Json(new { total, days = result });
    }

    /// <summary>Returns available-vehicle count per location for a given date.</summary>
    [HttpGet]
    public async Task<IActionResult> GetLocationAvailability(string date)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
            parsedDate = DateOnly.FromDateTime(DateTime.Today);

        var dayStart      = parsedDate.ToDateTime(TimeOnly.MinValue);
        var dayEnd        = dayStart.AddDays(1);
        var unavailableIds = (await bookingRepo.GetUnavailableVehicleIdsAsync(dayStart, dayEnd)).ToHashSet();
        var allVehicles   = await vehicleRepo.GetAllAsync();

        var result = allVehicles
            .GroupBy(v => v.Location ?? "Unknown")
            .Select(g => new
            {
                location  = g.Key,
                total     = g.Count(),
                available = g.Count(v => !unavailableIds.Contains(v.Id))
            })
            .OrderBy(x => x.location);

        return Json(result);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
