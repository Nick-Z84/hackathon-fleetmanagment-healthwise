using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;
using VehicleBookingSystem.Infrastructure.Data;

namespace VehicleBookingSystem.Infrastructure.Repositories;

public class JsonAuditLogRepository(JsonDataContext ctx) : IAuditLogRepository
{
    public async Task<IEnumerable<AuditLog>> GetAllAsync() =>
        (await ctx.AuditLogs.LoadAsync()).OrderByDescending(a => a.Timestamp);

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId)
    {
        var list = await ctx.AuditLogs.LoadAsync();
        return list.Where(a => a.EntityType == entityType && a.EntityId == entityId)
                   .OrderByDescending(a => a.Timestamp);
    }

    public async Task AddAsync(AuditLog log)
    {
        var list = await ctx.AuditLogs.LoadAsync();
        log.Id   = list.Count > 0 ? list.Max(a => a.Id) + 1 : 1;
        list.Add(log);
        await ctx.AuditLogs.SaveAsync(list);
    }
}
