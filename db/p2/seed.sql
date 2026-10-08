USE FleetDb;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Warehouses WHERE Code = N'CV-01')
    INSERT dbo.Warehouses (Code, City, Address) VALUES (N'CV-01', N'Чернівці', N'вул. Руська, 100');

IF NOT EXISTS (SELECT 1 FROM dbo.Warehouses WHERE Code = N'KY-01')
    INSERT dbo.Warehouses (Code, City, Address) VALUES (N'KY-01', N'Київ', N'пр. Перемоги, 50');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DeliveryZones WHERE Name = N'Центр')
    INSERT dbo.DeliveryZones (Name) VALUES (N'Центр');
IF NOT EXISTS (SELECT 1 FROM dbo.DeliveryZones WHERE Name = N'Околиці')
    INSERT dbo.DeliveryZones (Name) VALUES (N'Околиці');
IF NOT EXISTS (SELECT 1 FROM dbo.DeliveryZones WHERE Name = N'Міжміська')
    INSERT dbo.DeliveryZones (Name) VALUES (N'Міжміська');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE PlateNumber = N'AA1234BT')
    INSERT dbo.Vehicles (WarehouseId, PlateNumber, VehicleType, CapacityKg)
    SELECT WarehouseId, N'AA1234BT', N'Van', 800
    FROM dbo.Warehouses WHERE Code = N'CV-01';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Couriers WHERE Phone = N'+380991112233')
    INSERT dbo.Couriers (WarehouseId, FullName, Phone)
    SELECT WarehouseId, N'Андрій Мельник', N'+380991112233'
    FROM dbo.Warehouses WHERE Code = N'CV-01';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CourierLicenses WHERE Number = N'AB123456')
    INSERT dbo.CourierLicenses (CourierId, Number, Category, ExpiresOn)
    SELECT CourierId, N'AB123456', N'B', '2028-01-01'
    FROM dbo.Couriers WHERE Phone = N'+380991112233';
GO

IF NOT EXISTS (
    SELECT 1 FROM dbo.CourierZones cz
    JOIN dbo.Couriers c ON c.CourierId = cz.CourierId
    JOIN dbo.DeliveryZones z ON z.ZoneId = cz.ZoneId
    WHERE c.Phone = N'+380991112233' AND z.Name = N'Центр'
)
    INSERT dbo.CourierZones (CourierId, ZoneId)
    SELECT c.CourierId, z.ZoneId
    FROM dbo.Couriers c, dbo.DeliveryZones z
    WHERE c.Phone = N'+380991112233' AND z.Name = N'Центр';
GO
