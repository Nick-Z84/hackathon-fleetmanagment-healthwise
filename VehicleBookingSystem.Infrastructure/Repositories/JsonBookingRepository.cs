using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;
using VehicleBookingSystem.Infrastructure.Data;

namespace VehicleBookingSystem.Infrastructure.Repositories;

public class JsonBookingRepository(JsonDataContext ctx) : IBookingRepository
{
    // Populate navigation properties after loading
    private async Task<List<Booking>> LoadWithNavAsync()
    {
        var bookings = await ctx.Bookings.LoadAsync();
        var vehicles = await ctx.Vehicles.LoadAsync();
        var staff    = await ctx.Staff.LoadAsync();

        var vehicleMap = vehicles.ToDictionary(v => v.Id);
        var staffMap   = staff.ToDictionary(s => s.Id);

        foreach (var b in bookings)
        {
            if (vehicleMap.TryGetValue(b.VehicleId, out var v))
                b.Vehicle = v;
            if (b.DriverStaffId.HasValue && staffMap.TryGetValue(b.DriverStaffId.Value, out var s))
                b.DriverStaff = s;
        }
        return bookings;
    }

    public async Task<IEnumerable<Booking>> GetAllAsync() => await LoadWithNavAsync();

    public async Task<Booking?> GetByIdAsync(int id)
    {
        var list = await LoadWithNavAsync();
        return list.FirstOrDefault(b => b.Id == id);
    }

    public async Task<IEnumerable<Booking>> GetByVehicleIdAsync(int vehicleId)
    {
        var list = await LoadWithNavAsync();
        return list.Where(b => b.VehicleId == vehicleId);
    }

    public async Task<IEnumerable<Booking>> GetActiveBookingsAsync()
    {
        var list = await LoadWithNavAsync();
        return list.Where(b => b.Status == BookingStatus.Active);
    }

    public async Task<bool> HasActiveBookingAsync(int vehicleId)
    {
        var list = await ctx.Bookings.LoadAsync();
        return list.Any(b => b.VehicleId == vehicleId && b.Status == BookingStatus.Active);
    }

    public async Task<IEnumerable<int>> GetUnavailableVehicleIdsAsync(DateTime start, DateTime end)
    {
        var list = await ctx.Bookings.LoadAsync();
        return list
            .Where(b => b.Status == BookingStatus.Active
                     && b.StartTime < end
                     && (b.EndTime ?? b.StartTime.AddDays(1)) > start)
            .Select(b => b.VehicleId)
            .Distinct()
            .ToList();
    }

    public async Task<Booking> AddAsync(Booking booking)
    {
        var list     = await ctx.Bookings.LoadAsync();
        booking.Id   = list.Count > 0 ? list.Max(b => b.Id) + 1 : 1;
        list.Add(booking);
        await ctx.Bookings.SaveAsync(list);
        return booking;
    }

    public async Task<Booking> UpdateAsync(Booking booking)
    {
        var list  = await ctx.Bookings.LoadAsync();
        var index = list.FindIndex(b => b.Id == booking.Id);
        if (index >= 0) list[index] = booking;
        await ctx.Bookings.SaveAsync(list);
        return booking;
    }

    public async Task DeleteAsync(int id)
    {
        var list = await ctx.Bookings.LoadAsync();
        list.RemoveAll(b => b.Id == id);
        await ctx.Bookings.SaveAsync(list);
    }
}
