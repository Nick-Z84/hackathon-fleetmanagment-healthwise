using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.Core.Interfaces;

public interface IFuelReceiptRepository
{
    Task<IEnumerable<FuelReceipt>> GetAllAsync();
    Task<IEnumerable<FuelReceipt>> GetByVehicleIdAsync(int vehicleId);
    Task<FuelReceipt?> GetByIdAsync(int id);
    Task<FuelReceipt> AddAsync(FuelReceipt receipt);
    Task DeleteAsync(int id);
}
