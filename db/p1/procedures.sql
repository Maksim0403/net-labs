USE ShippingDb;
GO

-- Табличний тип для передачі списку послуг в усп_CreateShipment
IF TYPE_ID('dbo.ServiceList') IS NULL
    CREATE TYPE dbo.ServiceList AS TABLE (
        ServiceTypeId BIGINT NOT NULL,
        Quantity      INT    NOT NULL
    );
GO

-- =======================================================================
-- 1. usp_CreateShipment — створення відправлення (шапка 1:1 + позиції M:N)
-- =======================================================================
CREATE OR ALTER PROCEDURE dbo.usp_CreateShipment
    @CustomerId        BIGINT,
    @SenderAddress     NVARCHAR(300),
    @RecipientName     NVARCHAR(150),
    @RecipientPhone    NVARCHAR(20),
    @RecipientAddress  NVARCHAR(300),
    @WeightKg          DECIMAL(8,3),
    @DeclaredValue     DECIMAL(12,2) = 0,
    @Services          dbo.ServiceList READONLY,
    @CreatedBy         NVARCHAR(100) = 'system',
    @ShipmentId        BIGINT OUTPUT,
    @TrackingNumber    CHAR(12) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerId = @CustomerId AND IsDeleted = 0)
            THROW 50001, 'Клієнта не знайдено', 1;

        IF NOT EXISTS (SELECT 1 FROM @Services)
            THROW 50002, 'Відправлення повинно містити хоча б одну послугу', 1;

        SET @TrackingNumber = UPPER(LEFT(CONVERT(VARCHAR(36), NEWID()), 12));

        INSERT dbo.Shipments (CustomerId, TrackingNumber, Status, CreatedBy)
        VALUES (@CustomerId, @TrackingNumber, 0, @CreatedBy);
        SET @ShipmentId = SCOPE_IDENTITY();

        INSERT dbo.ShipmentDetails (ShipmentId, SenderAddress, RecipientName, RecipientPhone, RecipientAddress, WeightKg, DeclaredValue)
        VALUES (@ShipmentId, @SenderAddress, @RecipientName, @RecipientPhone, @RecipientAddress, @WeightKg, @DeclaredValue);

        INSERT dbo.ShipmentServices (ShipmentId, ServiceTypeId, Quantity, UnitPrice, ServiceName)
        SELECT @ShipmentId, s.ServiceTypeId, v.Quantity, s.Price, s.Name
        FROM @Services v
        JOIN dbo.ServiceTypes s ON s.ServiceTypeId = v.ServiceTypeId AND s.IsActive = 1 AND s.IsDeleted = 0;

        IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @Services)
            THROW 50003, 'Одна або декілька послуг недоступні', 1;

        UPDATE dbo.Shipments
           SET TotalPrice = (SELECT SUM(Quantity * UnitPrice) FROM dbo.ShipmentServices WHERE ShipmentId = @ShipmentId)
         WHERE ShipmentId = @ShipmentId;

        INSERT dbo.ShipmentStatusHistory (ShipmentId, OldStatus, NewStatus, ChangedBy)
        VALUES (@ShipmentId, NULL, 0, @CreatedBy);

    COMMIT TRANSACTION;
END
GO

-- =======================================================================
-- 2. usp_AddServiceToShipment — додавання послуги (CRUD + M:N, лише Draft)
-- =======================================================================
CREATE OR ALTER PROCEDURE dbo.usp_AddServiceToShipment
    @ShipmentId BIGINT, @ServiceTypeId BIGINT, @Quantity INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;
        IF NOT EXISTS (SELECT 1 FROM dbo.Shipments WITH (UPDLOCK) WHERE ShipmentId = @ShipmentId AND Status = 0)
            THROW 50004, 'Змінювати склад послуг можна лише для Draft-відправлення', 1;

        MERGE dbo.ShipmentServices AS t
        USING (SELECT ServiceTypeId, Price, Name FROM dbo.ServiceTypes
                WHERE ServiceTypeId = @ServiceTypeId AND IsActive = 1 AND IsDeleted = 0) AS s
           ON t.ShipmentId = @ShipmentId AND t.ServiceTypeId = s.ServiceTypeId
        WHEN MATCHED THEN UPDATE SET Quantity = t.Quantity + @Quantity
        WHEN NOT MATCHED THEN INSERT (ShipmentId, ServiceTypeId, Quantity, UnitPrice, ServiceName)
             VALUES (@ShipmentId, s.ServiceTypeId, @Quantity, s.Price, s.Name);

        IF @@ROWCOUNT = 0 THROW 50005, 'Послугу не знайдено або вона неактивна', 1;

        UPDATE dbo.Shipments
           SET TotalPrice = (SELECT SUM(Quantity * UnitPrice) FROM dbo.ShipmentServices WHERE ShipmentId = @ShipmentId),
               UpdatedAt = SYSUTCDATETIME()
         WHERE ShipmentId = @ShipmentId;
    COMMIT TRANSACTION;
END
GO

