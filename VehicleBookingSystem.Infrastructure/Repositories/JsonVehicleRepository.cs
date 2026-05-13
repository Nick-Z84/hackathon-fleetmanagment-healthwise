using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;
using VehicleBookingSystem.Infrastructure.Data;

namespace VehicleBookingSystem.Infrastructure.Repositories;

public class JsonVehicleRepository(JsonDataContext ctx) : IVehicleRepository
{
    public async Task<IEnumerable<Vehicle>> GetAllAsync() =>
        await ctx.Vehicles.LoadAsync();

    public async Task<Vehicle?> GetByIdAsync(int id)
    {
        var list = await ctx.Vehicles.LoadAsync();
        return list.FirstOrDefault(v => v.Id == id);
    }

    public async Task<IEnumerable<Vehicle>> GetByStatusAsync(VehicleStatus status)
    {
        var list = await ctx.Vehicles.LoadAsync();
        return list.Where(v => v.Status == status);
    }

    public async Task<IEnumerable<Vehicle>> GetDueForServiceAsync(DateOnly asOf)
    {
        var list = await ctx.Vehicles.LoadAsync();
        return list.Where(v => v.NextServiceDate.HasValue && v.NextServiceDate.Value <= asOf);
    }

    public async Task<IEnumerable<Vehicle>> GetUpcomingComplianceAsync(DateOnly windowEnd)
    {
        var list = await ctx.Vehicles.LoadAsync();
        return list.Where(v => v.Status != VehicleStatus.Retired &&
            ((v.RegistrationExpiry.HasValue && v.RegistrationExpiry.Value <= windowEnd) ||
             (v.InsuranceExpiry.HasValue    && v.InsuranceExpiry.Value    <= windowEnd)));
    }

    public async Task<IEnumerable<Vehicle>> GetComplianceAlertsAsync(DateOnly today, DateOnly windowEnd)
    {
        var list = await ctx.Vehicles.LoadAsync();
        return list.Where(v => v.Status != VehicleStatus.Retired &&
            ((v.NextServiceDate.HasValue      && v.NextServiceDate.Value      <= today) ||
             (v.RegistrationExpiry.HasValue   && v.RegistrationExpiry.Value   <= windowEnd) ||
             (v.InsuranceExpiry.HasValue      && v.InsuranceExpiry.Value      <= windowEnd)));
    }

    public async Task<Vehicle> AddAsync(Vehicle vehicle)
    {
        var list   = await ctx.Vehicles.LoadAsync();
        vehicle.Id = list.Count > 0 ? list.Max(v => v.Id) + 1 : 1;
        list.Add(vehicle);
        await ctx.Vehicles.SaveAsync(list);
        return vehicle;
    }

    public async Task<Vehicle> UpdateAsync(Vehicle vehicle)
    {
        var list  = await ctx.Vehicles.LoadAsync();
        var index = list.FindIndex(v => v.Id == vehicle.Id);
        if (index >= 0) list[index] = vehicle;
        await ctx.Vehicles.SaveAsync(list);
        return vehicle;
    }

    public async Task DeleteAsync(int id)
    {
        var list = await ctx.Vehicles.LoadAsync();
        list.RemoveAll(v => v.Id == id);
        await ctx.Vehicles.SaveAsync(list);
    }
}
