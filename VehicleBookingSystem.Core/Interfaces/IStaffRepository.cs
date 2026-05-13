using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.Core.Interfaces;

public interface IStaffRepository
{
    Task<IEnumerable<Staff>> GetAllAsync();
    Task<IEnumerable<Staff>> GetLicencedDriversAsync(DateOnly asOf);
    Task<Staff?> GetByIdAsync(int id);
    Task<Staff?> GetByUsernameAsync(string username);
    Task<Staff> AddAsync(Staff staff);
    Task<Staff> UpdateAsync(Staff staff);
    Task DeleteAsync(int id);
}

