using Fleet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fleet.Dal.Seeding;

public static class DbInitializer
{
    public static async Task SeedAsync(FleetDbContext db, CancellationToken ct = default)
    {
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "CV-01", ct);
        if (warehouse is null)
        {
            return;
        }

        if (!await db.Vehicles.AnyAsync(v => v.PlateNumber == "AA1234BT", ct))
        {
            var vehicle = new Vehicle(warehouse.Id, "AA1234BT", "Van", 800);
            await db.Vehicles.AddAsync(vehicle, ct);
        }

        if (!await db.Couriers.AnyAsync(c => c.Phone == "+380991112233", ct))
        {
            var courier = new Courier(warehouse.Id, "Андрій Мельник", "+380991112233");
            courier.AssignLicense(new CourierLicense("AB123456", "B", new DateOnly(2028, 1, 1)));

            var zone = await db.DeliveryZones.FirstOrDefaultAsync(x => x.Name == "Центр", ct);
            if (zone is not null)
            {
                courier.AssignToZone(zone);
            }

            await db.Couriers.AddAsync(courier, ct);
        }

        await db.SaveChangesAsync(ct);
    }
}

