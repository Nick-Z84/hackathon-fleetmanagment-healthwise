using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;
using VehicleBookingSystem.Infrastructure.Data;

namespace VehicleBookingSystem.Infrastructure.Repositories;

public class JsonFuelReceiptRepository(JsonDataContext ctx) : IFuelReceiptRepository
{
    private async Task<List<FuelReceipt>> LoadWithNavAsync()
    {
        var receipts = await ctx.FuelReceipts.LoadAsync();
        var vehicles = await ctx.Vehicles.LoadAsync();
        var map      = vehicles.ToDictionary(v => v.Id);
        foreach (var r in receipts)
            if (map.TryGetValue(r.VehicleId, out var v)) r.Vehicle = v;
        return receipts;
    }

    public async Task<IEnumerable<FuelReceipt>> GetAllAsync() => await LoadWithNavAsync();

    public async Task<IEnumerable<FuelReceipt>> GetByVehicleIdAsync(int vehicleId)
    {
        var list = await LoadWithNavAsync();
        return list.Where(r => r.VehicleId == vehicleId);
    }

    public async Task<FuelReceipt?> GetByIdAsync(int id)
    {
        var list = await LoadWithNavAsync();
        return list.FirstOrDefault(r => r.Id == id);
    }

    public async Task<FuelReceipt> AddAsync(FuelReceipt receipt)
    {
        var list    = await ctx.FuelReceipts.LoadAsync();
        receipt.Id  = list.Count > 0 ? list.Max(r => r.Id) + 1 : 1;
        list.Add(receipt);
        await ctx.FuelReceipts.SaveAsync(list);
        return receipt;
    }

    public async Task<FuelReceipt> UpdateAsync(FuelReceipt receipt)
    {
        var list  = await ctx.FuelReceipts.LoadAsync();
        var index = list.FindIndex(r => r.Id == receipt.Id);
        if (index >= 0) list[index] = receipt;
        await ctx.FuelReceipts.SaveAsync(list);
        return receipt;
    }

    public async Task DeleteAsync(int id)
    {
        var list = await ctx.FuelReceipts.LoadAsync();
        list.RemoveAll(r => r.Id == id);
        await ctx.FuelReceipts.SaveAsync(list);
    }
}
