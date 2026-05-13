using Microsoft.AspNetCore.Mvc;
using VehicleBookingSystem.Core.Interfaces;

namespace VehicleBookingSystem.Web.Controllers;

public class AuditController(IAuditLogRepository auditRepo) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await auditRepo.GetAllAsync());

    public async Task<IActionResult> Entity(string entityType, int entityId)
    {
        ViewBag.EntityType = entityType;
        ViewBag.EntityId = entityId;
        return View(await auditRepo.GetByEntityAsync(entityType, entityId));
    }
}
