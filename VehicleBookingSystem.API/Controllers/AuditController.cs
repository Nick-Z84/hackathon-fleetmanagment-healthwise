using Microsoft.AspNetCore.Mvc;
using VehicleBookingSystem.Core.Interfaces;

namespace VehicleBookingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController(IAuditLogRepository auditRepo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await auditRepo.GetAllAsync());

    [HttpGet("{entityType}/{entityId:int}")]
    public async Task<IActionResult> GetByEntity(string entityType, int entityId) =>
        Ok(await auditRepo.GetByEntityAsync(entityType, entityId));
}
