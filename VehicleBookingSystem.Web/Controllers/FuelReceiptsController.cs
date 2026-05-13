using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;
using VehicleBookingSystem.Web.Models;

namespace VehicleBookingSystem.Web.Controllers;

public class FuelReceiptsController(
    IFuelReceiptRepository fuelRepo,
    IVehicleRepository vehicleRepo,
    IAuditLogRepository auditRepo,
    IWebHostEnvironment env) : Controller
{
    public async Task<IActionResult> Index(int? vehicles, DateOnly? dateFrom, DateOnly? dateTo, string? uploadedBy)
    {
        var receipts = vehicles.HasValue
            ? await fuelRepo.GetByVehicleIdAsync(vehicles.Value)
            : await fuelRepo.GetAllAsync();

        if (dateFrom.HasValue)
            receipts = receipts.Where(r => r.Date >= dateFrom.Value);
        if (dateTo.HasValue)
            receipts = receipts.Where(r => r.Date <= dateTo.Value);
        if (!string.IsNullOrWhiteSpace(uploadedBy))
            receipts = receipts.Where(r => (r.UploadedBy ?? "").Contains(uploadedBy.Trim(), StringComparison.OrdinalIgnoreCase));

        var allVehicles = await vehicleRepo.GetAllAsync();
        ViewBag.VehicleId  = vehicles;
        ViewBag.DateFrom   = dateFrom?.ToString("yyyy-MM-dd");
        ViewBag.DateTo     = dateTo?.ToString("yyyy-MM-dd");
        ViewBag.UploadedBy = uploadedBy;
        ViewBag.Vehicles   = allVehicles.Select(v =>
            new SelectListItem($"{v.Year} {v.Make} {v.Model} — {v.LicensePlate}", v.Id.ToString()));
        ViewBag.Uploaders  = (await fuelRepo.GetAllAsync())
            .Select(r => r.UploadedBy).Where(u => !string.IsNullOrEmpty(u)).Distinct().OrderBy(u => u).ToList();

        return View(receipts.OrderByDescending(r => r.Date));
    }

    public async Task<IActionResult> Create(int? vehicles)
    {
        var allVehicles = await vehicleRepo.GetAllAsync();
        var vehicleList  = allVehicles.ToList();

        string? preSelectedDisplay = null;
        string? qrCodeUrl = null;

        if (vehicles.HasValue)
        {
            var v = vehicleList.FirstOrDefault(x => x.Id == vehicles.Value);
            if (v is not null)
            {
                preSelectedDisplay = $"{v.Year} {v.Make} {v.Model} — {v.LicensePlate}";
                // QR URL uses the exact format requested
                qrCodeUrl = $"https://localhost:44399/FuelReceipts/Create?vehicles={vehicles.Value}";
            }
        }

        var vm = new AddFuelReceiptViewModel
        {
            VehicleId                 = vehicles ?? 0,
            SourceVehicleId           = vehicles ?? 0,
            UploadedBy                = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? string.Empty,
            PreSelectedVehicleDisplay = preSelectedDisplay,
            QrCodeUrl                 = qrCodeUrl,
            Vehicles                  = vehicleList.Select(v =>
                new SelectListItem($"{v.Year} {v.Make} {v.Model} — {v.LicensePlate}", v.Id.ToString()))
        };
        return View(vm);
    }

    /// <summary>Renders a styled paper-receipt view for a single fuel receipt entry.</summary>
    public async Task<IActionResult> ViewReceipt(int id)
    {
        var receipt = await fuelRepo.GetByIdAsync(id);
        if (receipt is null) return NotFound();
        return View(receipt);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AddFuelReceiptViewModel vm)
    {
        var vehicles = await vehicleRepo.GetAllAsync();
        vm.Vehicles = vehicles.Select(v =>
            new SelectListItem($"{v.Year} {v.Make} {v.Model} — {v.LicensePlate}", v.Id.ToString()));

        if (!ModelState.IsValid) return View(vm);

        string? fileName = null;
        string? filePath = null;

        if (vm.ReceiptFile is { Length: > 0 })
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var ext = Path.GetExtension(vm.ReceiptFile.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError("ReceiptFile", "Only JPG, PNG, or PDF files are accepted.");
                return View(vm);
            }

            var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "fuel");
            Directory.CreateDirectory(uploadsDir);
            fileName = $"{Guid.NewGuid()}{ext}";
            filePath = Path.Combine("uploads", "fuel", fileName);
            using var stream = new FileStream(Path.Combine(env.WebRootPath, filePath), FileMode.Create);
            await vm.ReceiptFile.CopyToAsync(stream);
        }

        var receipt = new FuelReceipt
        {
            VehicleId = vm.VehicleId,
            Date = vm.Date,
            Litres = vm.Litres,
            CostPerLitre = vm.CostPerLitre,
            TotalCost = vm.TotalCost,
            Station = vm.Station,
            OdometerReading = vm.OdometerReading,
            UploadedBy = vm.UploadedBy,
            ReceiptFileName = vm.ReceiptFile?.FileName,
            ReceiptFilePath = filePath
        };

        var created = await fuelRepo.AddAsync(receipt);
        await auditRepo.AddAsync(new AuditLog
        {
            EntityType = "Vehicle", EntityId = vm.VehicleId, Action = "FuelReceipt",
            PerformedBy = vm.UploadedBy,
            Details = $"{vm.Litres}L @ ${vm.CostPerLitre}/L = ${vm.TotalCost} on {vm.Date}"
        });

        // When opened from a Vehicle Details page, return there after saving
        if (vm.SourceVehicleId > 0)
            return RedirectToAction("Details", "Vehicles", new { id = vm.SourceVehicleId });

        return RedirectToAction(nameof(Index), new { vehicles = vm.VehicleId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int vehicleId)
    {
        var receipt = await fuelRepo.GetByIdAsync(id);
        if (receipt is not null && receipt.ReceiptFilePath is not null)
        {
            var fullPath = Path.Combine(env.WebRootPath, receipt.ReceiptFilePath);
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
        await fuelRepo.DeleteAsync(id);
        return RedirectToAction(nameof(Index), new { vehicleId });
    }
}
