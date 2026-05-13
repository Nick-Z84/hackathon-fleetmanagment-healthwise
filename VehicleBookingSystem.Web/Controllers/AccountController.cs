using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Web.Models;

namespace VehicleBookingSystem.Web.Controllers;

[AllowAnonymous]
public class AccountController(
    IStaffRepository staffRepo,
    IPasswordHasher hasher) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(returnUrl ?? "/");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        var staff = await staffRepo.GetByUsernameAsync(vm.Username.Trim());
        if (staff is null || !hasher.Verify(vm.Password, staff.PasswordHash, staff.PasswordSalt))
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, staff.Id.ToString()),
            new(ClaimTypes.Name,           staff.Username),
            new(ClaimTypes.GivenName,      staff.FullName),
            new(ClaimTypes.Role,           staff.Role.ToString())
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = vm.RememberMe });

        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
