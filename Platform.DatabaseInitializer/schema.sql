/* =====================================================================
   Проєкт №1 — Shipping (Замовлення на доставку)
   SQL Server + ADO.NET & Dapper
   Зв'язки: 1:1 Shipments<->ShipmentDetails, 1:N Customers->Shipments,
            M:N Shipments<->ServiceTypes через ShipmentServices
   ===================================================================== */

IF DB_ID('ShippingDb') IS NULL
    CREATE DATABASE ShippingDb;
GO
USE ShippingDb;
GO

-- ---------------------------------------------------------------------
-- Customers  (1:N -> Shipments)
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Customers') IS NULL
CREATE TABLE dbo.Customers (
    CustomerId  BIGINT IDENTITY(1,1) PRIMARY KEY,
    FullName    NVARCHAR(150) NOT NULL,
    Phone       NVARCHAR(20)  NOT NULL,
    Email       NVARCHAR(150) NULL,

    CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NOT NULL CONSTRAINT DF_Customers_CreatedBy DEFAULT 'system',
    UpdatedAt   DATETIME2     NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT           NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT 0,
    RowVersion  ROWVERSION,

    CONSTRAINT UQ_Customers_Phone UNIQUE (Phone),
    CONSTRAINT CK_Customers_Phone CHECK (LEN(Phone) >= 10)
);
GO
-- унікальність email лише серед заповнених значень
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Customers_Email')
    CREATE UNIQUE INDEX UX_Customers_Email ON dbo.Customers(Email) WHERE Email IS NOT NULL;
GO

-- ---------------------------------------------------------------------
-- ServiceTypes  (довідник послуг доставки; M:N зі Shipments)
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.ServiceTypes') IS NULL
CREATE TABLE dbo.ServiceTypes (
    ServiceTypeId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Code          NVARCHAR(20)  NOT NULL,
    Name          NVARCHAR(150) NOT NULL,
    Price         DECIMAL(12,2) NOT NULL,
    IsActive      BIT           NOT NULL DEFAULT 1,

    CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy     NVARCHAR(100) NOT NULL DEFAULT 'system',
    UpdatedAt     DATETIME2     NULL,
    UpdatedBy     NVARCHAR(100) NULL,
    IsDeleted     BIT           NOT NULL DEFAULT 0,
    RowVersion    ROWVERSION,   -- довідник можуть паралельно редагувати кілька адміністраторів

    CONSTRAINT UQ_ServiceTypes_Code UNIQUE (Code),
    CONSTRAINT CK_ServiceTypes_Price CHECK (Price >= 0)
);
GO

-- ---------------------------------------------------------------------
-- Shipments  (агрегат-корінь; CourierId/CourierName/CourierPhone —
-- дубльовані поля з контексту Fleet (Проєкт №2), БЕЗ FOREIGN KEY:
-- це звичайні стовпці, оновлювані подіями CourierAssigned/CourierUpdated.
-- Див. docs/dedup-policy.md)
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Shipments') IS NULL
CREATE TABLE dbo.Shipments (
    ShipmentId       BIGINT IDENTITY(1,1) PRIMARY KEY,
    CustomerId       BIGINT        NOT NULL,               -- FK у межах цієї ж БД
    TrackingNumber   CHAR(12)      NOT NULL,
    Status           TINYINT       NOT NULL CONSTRAINT DF_Shipments_Status DEFAULT 0,
                      -- 0 Draft, 1 Confirmed, 2 InTransit, 3 Delivered, 4 Cancelled
    TotalPrice       DECIMAL(12,2) NOT NULL CONSTRAINT DF_Shipments_TotalPrice DEFAULT 0,

    -- дубльовані (не FK) поля з контексту Fleet — лише ідентифікатор + снепшот імені/телефону
    AssignedCourierId    BIGINT        NULL,
    AssignedCourierName  NVARCHAR(150) NULL,
    AssignedCourierPhone NVARCHAR(20)  NULL,

    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NOT NULL DEFAULT 'system',
    UpdatedAt   DATETIME2     NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT           NOT NULL DEFAULT 0,
    RowVersion  ROWVERSION,

    CONSTRAINT FK_Shipments_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
    CONSTRAINT UQ_Shipments_TrackingNumber UNIQUE (TrackingNumber),
    CONSTRAINT CK_Shipments_Status CHECK (Status BETWEEN 0 AND 4),
    CONSTRAINT CK_Shipments_TotalPrice CHECK (TotalPrice >= 0)
);
GO
-- під запит "замовлення клієнта за статусом" (sp_GetCustomerShipments)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shipments_Customer_Status')
    CREATE INDEX IX_Shipments_Customer_Status ON dbo.Shipments(CustomerId, Status) INCLUDE (TotalPrice, CreatedAt);
