using Fleet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fleet.Dal.Configurations;

public abstract class AuditableConfiguration<T> : IEntityTypeConfiguration<T> where T : AuditableEntity
{
    public virtual void Configure(EntityTypeBuilder<T> b)
    {
        b.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.CreatedBy).HasMaxLength(100).HasDefaultValue("system");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        b.Property(x => x.UpdatedBy).HasMaxLength(100);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class WarehouseConfiguration : AuditableConfiguration<Warehouse>
{
    public override void Configure(EntityTypeBuilder<Warehouse> b)
    {
        base.Configure(b);
        b.ToTable("Warehouses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("WarehouseId");
        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.Property(x => x.City).HasMaxLength(100).IsRequired();
        b.Property(x => x.Address).HasMaxLength(300).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.City).HasDatabaseName("IX_Warehouses_City");
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasData(
            new { Id = 1L, Code = "CV-01", City = "Чернівці", Address = "вул. Руська, 100", CreatedAt = new DateTime(2024, 1, 1), CreatedBy = "seed", IsDeleted = false },
            new { Id = 2L, Code = "KY-01", City = "Київ", Address = "пр. Перемоги, 50", CreatedAt = new DateTime(2024, 1, 1), CreatedBy = "seed", IsDeleted = false }
        );
    }
}

public sealed class VehicleConfiguration : AuditableConfiguration<Vehicle>
{
    public override void Configure(EntityTypeBuilder<Vehicle> b)
    {
        base.Configure(b);
        b.ToTable("Vehicles", t => t.HasCheckConstraint("CK_Vehicles_Capacity", "[CapacityKg] > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("VehicleId");
        b.Property(x => x.PlateNumber).HasMaxLength(20).IsRequired();
        b.Property(x => x.VehicleType).HasMaxLength(30).HasDefaultValue("Van");
        b.Property(x => x.CapacityKg).HasPrecision(9, 2);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.PlateNumber).IsUnique();
        b.HasOne(x => x.Warehouse).WithMany(x => x.Vehicles).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CourierConfiguration : AuditableConfiguration<Courier>
{
    public override void Configure(EntityTypeBuilder<Courier> b)
    {
        base.Configure(b);
        b.ToTable("Couriers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("CourierId");
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.Phone).IsUnique();
        b.HasOne(x => x.Warehouse).WithMany(x => x.Couriers).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CourierLicenseConfiguration : IEntityTypeConfiguration<CourierLicense>
{
    public void Configure(EntityTypeBuilder<CourierLicense> b)
    {
        b.ToTable("CourierLicenses");
        b.HasKey(x => x.CourierId);
        b.Property(x => x.Number).HasMaxLength(30).IsRequired();
        b.Property(x => x.Category).HasMaxLength(5).HasDefaultValue("B");
        b.Property(x => x.ExpiresOn).HasColumnType("date");
        b.HasIndex(x => x.Number).IsUnique();
        b.HasOne(x => x.Courier).WithOne(x => x.License).HasForeignKey<CourierLicense>(x => x.CourierId).OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(x => !x.Courier.IsDeleted);
    }
}

public sealed class DeliveryZoneConfiguration : AuditableConfiguration<DeliveryZone>
{
    public override void Configure(EntityTypeBuilder<DeliveryZone> b)
    {
        base.Configure(b);
        b.ToTable("DeliveryZones");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ZoneId");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
        b.HasData(
            new { Id = 1L, Name = "Центр", CreatedAt = new DateTime(2024, 1, 1), CreatedBy = "seed", IsDeleted = false },
            new { Id = 2L, Name = "Околиці", CreatedAt = new DateTime(2024, 1, 1), CreatedBy = "seed", IsDeleted = false },
            new { Id = 3L, Name = "Міжміська", CreatedAt = new DateTime(2024, 1, 1), CreatedBy = "seed", IsDeleted = false }
        );
    }
}

public sealed class CourierZoneConfiguration : IEntityTypeConfiguration<CourierZone>
{
    public void Configure(EntityTypeBuilder<CourierZone> b)
    {
        b.ToTable("CourierZones");
        b.HasKey(x => new { x.CourierId, x.ZoneId });
        b.Property(x => x.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.HasOne(x => x.Courier).WithMany(x => x.CourierZones).HasForeignKey(x => x.CourierId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Zone).WithMany(x => x.CourierZones).HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(x => !x.Courier.IsDeleted && !x.Zone.IsDeleted);
    }
}

public sealed class RouteConfiguration : AuditableConfiguration<Route>
{
    public override void Configure(EntityTypeBuilder<Route> b)
    {
        base.Configure(b);
        b.ToTable("Routes", t =>
        {
            t.HasCheckConstraint("CK_Routes_Status", "[Status] IN ('Planned','InProgress','Completed','Cancelled')");
            t.HasCheckConstraint("CK_Routes_Distance", "[PlannedDistanceKm] >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("RouteId");
        b.Property(x => x.PlannedDate).HasColumnType("date");
        b.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Planned");
        b.Property(x => x.PlannedDistanceKm).HasPrecision(8, 2).HasDefaultValue(0);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.CourierId, x.PlannedDate }).HasDatabaseName("IX_Routes_Courier_Date");
        b.HasIndex(x => new { x.Status, x.PlannedDate }).HasDatabaseName("IX_Routes_Status_Date");
        b.HasOne(x => x.Courier).WithMany(x => x.Routes).HasForeignKey(x => x.CourierId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Zone).WithMany(x => x.Routes).HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Restrict);
    }
}

