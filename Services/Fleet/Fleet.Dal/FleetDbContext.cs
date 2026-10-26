using Fleet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fleet.Dal;

public class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options)
{
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Courier> Couriers => Set<Courier>();
    public DbSet<CourierLicense> CourierLicenses => Set<CourierLicense>();
    public DbSet<DeliveryZone> DeliveryZones => Set<DeliveryZone>();
    public DbSet<CourierZone> CourierZones => Set<CourierZone>();
    public DbSet<Route> Routes => Set<Route>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(FleetDbContext).Assembly);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(x => x.CreatedAt).CurrentValue = now;
                entry.Property(x => x.CreatedBy).CurrentValue = "fleet-api";
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.UpdatedAt).CurrentValue = now;
                entry.Property(x => x.UpdatedBy).CurrentValue = "fleet-api";
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