GO
-- під запит "активні відправлення" (фільтрований індекс)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shipments_Active')
    CREATE INDEX IX_Shipments_Active ON dbo.Shipments(Status, CreatedAt) WHERE IsDeleted = 0;
GO

-- ---------------------------------------------------------------------
-- ShipmentDetails  — 1:1 зі Shipments (PK = FK)
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.ShipmentDetails') IS NULL
CREATE TABLE dbo.ShipmentDetails (
    ShipmentId        BIGINT PRIMARY KEY,
    SenderAddress     NVARCHAR(300) NOT NULL,
    RecipientName     NVARCHAR(150) NOT NULL,
    RecipientPhone    NVARCHAR(20)  NOT NULL,
    RecipientAddress  NVARCHAR(300) NOT NULL,
    WeightKg          DECIMAL(8,3)  NOT NULL,
    DeclaredValue     DECIMAL(12,2) NOT NULL CONSTRAINT DF_ShipmentDetails_DeclaredValue DEFAULT 0,

    CONSTRAINT FK_ShipmentDetails_Shipments FOREIGN KEY (ShipmentId)
        REFERENCES dbo.Shipments(ShipmentId) ON DELETE CASCADE,
    CONSTRAINT CK_ShipmentDetails_Weight CHECK (WeightKg > 0),
    CONSTRAINT CK_ShipmentDetails_Declared CHECK (DeclaredValue >= 0)
);
GO

-- ---------------------------------------------------------------------
-- ShipmentServices — M:N (Shipments <-> ServiceTypes), власні поля
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.ShipmentServices') IS NULL
CREATE TABLE dbo.ShipmentServices (
    ShipmentId     BIGINT        NOT NULL,
    ServiceTypeId  BIGINT        NOT NULL,
    Quantity       INT           NOT NULL CONSTRAINT DF_ShipmentServices_Qty DEFAULT 1,
    UnitPrice      DECIMAL(12,2) NOT NULL,     -- знімок ціни на момент замовлення
    ServiceName    NVARCHAR(150) NOT NULL,     -- знімок назви на момент замовлення

    CONSTRAINT PK_ShipmentServices PRIMARY KEY (ShipmentId, ServiceTypeId),
    CONSTRAINT FK_SS_Shipments FOREIGN KEY (ShipmentId) REFERENCES dbo.Shipments(ShipmentId) ON DELETE CASCADE,
    CONSTRAINT FK_SS_ServiceTypes FOREIGN KEY (ServiceTypeId) REFERENCES dbo.ServiceTypes(ServiceTypeId),
    CONSTRAINT CK_SS_Qty CHECK (Quantity > 0),
    CONSTRAINT CK_SS_Price CHECK (UnitPrice >= 0)
);
GO
-- під запит "які відправлення використовують цю послугу"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ShipmentServices_ServiceType')
    CREATE INDEX IX_ShipmentServices_ServiceType ON dbo.ShipmentServices(ServiceTypeId);
GO

-- ---------------------------------------------------------------------
-- ShipmentStatusHistory — 1:N від Shipments, аудит зміни статусів
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.ShipmentStatusHistory') IS NULL
CREATE TABLE dbo.ShipmentStatusHistory (
    HistoryId   BIGINT IDENTITY(1,1) PRIMARY KEY,
    ShipmentId  BIGINT        NOT NULL,
    OldStatus   TINYINT       NULL,
    NewStatus   TINYINT       NOT NULL,
    ChangedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    ChangedBy   NVARCHAR(100) NOT NULL DEFAULT 'system',

    CONSTRAINT FK_SSH_Shipments FOREIGN KEY (ShipmentId) REFERENCES dbo.Shipments(ShipmentId) ON DELETE CASCADE
);
GO
-- під запит "історія конкретного відправлення в хронологічному порядку"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SSH_Shipment_ChangedAt')
    CREATE INDEX IX_SSH_Shipment_ChangedAt ON dbo.ShipmentStatusHistory(ShipmentId, ChangedAt);
GO

-- ---------------------------------------------------------------------
-- Права найменших привілеїв для застосунку
-- ---------------------------------------------------------------------
IF DATABASE_PRINCIPAL_ID('shipping_app') IS NULL
BEGIN
    CREATE ROLE shipping_app;
    GRANT EXECUTE ON SCHEMA::dbo TO shipping_app;
    GRANT SELECT  ON SCHEMA::dbo TO shipping_app;
    DENY  DELETE  ON SCHEMA::dbo TO shipping_app;   -- лише soft delete через процедури
END
GO
