/* =====================================================================
   Проєкт №2 — Fleet (Автопарк і логістика)
   SQL Server, схема спроєктована під EF Core Code First (реалізація — Лаб. 03)
   Зв'язки: 1:N Warehouse->Vehicle/Courier, 1:1 Courier<->CourierLicense,
            M:N Courier<->DeliveryZone через CourierZones (з полем AssignedAt),
            1:N Courier/Vehicle/DeliveryZone -> Route
   ===================================================================== */

IF DB_ID('FleetDb') IS NULL
    CREATE DATABASE FleetDb;
GO
USE FleetDb;
GO

-- ---------------------------------------------------------------------
-- Warehouses
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Warehouses') IS NULL
CREATE TABLE dbo.Warehouses (
    WarehouseId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Code        NVARCHAR(20)  NOT NULL,
    City        NVARCHAR(100) NOT NULL,
    Address     NVARCHAR(300) NOT NULL,

    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NOT NULL DEFAULT 'system',
    UpdatedAt   DATETIME2     NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT           NOT NULL DEFAULT 0,
    RowVersion  ROWVERSION,

    CONSTRAINT UQ_Warehouses_Code UNIQUE (Code)
);
GO
-- під запит "склади міста"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Warehouses_City')
    CREATE INDEX IX_Warehouses_City ON dbo.Warehouses(City);
GO

-- ---------------------------------------------------------------------
-- Vehicles  — 1:N від Warehouse
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Vehicles') IS NULL
CREATE TABLE dbo.Vehicles (
    VehicleId    BIGINT IDENTITY(1,1) PRIMARY KEY,
    WarehouseId  BIGINT        NOT NULL,
    PlateNumber  NVARCHAR(20)  NOT NULL,
    VehicleType  NVARCHAR(30)  NOT NULL DEFAULT 'Van',
    CapacityKg   DECIMAL(9,2)  NOT NULL,

    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NOT NULL DEFAULT 'system',
    UpdatedAt   DATETIME2     NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT           NOT NULL DEFAULT 0,
    RowVersion  ROWVERSION,   -- паралельне редагування даних транспорту різними диспетчерами

    CONSTRAINT FK_Vehicles_Warehouses FOREIGN KEY (WarehouseId) REFERENCES dbo.Warehouses(WarehouseId),
    CONSTRAINT UQ_Vehicles_Plate UNIQUE (PlateNumber),
    CONSTRAINT CK_Vehicles_Capacity CHECK (CapacityKg > 0)
);
GO

-- ---------------------------------------------------------------------
-- Couriers  — 1:N від Warehouse
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Couriers') IS NULL
CREATE TABLE dbo.Couriers (
    CourierId    BIGINT IDENTITY(1,1) PRIMARY KEY,
    WarehouseId  BIGINT        NOT NULL,
    FullName     NVARCHAR(150) NOT NULL,
    Phone        NVARCHAR(20)  NOT NULL,

    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NOT NULL DEFAULT 'system',
    UpdatedAt   DATETIME2     NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT           NOT NULL DEFAULT 0,
    RowVersion  ROWVERSION,   -- паралельне редагування профілю кур'єра

    CONSTRAINT FK_Couriers_Warehouses FOREIGN KEY (WarehouseId) REFERENCES dbo.Warehouses(WarehouseId),
    CONSTRAINT UQ_Couriers_Phone UNIQUE (Phone)
);
GO

-- ---------------------------------------------------------------------
-- CourierLicenses — 1:1 з Couriers (PK = FK)
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.CourierLicenses') IS NULL
CREATE TABLE dbo.CourierLicenses (
    CourierId  BIGINT PRIMARY KEY,
    Number     NVARCHAR(30) NOT NULL,
    Category   NVARCHAR(5)  NOT NULL DEFAULT 'B',
    ExpiresOn  DATE         NOT NULL,

    CONSTRAINT FK_CourierLicenses_Couriers FOREIGN KEY (CourierId)
        REFERENCES dbo.Couriers(CourierId) ON DELETE CASCADE,
    CONSTRAINT UQ_CourierLicenses_Number UNIQUE (Number)
);
GO

-- ---------------------------------------------------------------------
-- DeliveryZones
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.DeliveryZones') IS NULL
CREATE TABLE dbo.DeliveryZones (
    ZoneId  BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name    NVARCHAR(100) NOT NULL,

    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NOT NULL DEFAULT 'system',
    UpdatedAt   DATETIME2     NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT           NOT NULL DEFAULT 0,

    CONSTRAINT UQ_DeliveryZones_Name UNIQUE (Name)
);
GO

-- ---------------------------------------------------------------------
-- CourierZones — M:N (Couriers <-> DeliveryZones) з власним полем AssignedAt
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.CourierZones') IS NULL
CREATE TABLE dbo.CourierZones (
    CourierId   BIGINT   NOT NULL,
    ZoneId      BIGINT   NOT NULL,
    AssignedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),   -- власне поле проміжної сутності

    CONSTRAINT PK_CourierZones PRIMARY KEY (CourierId, ZoneId),
    CONSTRAINT FK_CZ_Couriers FOREIGN KEY (CourierId) REFERENCES dbo.Couriers(CourierId) ON DELETE CASCADE,
    CONSTRAINT FK_CZ_Zones    FOREIGN KEY (ZoneId)    REFERENCES dbo.DeliveryZones(ZoneId) ON DELETE CASCADE
);
GO

-- ---------------------------------------------------------------------
-- Routes — 1:N від Courier, Vehicle, DeliveryZone
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Routes') IS NULL
CREATE TABLE dbo.Routes (
    RouteId             BIGINT IDENTITY(1,1) PRIMARY KEY,
    CourierId           BIGINT        NOT NULL,
    VehicleId           BIGINT        NOT NULL,
    ZoneId              BIGINT        NOT NULL,
    PlannedDate         DATE          NOT NULL,
    Status              NVARCHAR(20)  NOT NULL DEFAULT 'Planned',
    PlannedDistanceKm   DECIMAL(8,2)  NOT NULL DEFAULT 0,

    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NOT NULL DEFAULT 'system',
    UpdatedAt   DATETIME2     NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT           NOT NULL DEFAULT 0,
    RowVersion  ROWVERSION,   -- статус маршруту можуть одночасно оновлювати диспетчер і кур'єр

    CONSTRAINT FK_Routes_Couriers FOREIGN KEY (CourierId) REFERENCES dbo.Couriers(CourierId),
    CONSTRAINT FK_Routes_Vehicles FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles(VehicleId),
    CONSTRAINT FK_Routes_Zones    FOREIGN KEY (ZoneId)    REFERENCES dbo.DeliveryZones(ZoneId),
    CONSTRAINT CK_Routes_Status   CHECK (Status IN ('Planned','InProgress','Completed','Cancelled')),
    CONSTRAINT CK_Routes_Distance CHECK (PlannedDistanceKm >= 0)
);
GO
-- під запит "маршрути кур'єра на дату" та "маршрути за статусом/датою"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Routes_Courier_Date')
    CREATE INDEX IX_Routes_Courier_Date ON dbo.Routes(CourierId, PlannedDate);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Routes_Status_Date')
    CREATE INDEX IX_Routes_Status_Date ON dbo.Routes(Status, PlannedDate);
GO
