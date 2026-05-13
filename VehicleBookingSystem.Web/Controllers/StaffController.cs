using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;
using VehicleBookingSystem.Web.Models;

namespace VehicleBookingSystem.Web.Controllers;

public class StaffController(IStaffRepository staffRepo, IPasswordHasher hasher) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var staff = await staffRepo.GetAllAsync();
        // Deduplicate: keep only the first record per username
        var unique = staff
            .GroupBy(s => s.Username.ToLowerInvariant())
            .Select(g => g.First())
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName);
        ViewBag.Today = today;
        return View(unique);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new StaffViewModel());

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Password))
            ModelState.AddModelError("Password", "Password is required for new staff.");
        if (!ModelState.IsValid) return View(vm);

        var (hash, salt) = hasher.Hash(vm.Password!);
        await staffRepo.AddAsync(new Staff
        {
            FirstName = vm.FirstName, LastName = vm.LastName,
            Username = vm.Username, Role = vm.Role,
            PasswordHash = hash, PasswordSalt = salt,
            HasDriversLicence = vm.HasDriversLicence,
            DriversLicenceExpiry = vm.DriversLicenceExpiry
        });
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await staffRepo.GetByIdAsync(id);
        if (s is null) return NotFound();
        return View(new StaffViewModel
        {
            Id = s.Id, FirstName = s.FirstName, LastName = s.LastName,
            Username = s.Username, Role = s.Role,
            HasDriversLicence = s.HasDriversLicence, DriversLicenceExpiry = s.DriversLicenceExpiry
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StaffViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var s = await staffRepo.GetByIdAsync(id);
        if (s is null) return NotFound();

        s.FirstName = vm.FirstName; s.LastName = vm.LastName;
        s.Username  = vm.Username;  s.Role = vm.Role;
        s.HasDriversLicence = vm.HasDriversLicence;
        s.DriversLicenceExpiry = vm.DriversLicenceExpiry;

        // Only update password if a new one was provided
        if (!string.IsNullOrWhiteSpace(vm.Password))
        {
            var (hash, salt) = hasher.Hash(vm.Password);
            s.PasswordHash = hash;
            s.PasswordSalt = salt;
        }

        await staffRepo.UpdateAsync(s);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await staffRepo.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
