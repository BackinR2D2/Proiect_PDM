-- ============================================================
--  Budget Planner - Script creare baza de date
--  Ruleaza in SSMS conectat la SQL Server local (localhost)
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'BudgetPlannerDB')
BEGIN
    CREATE DATABASE BudgetPlannerDB;
END
GO

USE BudgetPlannerDB;
GO

-- ------------------------------------------------------------
--  Setari aplicatie (un singur rand per instalare)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppSettings')
CREATE TABLE AppSettings (
    Id              INT             NOT NULL DEFAULT 1,
    FullName        NVARCHAR(200)   NOT NULL DEFAULT 'Utilizator',
    DefaultCurrency NVARCHAR(10)    NOT NULL DEFAULT 'RON',
    WeekStartsOn    NVARCHAR(20)    NOT NULL DEFAULT 'Luni',
    AutoSyncRates   BIT             NOT NULL DEFAULT 1,
    RoundUpSavings  BIT             NOT NULL DEFAULT 0,
    ReminderDay     INT             NOT NULL DEFAULT 5,
    CONSTRAINT PK_AppSettings PRIMARY KEY (Id),
    CONSTRAINT CK_AppSettings_SingleRow CHECK (Id = 1)
);
GO

-- ------------------------------------------------------------
--  Categorii de bugete
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BudgetCategories')
CREATE TABLE BudgetCategories (
    Id              INT             NOT NULL IDENTITY(1,1),
    Name            NVARCHAR(100)   NOT NULL,
    MonthlyLimit    DECIMAL(18,2)   NOT NULL DEFAULT 0,
    AlertThreshold  DECIMAL(5,4)    NOT NULL DEFAULT 0.8,
    CONSTRAINT PK_BudgetCategories PRIMARY KEY (Id),
    CONSTRAINT UQ_BudgetCategories_Name UNIQUE (Name)
);
GO

-- ------------------------------------------------------------
--  Obiective de economii
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavingsGoals')
CREATE TABLE SavingsGoals (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    Title           NVARCHAR(200)    NOT NULL,
    TargetAmount    DECIMAL(18,2)    NOT NULL DEFAULT 0,
    CurrentAmount   DECIMAL(18,2)    NOT NULL DEFAULT 0,
    Deadline        DATE             NOT NULL,
    IsPinned        BIT              NOT NULL DEFAULT 0,
    CONSTRAINT PK_SavingsGoals PRIMARY KEY (Id)
);
GO

-- ------------------------------------------------------------
--  Tranzactii
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Transactions')
CREATE TABLE Transactions (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    Title           NVARCHAR(200)    NOT NULL,
    Category        NVARCHAR(100)    NOT NULL DEFAULT '',
    Type            TINYINT          NOT NULL,   -- 0 = Cheltuiala, 1 = Venit
    Amount          DECIMAL(18,2)    NOT NULL DEFAULT 0,
    OccurredOn      DATETIME2        NOT NULL DEFAULT GETDATE(),
    Notes           NVARCHAR(1000)   NOT NULL DEFAULT '',
    IsRecurring     BIT              NOT NULL DEFAULT 0,
    CONSTRAINT PK_Transactions PRIMARY KEY (Id),
    CONSTRAINT CK_Transactions_Amount CHECK (Amount >= 0),
    CONSTRAINT CK_Transactions_Type CHECK (Type IN (0, 1))
);
GO

-- ------------------------------------------------------------
--  Date initiale - AppSettings
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE Id = 1)
    INSERT INTO AppSettings (Id, FullName, DefaultCurrency, WeekStartsOn, AutoSyncRates, RoundUpSavings, ReminderDay)
    VALUES (1, 'Utilizator', 'RON', 'Luni', 1, 0, 5);
GO

-- ------------------------------------------------------------
--  Date initiale - Categorii predefinite
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM BudgetCategories)
BEGIN
    INSERT INTO BudgetCategories (Name, MonthlyLimit, AlertThreshold) VALUES
        ('Mancare',     1300.00, 0.80),
        ('Transport',    550.00, 0.75),
        ('Utilitati',    900.00, 0.90),
        ('Sanatate',     450.00, 0.70),
        ('Timp liber',   600.00, 0.80),
        ('Educatie',     300.00, 0.80),
        ('Imbracaminte', 400.00, 0.80),
        ('Altele',       200.00, 0.80);
END
GO

PRINT 'BudgetPlannerDB creat cu succes!';
GO
