using VehicleBookingSystem.Core.Models;
using VehicleBookingSystem.Infrastructure.Services;

namespace VehicleBookingSystem.Infrastructure.Data;

/// <summary>Singleton that owns all encrypted JSON file stores.</summary>
public sealed class JsonDataContext
{
    public JsonFileStore<Vehicle>     Vehicles     { get; }
    public JsonFileStore<Booking>     Bookings     { get; }
    public JsonFileStore<Staff>       Staff        { get; }
    public JsonFileStore<FuelReceipt> FuelReceipts { get; }
    public JsonFileStore<AuditLog>    AuditLogs    { get; }

    public JsonDataContext(string dataDirectory, DataEncryptor encryptor)
    {
        Vehicles     = new(dataDirectory, "vehicles.dat",     encryptor);
        Bookings     = new(dataDirectory, "bookings.dat",     encryptor);
        Staff        = new(dataDirectory, "staff.dat",        encryptor);
        FuelReceipts = new(dataDirectory, "fuelreceipts.dat", encryptor);
        AuditLogs    = new(dataDirectory, "auditlogs.dat",    encryptor);
    }
}
