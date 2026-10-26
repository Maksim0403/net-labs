IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [DeliveryZones] (
    [ZoneId] bigint NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_DeliveryZones] PRIMARY KEY ([ZoneId])
);
GO

CREATE TABLE [Warehouses] (
    [WarehouseId] bigint NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [City] nvarchar(100) NOT NULL,
    [Address] nvarchar(300) NOT NULL,
    [RowVersion] rowversion NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([WarehouseId])
);
GO

CREATE TABLE [Couriers] (
    [CourierId] bigint NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [WarehouseId] bigint NOT NULL,
    [RowVersion] rowversion NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Couriers] PRIMARY KEY ([CourierId]),
    CONSTRAINT [FK_Couriers_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([WarehouseId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Vehicles] (
    [VehicleId] bigint NOT NULL IDENTITY,
    [PlateNumber] nvarchar(20) NOT NULL,
    [VehicleType] nvarchar(30) NOT NULL DEFAULT N'Van',
    [CapacityKg] decimal(9,2) NOT NULL,
    [WarehouseId] bigint NOT NULL,
    [RowVersion] rowversion NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Vehicles] PRIMARY KEY ([VehicleId]),
    CONSTRAINT [CK_Vehicles_Capacity] CHECK ([CapacityKg] > 0),
    CONSTRAINT [FK_Vehicles_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([WarehouseId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [CourierLicenses] (
    [CourierId] bigint NOT NULL,
    [Number] nvarchar(30) NOT NULL,
    [Category] nvarchar(5) NOT NULL DEFAULT N'B',
    [ExpiresOn] date NOT NULL,
    CONSTRAINT [PK_CourierLicenses] PRIMARY KEY ([CourierId]),
    CONSTRAINT [FK_CourierLicenses_Couriers_CourierId] FOREIGN KEY ([CourierId]) REFERENCES [Couriers] ([CourierId]) ON DELETE CASCADE
);
GO

CREATE TABLE [CourierZones] (
    [CourierId] bigint NOT NULL,
    [ZoneId] bigint NOT NULL,
    [AssignedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_CourierZones] PRIMARY KEY ([CourierId], [ZoneId]),
    CONSTRAINT [FK_CourierZones_Couriers_CourierId] FOREIGN KEY ([CourierId]) REFERENCES [Couriers] ([CourierId]) ON DELETE CASCADE,
    CONSTRAINT [FK_CourierZones_DeliveryZones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [DeliveryZones] ([ZoneId]) ON DELETE CASCADE
);
GO

CREATE TABLE [Routes] (
    [RouteId] bigint NOT NULL IDENTITY,
    [CourierId] bigint NOT NULL,
    [VehicleId] bigint NOT NULL,
    [ZoneId] bigint NOT NULL,
    [PlannedDate] date NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Planned',
    [PlannedDistanceKm] decimal(8,2) NOT NULL DEFAULT 0.0,
    [RowVersion] rowversion NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Routes] PRIMARY KEY ([RouteId]),
    CONSTRAINT [CK_Routes_Distance] CHECK ([PlannedDistanceKm] >= 0),
    CONSTRAINT [CK_Routes_Status] CHECK ([Status] IN ('Planned','InProgress','Completed','Cancelled')),
    CONSTRAINT [FK_Routes_Couriers_CourierId] FOREIGN KEY ([CourierId]) REFERENCES [Couriers] ([CourierId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Routes_DeliveryZones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [DeliveryZones] ([ZoneId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Routes_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([VehicleId]) ON DELETE NO ACTION
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ZoneId', N'CreatedAt', N'CreatedBy', N'IsDeleted', N'Name', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[DeliveryZones]'))
    SET IDENTITY_INSERT [DeliveryZones] ON;
INSERT INTO [DeliveryZones] ([ZoneId], [CreatedAt], [CreatedBy], [IsDeleted], [Name], [UpdatedAt], [UpdatedBy])
VALUES (CAST(1 AS bigint), '2024-01-01T00:00:00.0000000', N'seed', CAST(0 AS bit), N'Центр', NULL, NULL),
(CAST(2 AS bigint), '2024-01-01T00:00:00.0000000', N'seed', CAST(0 AS bit), N'Околиці', NULL, NULL),
(CAST(3 AS bigint), '2024-01-01T00:00:00.0000000', N'seed', CAST(0 AS bit), N'Міжміська', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ZoneId', N'CreatedAt', N'CreatedBy', N'IsDeleted', N'Name', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[DeliveryZones]'))
    SET IDENTITY_INSERT [DeliveryZones] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'WarehouseId', N'Address', N'City', N'Code', N'CreatedAt', N'CreatedBy', N'IsDeleted', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Warehouses]'))
    SET IDENTITY_INSERT [Warehouses] ON;
INSERT INTO [Warehouses] ([WarehouseId], [Address], [City], [Code], [CreatedAt], [CreatedBy], [IsDeleted], [UpdatedAt], [UpdatedBy])
VALUES (CAST(1 AS bigint), N'вул. Руська, 100', N'Чернівці', N'CV-01', '2024-01-01T00:00:00.0000000', N'seed', CAST(0 AS bit), NULL, NULL),
(CAST(2 AS bigint), N'пр. Перемоги, 50', N'Київ', N'KY-01', '2024-01-01T00:00:00.0000000', N'seed', CAST(0 AS bit), NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'WarehouseId', N'Address', N'City', N'Code', N'CreatedAt', N'CreatedBy', N'IsDeleted', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Warehouses]'))
    SET IDENTITY_INSERT [Warehouses] OFF;
GO

CREATE UNIQUE INDEX [IX_CourierLicenses_Number] ON [CourierLicenses] ([Number]);
GO

CREATE UNIQUE INDEX [IX_Couriers_Phone] ON [Couriers] ([Phone]);
GO

CREATE INDEX [IX_Couriers_WarehouseId] ON [Couriers] ([WarehouseId]);
GO

CREATE INDEX [IX_CourierZones_ZoneId] ON [CourierZones] ([ZoneId]);
GO

CREATE UNIQUE INDEX [IX_DeliveryZones_Name] ON [DeliveryZones] ([Name]);
GO

CREATE INDEX [IX_Routes_Courier_Date] ON [Routes] ([CourierId], [PlannedDate]);
GO

CREATE INDEX [IX_Routes_Status_Date] ON [Routes] ([Status], [PlannedDate]);
GO

CREATE INDEX [IX_Routes_VehicleId] ON [Routes] ([VehicleId]);
GO

CREATE INDEX [IX_Routes_ZoneId] ON [Routes] ([ZoneId]);
GO

CREATE UNIQUE INDEX [IX_Vehicles_PlateNumber] ON [Vehicles] ([PlateNumber]);
GO

CREATE INDEX [IX_Vehicles_WarehouseId] ON [Vehicles] ([WarehouseId]);
GO

CREATE INDEX [IX_Warehouses_City] ON [Warehouses] ([City]);
GO

CREATE UNIQUE INDEX [IX_Warehouses_Code] ON [Warehouses] ([Code]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261021203255_InitialCreate', N'8.0.11');
GO

COMMIT;
GO

