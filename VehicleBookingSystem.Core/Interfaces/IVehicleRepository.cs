using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.Core.Interfaces;

public interface IVehicleRepository
{
    Task<IEnumerable<Vehicle>> GetAllAsync();
    Task<Vehicle?> GetByIdAsync(int id);
    Task<Vehicle> AddAsync(Vehicle vehicle);
    Task<Vehicle> UpdateAsync(Vehicle vehicle);
    Task DeleteAsync(int id);
    Task<IEnumerable<Vehicle>> GetByStatusAsync(VehicleStatus status);
    Task<IEnumerable<Vehicle>> GetDueForServiceAsync(DateOnly asOf);
    Task<IEnumerable<Vehicle>> GetUpcomingComplianceAsync(DateOnly windowEnd);
    Task<IEnumerable<Vehicle>> GetComplianceAlertsAsync(DateOnly today, DateOnly windowEnd);
}
