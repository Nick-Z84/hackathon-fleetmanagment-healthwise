using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;
using VehicleBookingSystem.Infrastructure.Data;

namespace VehicleBookingSystem.Infrastructure.Repositories;

public class JsonStaffRepository(JsonDataContext ctx) : IStaffRepository
{
    public async Task<IEnumerable<Staff>> GetAllAsync() =>
        (await ctx.Staff.LoadAsync()).OrderBy(s => s.LastName).ThenBy(s => s.FirstName);

    public async Task<IEnumerable<Staff>> GetLicencedDriversAsync(DateOnly asOf)
    {
        var list = await ctx.Staff.LoadAsync();
        return list
            .Where(s => s.HasDriversLicence
                     && s.DriversLicenceExpiry.HasValue
                     && s.DriversLicenceExpiry.Value >= asOf)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName);
    }

    public async Task<Staff?> GetByIdAsync(int id)
    {
        var list = await ctx.Staff.LoadAsync();
        return list.FirstOrDefault(s => s.Id == id);
    }

    public async Task<Staff?> GetByUsernameAsync(string username)
    {
        var normalised = username.Trim().ToLowerInvariant();
        var list       = await ctx.Staff.LoadAsync();
        return list.FirstOrDefault(s => s.Username.ToLowerInvariant() == normalised);
    }

    public async Task<Staff> AddAsync(Staff staff)
    {
        var list  = await ctx.Staff.LoadAsync();
        staff.Id  = list.Count > 0 ? list.Max(s => s.Id) + 1 : 1;
        list.Add(staff);
        await ctx.Staff.SaveAsync(list);
        return staff;
    }

    public async Task<Staff> UpdateAsync(Staff staff)
    {
        var list  = await ctx.Staff.LoadAsync();
        var index = list.FindIndex(s => s.Id == staff.Id);
        if (index >= 0) list[index] = staff;
        await ctx.Staff.SaveAsync(list);
        return staff;
    }

    public async Task DeleteAsync(int id)
    {
        var list = await ctx.Staff.LoadAsync();
        list.RemoveAll(s => s.Id == id);
        await ctx.Staff.SaveAsync(list);
    }
}
