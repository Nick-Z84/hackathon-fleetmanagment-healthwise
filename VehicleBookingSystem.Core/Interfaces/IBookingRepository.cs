using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.Core.Interfaces;

public interface IBookingRepository
{
    Task<IEnumerable<Booking>> GetAllAsync();
    Task<Booking?> GetByIdAsync(int id);
    Task<Booking> AddAsync(Booking booking);
    Task<Booking> UpdateAsync(Booking booking);
    Task<IEnumerable<Booking>> GetByVehicleIdAsync(int vehicleId);
    Task<IEnumerable<Booking>> GetActiveBookingsAsync();
    Task<bool> HasActiveBookingAsync(int vehicleId);
    Task<IEnumerable<int>> GetUnavailableVehicleIdsAsync(DateTime start, DateTime end);
    Task DeleteAsync(int id);
}