-- =======================================================================
-- 3. usp_ConfirmShipment — бізнес-операція підтвердження (Draft -> Confirmed)
-- =======================================================================
CREATE OR ALTER PROCEDURE dbo.usp_ConfirmShipment
    @ShipmentId BIGINT, @ConfirmedBy NVARCHAR(100) = 'system'
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;
        DECLARE @Status TINYINT;
        SELECT @Status = Status FROM dbo.Shipments WITH (UPDLOCK, HOLDLOCK)
         WHERE ShipmentId = @ShipmentId AND IsDeleted = 0;

        IF @Status IS NULL THROW 50006, 'Відправлення не знайдено', 1;
        IF @Status <> 0 THROW 50007, 'Підтвердити можна лише Draft-відправлення', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.ShipmentServices WHERE ShipmentId = @ShipmentId)
            THROW 50008, 'У відправленні немає жодної послуги', 1;

        UPDATE dbo.Shipments SET Status = 1, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @ConfirmedBy
         WHERE ShipmentId = @ShipmentId;

        INSERT dbo.ShipmentStatusHistory (ShipmentId, OldStatus, NewStatus, ChangedBy)
        VALUES (@ShipmentId, 0, 1, @ConfirmedBy);
    COMMIT TRANSACTION;
END
GO

-- =======================================================================
-- 4. usp_ChangeShipmentStatus — транзакційна зміна статусу з перевіркою
--    допустимого переходу (ключова процедура для DAL у Лаб. 02)
-- =======================================================================
CREATE OR ALTER PROCEDURE dbo.usp_ChangeShipmentStatus
    @ShipmentId BIGINT,
    @NewStatus  TINYINT,
    @ChangedBy  NVARCHAR(100) = 'system'
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;
        DECLARE @Old TINYINT;
        SELECT @Old = Status FROM dbo.Shipments WITH (UPDLOCK) WHERE ShipmentId = @ShipmentId;

        IF @Old IS NULL THROW 50006, 'Відправлення не знайдено', 1;

        -- допустимі переходи: Confirmed(1)->InTransit(2)->Delivered(3)
        IF NOT ( (@Old = 1 AND @NewStatus = 2) OR (@Old = 2 AND @NewStatus = 3) )
            THROW 50012, 'Недопустимий перехід статусу', 1;

        UPDATE dbo.Shipments SET Status = @NewStatus, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @ChangedBy
         WHERE ShipmentId = @ShipmentId;

        INSERT dbo.ShipmentStatusHistory (ShipmentId, OldStatus, NewStatus, ChangedBy)
        VALUES (@ShipmentId, @Old, @NewStatus, @ChangedBy);
    COMMIT TRANSACTION;
END
GO

-- =======================================================================
-- 5. usp_CancelShipment — скасування (лише до відправлення в дорогу)
-- =======================================================================
CREATE OR ALTER PROCEDURE dbo.usp_CancelShipment
    @ShipmentId BIGINT, @ChangedBy NVARCHAR(100) = 'system'
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;
        DECLARE @Old TINYINT;
        SELECT @Old = Status FROM dbo.Shipments WITH (UPDLOCK) WHERE ShipmentId = @ShipmentId;

        IF @Old IS NULL THROW 50006, 'Відправлення не знайдено', 1;
        IF @Old NOT IN (0, 1) THROW 50009, 'Скасування можливе лише до відправлення в дорогу', 1;

        UPDATE dbo.Shipments SET Status = 4, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @ChangedBy
         WHERE ShipmentId = @ShipmentId;

        INSERT dbo.ShipmentStatusHistory (ShipmentId, OldStatus, NewStatus, ChangedBy)
        VALUES (@ShipmentId, @Old, 4, @ChangedBy);
    COMMIT TRANSACTION;
END
GO

-- =======================================================================
-- 6a. usp_DeleteCustomer — CRUD: Delete (м'яке видалення клієнта)
-- =======================================================================
CREATE OR ALTER PROCEDURE dbo.usp_DeleteCustomer
    @CustomerId BIGINT, @DeletedBy NVARCHAR(100) = 'system'
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;
        IF EXISTS (SELECT 1 FROM dbo.Shipments WHERE CustomerId = @CustomerId AND Status NOT IN (3, 4) AND IsDeleted = 0)
            THROW 50010, 'Не можна видалити клієнта з активними відправленнями', 1;

        UPDATE dbo.Customers
           SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @DeletedBy
         WHERE CustomerId = @CustomerId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50011, 'Клієнта не знайдено або вже видалено', 1;
    COMMIT TRANSACTION;
END
GO

-- =======================================================================
-- 6. usp_GetCustomerShipments — пагінований пошук (CRUD: Read)
-- =======================================================================
CREATE OR ALTER PROCEDURE dbo.usp_GetCustomerShipments
    @CustomerId BIGINT,
    @Status     TINYINT = NULL,
    @Page       INT = 1,
    @PageSize   INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.ShipmentId, s.TrackingNumber, s.Status, s.TotalPrice, s.CreatedAt,
           d.RecipientName, d.RecipientAddress
      FROM dbo.Shipments s
      JOIN dbo.ShipmentDetails d ON d.ShipmentId = s.ShipmentId
     WHERE s.CustomerId = @CustomerId AND s.IsDeleted = 0
       AND (@Status IS NULL OR s.Status = @Status)
     ORDER BY s.CreatedAt DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO
