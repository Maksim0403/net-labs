USE ShippingDb;
GO

-- Customers (ідемпотентно: перевірка за унікальним телефоном)
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Phone = N'+380501112233')
    INSERT dbo.Customers (FullName, Phone, Email) VALUES (N'Іван Петренко', N'+380501112233', N'ivan@example.com');

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Phone = N'+380671234567')
    INSERT dbo.Customers (FullName, Phone, Email) VALUES (N'Олена Коваль', N'+380671234567', N'olena@example.com');
GO

-- ServiceTypes (ідемпотентно: перевірка за унікальним кодом)
IF NOT EXISTS (SELECT 1 FROM dbo.ServiceTypes WHERE Code = N'STD')
    INSERT dbo.ServiceTypes (Code, Name, Price) VALUES (N'STD', N'Стандартна доставка', 60.00);

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceTypes WHERE Code = N'EXPRESS')
    INSERT dbo.ServiceTypes (Code, Name, Price) VALUES (N'EXPRESS', N'Експрес-доставка', 150.00);

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceTypes WHERE Code = N'INSURANCE')
    INSERT dbo.ServiceTypes (Code, Name, Price) VALUES (N'INSURANCE', N'Страхування вантажу', 40.00);

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceTypes WHERE Code = N'FRAGILE')
    INSERT dbo.ServiceTypes (Code, Name, Price) VALUES (N'FRAGILE', N'Обережна упаковка (крихке)', 35.00);
GO

-- Приклад відправлення через процедуру (повторний запуск безпечний:
-- TrackingNumber генерується випадково, але даних-дублікатів клієнтів/послуг не буде)
IF NOT EXISTS (SELECT 1 FROM dbo.Shipments)
BEGIN
    DECLARE @svc dbo.ServiceList;
    INSERT @svc (ServiceTypeId, Quantity)
    SELECT ServiceTypeId, 1 FROM dbo.ServiceTypes WHERE Code IN (N'STD', N'INSURANCE');

    DECLARE @id BIGINT, @trk CHAR(12);
    EXEC dbo.usp_CreateShipment
         @CustomerId = 1,
         @SenderAddress = N'Чернівці, вул. Руська, 100',
         @RecipientName = N'Коваль Ірина',
         @RecipientPhone = N'+380931234567',
         @RecipientAddress = N'Київ, вул. Хрещатик, 5',
         @WeightKg = 2.5,
         @DeclaredValue = 1000,
         @Services = @svc,
         @CreatedBy = N'seed',
         @ShipmentId = @id OUTPUT,
         @TrackingNumber = @trk OUTPUT;

    PRINT CONCAT('Seed: створено відправлення #', @id, ' (', @trk, ')');
END
GO
