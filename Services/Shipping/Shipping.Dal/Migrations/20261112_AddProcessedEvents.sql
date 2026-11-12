-- Таблиця оброблених подій для ідемпотентності споживача (Лабораторна 08)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcessedEvents' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ProcessedEvents (
        EventId     UNIQUEIDENTIFIER PRIMARY KEY,
        ProcessedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

-- Локальна копія довідника товарів (read model) за політикою дублювання
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Products (
        ProductId   INT PRIMARY KEY,
        Name        NVARCHAR(200) NOT NULL,
        Price       DECIMAL(12,2) NOT NULL,
        UpdatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

