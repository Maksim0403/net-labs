using System.ComponentModel.DataAnnotations;

namespace Fleet.Bll.Dto;

// ---------- Courier ----------
public class CourierDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public long WarehouseId { get; set; }
}

public class ZoneAssignmentDto
{
    public long ZoneId { get; set; }
    public string ZoneName { get; set; } = "";
    public DateTime AssignedAt { get; set; }
}

public class CourierDetailsDto : CourierDto
{
    public string WarehouseCode { get; set; } = "";
    public string? LicenseNumber { get; set; }
    public string? LicenseCategory { get; set; }
    public DateOnly? LicenseExpiresOn { get; set; }
    public List<ZoneAssignmentDto> Zones { get; set; } = new();
}

// Прості правила — DataAnnotations; складніші (формат телефону) — FluentValidation.
public class CreateCourierDto
{
    [Range(1, long.MaxValue)]
    public long WarehouseId { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = "";

    [Required, StringLength(20)]
    public string Phone { get; set; } = "";
}

public class AssignZoneDto
{
    [Range(1, long.MaxValue)]
    public long ZoneId { get; set; }
}

// ---------- Warehouse ----------
public class WarehouseDto
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";
}

public class WarehouseWithVehiclesDto : WarehouseDto
{
    public List<VehicleDto> Vehicles { get; set; } = new();
}

public class CreateWarehouseDto
{
    [Required, StringLength(20, MinimumLength = 2)]
    public string Code { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = "";

    [Required, StringLength(300, MinimumLength = 3)]
    public string Address { get; set; } = "";
}

// ---------- Vehicle ----------
public class VehicleDto
{
    public long Id { get; set; }
    public long WarehouseId { get; set; }
    public string PlateNumber { get; set; } = "";
    public string VehicleType { get; set; } = "";
    public decimal CapacityKg { get; set; }
}

public class CreateVehicleDto
{
    [Range(1, long.MaxValue)]
    public long WarehouseId { get; set; }

    [Required, StringLength(20, MinimumLength = 3)]
    public string PlateNumber { get; set; } = "";

    [StringLength(30)]
    public string VehicleType { get; set; } = "Van";

    [Range(0.01, 100000)]
    public decimal CapacityKg { get; set; }
}

public class ChangeCapacityDto
{
    [Range(0.01, 100000)]
    public decimal CapacityKg { get; set; }
}

// ---------- Route ----------
public class RouteDto
{
    public long Id { get; set; }
    public long CourierId { get; set; }
    public long VehicleId { get; set; }
    public long ZoneId { get; set; }
    public DateOnly PlannedDate { get; set; }
    public string Status { get; set; } = "";
    public decimal PlannedDistanceKm { get; set; }
}

public class CreateRouteDto
{
    [Range(1, long.MaxValue)] public long CourierId { get; set; }
    [Range(1, long.MaxValue)] public long VehicleId { get; set; }
    [Range(1, long.MaxValue)] public long ZoneId { get; set; }
    public DateOnly PlannedDate { get; set; }

    [Range(0, 100000)]
    public decimal PlannedDistanceKm { get; set; }
}
