using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.Core.Interfaces;

public interface IAuditLogRepository
{
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId);
    Task AddAsync(AuditLog log);
}
