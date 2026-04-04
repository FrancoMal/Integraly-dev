USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'AIcoding')
BEGIN
    CREATE DATABASE AIcoding;
END
GO

USE AIcoding;
GO

-- Roles table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Roles' AND xtype='U')
BEGIN
    CREATE TABLE Roles (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(50) NOT NULL UNIQUE,
        Description NVARCHAR(255) NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Seed roles: admin, instructor, usuario
IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'admin')
BEGIN
    SET IDENTITY_INSERT Roles ON;
    INSERT INTO Roles (Id, Name, Description) VALUES (1, 'admin', 'Administrador con acceso total');
    SET IDENTITY_INSERT Roles OFF;
END
GO

IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'instructor')
BEGIN
    SET IDENTITY_INSERT Roles ON;
    INSERT INTO Roles (Id, Name, Description) VALUES (2, 'instructor', 'Instructor de tutorias');
    SET IDENTITY_INSERT Roles OFF;
END
GO

IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'usuario')
BEGIN
    SET IDENTITY_INSERT Roles ON;
    INSERT INTO Roles (Id, Name, Description) VALUES (3, 'usuario', 'Usuario que reserva tutorias');
    SET IDENTITY_INSERT Roles OFF;
END
GO

-- Users table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE Users (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Username NVARCHAR(100) NOT NULL UNIQUE,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        FirstName NVARCHAR(100) NULL,
        LastName NVARCHAR(100) NULL,
        Phone NVARCHAR(50) NULL,
        RoleId INT NOT NULL DEFAULT 3,
        VpsInfo NVARCHAR(MAX) NULL,
        Timezone NVARCHAR(100) DEFAULT 'America/Argentina/Buenos_Aires',
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        IsActive BIT DEFAULT 1,
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id)
    );
END
GO

-- Add VpsInfo column if table already exists but column doesn't
IF EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'VpsInfo')
BEGIN
    ALTER TABLE Users ADD VpsInfo NVARCHAR(MAX) NULL;
END
GO

-- Add Timezone column if it doesn't exist
IF EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Timezone')
BEGIN
    ALTER TABLE Users ADD Timezone NVARCHAR(100) DEFAULT 'America/Argentina/Buenos_Aires';
END
GO

-- Drop old Role string column if it exists (legacy)
IF EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
   AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Role')
BEGIN
    ALTER TABLE Users DROP COLUMN Role;
END
GO

-- Invitations table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Invitations' AND xtype='U')
BEGIN
    CREATE TABLE Invitations (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Email NVARCHAR(255) NOT NULL,
        RoleId INT NOT NULL,
        Token NVARCHAR(100) NOT NULL UNIQUE,
        CreatedBy INT NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UsedAt DATETIME2 NULL,
        ExpiresAt DATETIME2 NOT NULL,
        CONSTRAINT FK_Invitations_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id),
        CONSTRAINT FK_Invitations_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
    );
    CREATE INDEX IX_Invitations_Token ON Invitations (Token);
    CREATE INDEX IX_Invitations_Email ON Invitations (Email);
END
GO

-- TokenPacks table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TokenPacks' AND xtype='U')
BEGIN
    CREATE TABLE TokenPacks (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        TotalTokens INT NOT NULL,
        RemainingTokens INT NOT NULL,
        CreatedBy INT NOT NULL,
        Description NVARCHAR(500) NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_TokenPacks_User FOREIGN KEY (UserId) REFERENCES Users(Id),
        CONSTRAINT FK_TokenPacks_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
    );
    CREATE INDEX IX_TokenPacks_UserId ON TokenPacks (UserId);
END
GO

-- Availability table (instructor weekly schedule)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Availabilities' AND xtype='U')
BEGIN
    CREATE TABLE Availabilities (
        Id INT PRIMARY KEY IDENTITY(1,1),
        InstructorId INT NOT NULL,
        DayOfWeek INT NOT NULL,
        StartHour INT NOT NULL,
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Availabilities_Instructor FOREIGN KEY (InstructorId) REFERENCES Users(Id),
        CONSTRAINT CK_Availabilities_DayOfWeek CHECK (DayOfWeek >= 0 AND DayOfWeek <= 6),
        CONSTRAINT CK_Availabilities_StartHour CHECK (StartHour >= 0 AND StartHour <= 23)
    );
    CREATE INDEX IX_Availabilities_InstructorId ON Availabilities (InstructorId);
    CREATE UNIQUE INDEX IX_Availabilities_Unique ON Availabilities (InstructorId, DayOfWeek, StartHour);
END
GO

-- Bookings table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Bookings' AND xtype='U')
BEGIN
    CREATE TABLE Bookings (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        InstructorId INT NOT NULL,
        TokenPackId INT NOT NULL,
        ScheduledDate DATE NOT NULL,
        StartHour INT NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'confirmed',
        MeetLink NVARCHAR(500) NULL,
        UserNotes NVARCHAR(1000) NULL,
        AdminNotes NVARCHAR(1000) NULL,
        CancelledAt DATETIME2 NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Bookings_User FOREIGN KEY (UserId) REFERENCES Users(Id),
        CONSTRAINT FK_Bookings_Instructor FOREIGN KEY (InstructorId) REFERENCES Users(Id),
        CONSTRAINT FK_Bookings_TokenPack FOREIGN KEY (TokenPackId) REFERENCES TokenPacks(Id),
        CONSTRAINT CK_Bookings_Status CHECK (Status IN ('confirmed', 'cancelled', 'completed')),
        CONSTRAINT CK_Bookings_StartHour CHECK (StartHour >= 0 AND StartHour <= 23)
    );
    CREATE INDEX IX_Bookings_UserId ON Bookings (UserId);
    CREATE INDEX IX_Bookings_InstructorId ON Bookings (InstructorId);
    CREATE INDEX IX_Bookings_ScheduledDate ON Bookings (ScheduledDate);
END
GO

-- Add UserNotes and AdminNotes columns to existing Bookings tables
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Bookings') AND name = 'UserNotes')
BEGIN
    ALTER TABLE Bookings ADD UserNotes NVARCHAR(1000) NULL;
    ALTER TABLE Bookings ADD AdminNotes NVARCHAR(1000) NULL;
END
GO

-- AuditLogs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
BEGIN
    CREATE TABLE AuditLogs (
        Id INT PRIMARY KEY IDENTITY(1,1),
        EntityType NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(100) NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        Changes NVARCHAR(MAX) NULL,
        UserName NVARCHAR(100) NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE()
    );
    CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs (EntityType, EntityId);
    CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs (CreatedAt DESC);
END
GO

-- RolePermissions table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RolePermissions' AND xtype='U')
BEGIN
    CREATE TABLE RolePermissions (
        Id INT PRIMARY KEY IDENTITY(1,1),
        RoleId INT NOT NULL,
        MenuKey NVARCHAR(50) NOT NULL,
        CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_RolePermissions UNIQUE (RoleId, MenuKey)
    );
    CREATE INDEX IX_RolePermissions_RoleId ON RolePermissions (RoleId);
END
GO

-- App Settings table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppSettings' AND xtype='U')
BEGIN
    CREATE TABLE AppSettings (
        [Key] NVARCHAR(100) PRIMARY KEY,
        [Value] NVARCHAR(MAX) NOT NULL,
        UpdatedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Seed admin user (password will be set by API on startup)
IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, RoleId)
    VALUES ('admin', 'admin@integraly.dev', 'placeholder', 'Admin', 'Sistema', 1);
END
GO

-- Clear old permissions and reseed
DELETE FROM RolePermissions;
GO

-- Seed admin permissions (all menus)
INSERT INTO RolePermissions (RoleId, MenuKey) VALUES
(1, 'dashboard'), (1, 'calendario'), (1, 'reservar'), (1, 'mis-reservas'),
(1, 'usuarios'), (1, 'invitaciones'), (1, 'packs'), (1, 'todas-reservas'),
(1, 'auditoria'), (1, 'config'), (1, 'perfil'), (1, 'webinar'), (1, 'changelog');
GO

-- Seed instructor permissions
INSERT INTO RolePermissions (RoleId, MenuKey) VALUES
(2, 'dashboard'), (2, 'calendario'), (2, 'mis-reservas'), (2, 'perfil');
GO

-- Seed usuario permissions
INSERT INTO RolePermissions (RoleId, MenuKey) VALUES
(3, 'dashboard'), (3, 'reservar'), (3, 'mis-reservas'), (3, 'perfil'), (3, 'comprar');
GO

-- Seed AppSettings
IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'BrandName')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('BrandName', 'Integraly');
END
ELSE
BEGIN
    UPDATE AppSettings SET [Value] = 'Integraly', UpdatedAt = GETDATE() WHERE [Key] = 'BrandName';
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'CancellationHours')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('CancellationHours', '24');
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'PrimaryColor')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('PrimaryColor', '#10b981');
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'SidebarBgColor')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('SidebarBgColor', '#1a1a2e');
END
GO

-- Seed SMTP settings
-- Availability thresholds (for color coding in Reservar)
IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'AvailabilityGreenMin')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('AvailabilityGreenMin', '3');
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'AvailabilityYellowMin')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('AvailabilityYellowMin', '2');
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'DefaultTimezone')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('DefaultTimezone', 'America/Argentina/Buenos_Aires');
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'SmtpHost')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('SmtpHost', 'mail.integraly.com');
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'SmtpPort')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('SmtpPort', '587');
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'SmtpSenderName')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('SmtpSenderName', 'Integraly');
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'SmtpSenderEmail')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('SmtpSenderEmail', 'ventas@integraly.com');
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'SmtpUser')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('SmtpUser', 'ventas@integraly.com');
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'SmtpPassword')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('SmtpPassword', 'Ajmg1245');
END
GO

IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'SmtpUseSsl')
BEGIN
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('SmtpUseSsl', 'false');
END
GO

-- WeekAvailabilities table (per-week overrides)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WeekAvailabilities' AND xtype='U')
BEGIN
    CREATE TABLE WeekAvailabilities (
        Id INT PRIMARY KEY IDENTITY(1,1),
        InstructorId INT NOT NULL,
        Date DATE NOT NULL,
        StartHour INT NOT NULL,
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_WeekAvailabilities_Instructor FOREIGN KEY (InstructorId) REFERENCES Users(Id),
        CONSTRAINT CK_WeekAvailabilities_StartHour CHECK (StartHour >= 0 AND StartHour <= 23)
    );
    CREATE INDEX IX_WeekAvailabilities_InstructorId ON WeekAvailabilities (InstructorId);
    CREATE UNIQUE INDEX IX_WeekAvailabilities_Unique ON WeekAvailabilities (InstructorId, Date, StartHour);
END
GO

-- InstructorTasks table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='InstructorTasks' AND xtype='U')
BEGIN
    CREATE TABLE InstructorTasks (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        InstructorId INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        TaskType NVARCHAR(50) NOT NULL DEFAULT 'otra',
        TaskDate DATE NOT NULL,
        StartHour INT NOT NULL DEFAULT 8,
        EndHour INT NOT NULL DEFAULT 9,
        HoursWorked DECIMAL(5,2) NOT NULL DEFAULT 0,
        Status NVARCHAR(50) NOT NULL DEFAULT 'pendiente',
        AssignedByUserId INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CompletedAt DATETIME2 NULL,
        CONSTRAINT FK_InstructorTasks_Instructor FOREIGN KEY (InstructorId) REFERENCES Users(Id),
        CONSTRAINT FK_InstructorTasks_AssignedBy FOREIGN KEY (AssignedByUserId) REFERENCES Users(Id)
    );
    CREATE INDEX IX_InstructorTasks_InstructorId ON InstructorTasks (InstructorId);
    CREATE INDEX IX_InstructorTasks_TaskDate ON InstructorTasks (TaskDate);
END
GO

-- Add StartHour and EndHour to existing InstructorTasks tables
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('InstructorTasks') AND name = 'StartHour')
BEGIN
    ALTER TABLE InstructorTasks ADD StartHour INT NOT NULL DEFAULT 8;
    ALTER TABLE InstructorTasks ADD EndHour INT NOT NULL DEFAULT 9;
END
GO

-- WebinarDates table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WebinarDates' AND xtype='U')
BEGIN
    CREATE TABLE WebinarDates (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(200) NOT NULL DEFAULT '',
        Date DATETIME2 NOT NULL,
        MeetingLink NVARCHAR(500) NULL,
        InviteSubject NVARCHAR(500) NULL,
        InviteMessage NVARCHAR(MAX) NULL,
        SendByEmail BIT NOT NULL DEFAULT 0,
        SendByWhatsapp BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Add Name column to WebinarDates if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WebinarDates') AND name = 'Name')
BEGIN
    ALTER TABLE WebinarDates ADD Name NVARCHAR(200) NOT NULL DEFAULT '';
END
GO

-- Add invite fields to WebinarDates if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WebinarDates') AND name = 'InviteSubject')
BEGIN
    ALTER TABLE WebinarDates ADD InviteSubject NVARCHAR(500) NULL;
    ALTER TABLE WebinarDates ADD InviteMessage NVARCHAR(MAX) NULL;
    ALTER TABLE WebinarDates ADD SendByEmail BIT NOT NULL DEFAULT 0;
    ALTER TABLE WebinarDates ADD SendByWhatsapp BIT NOT NULL DEFAULT 0;
END
GO

-- WebinarContacts table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WebinarContacts' AND xtype='U')
BEGIN
    CREATE TABLE WebinarContacts (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Email NVARCHAR(255) NOT NULL,
        FullName NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(50) NULL,
        Company NVARCHAR(200) NULL,
        Tag NVARCHAR(100) NULL,
        UUID NVARCHAR(100) NOT NULL UNIQUE,
        WebinarDateId INT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_WebinarContacts_WebinarDate FOREIGN KEY (WebinarDateId) REFERENCES WebinarDates(Id)
    );
    CREATE UNIQUE INDEX IX_WebinarContacts_UUID ON WebinarContacts (UUID);
END
GO

-- Migrate WebinarContacts: FirstName+LastName -> FullName, add Company
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WebinarContacts') AND name = 'FirstName')
BEGIN
    ALTER TABLE WebinarContacts ADD FullName NVARCHAR(200) NULL;
    EXEC('UPDATE WebinarContacts SET FullName = FirstName + '' '' + LastName');
    ALTER TABLE WebinarContacts ALTER COLUMN FullName NVARCHAR(200) NOT NULL;
    ALTER TABLE WebinarContacts DROP COLUMN FirstName;
    ALTER TABLE WebinarContacts DROP COLUMN LastName;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WebinarContacts') AND name = 'Company')
BEGIN
    ALTER TABLE WebinarContacts ADD Company NVARCHAR(200) NULL;
END
GO

-- Add Tag column to WebinarContacts if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WebinarContacts') AND name = 'Tag')
BEGIN
    ALTER TABLE WebinarContacts ADD Tag NVARCHAR(100) NULL;
END
GO

-- WebinarRegistrations table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WebinarRegistrations' AND xtype='U')
BEGIN
    CREATE TABLE WebinarRegistrations (
        Id INT PRIMARY KEY IDENTITY(1,1),
        ContactId INT NOT NULL,
        WebinarDateId INT NOT NULL,
        KnowsChatGPT BIT DEFAULT 0,
        KnowsClaude BIT DEFAULT 0,
        KnowsGrok BIT DEFAULT 0,
        KnowsGemini BIT DEFAULT 0,
        KnowsCopilot BIT DEFAULT 0,
        KnowsPerplexity BIT DEFAULT 0,
        KnowsDeepSeek BIT DEFAULT 0,
        VibeCodingKnowledge NVARCHAR(50) NOT NULL DEFAULT 'no_idea',
        RegisteredAt DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_WebinarRegistrations_Contact FOREIGN KEY (ContactId) REFERENCES WebinarContacts(Id),
        CONSTRAINT FK_WebinarRegistrations_WebinarDate FOREIGN KEY (WebinarDateId) REFERENCES WebinarDates(Id)
    );
    CREATE INDEX IX_WebinarRegistrations_ContactId ON WebinarRegistrations (ContactId);
    CREATE INDEX IX_WebinarRegistrations_WebinarDateId ON WebinarRegistrations (WebinarDateId);
END
GO

-- PaymentPlans table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PaymentPlans' AND xtype='U')
BEGIN
    CREATE TABLE PaymentPlans (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500),
        Classes INT NOT NULL,
        Price DECIMAL(10,2) NOT NULL,
        Currency NVARCHAR(10) DEFAULT 'ARS',
        Active BIT DEFAULT 1,
        DisplayOrder INT DEFAULT 0,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- Payments table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Payments' AND xtype='U')
BEGIN
    CREATE TABLE Payments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
        PaymentPlanId INT NOT NULL FOREIGN KEY REFERENCES PaymentPlans(Id),
        Amount DECIMAL(10,2) NOT NULL,
        Currency NVARCHAR(10) DEFAULT 'ARS',
        Status NVARCHAR(50) DEFAULT 'pending',
        MercadoPagoOrderId NVARCHAR(200),
        MercadoPagoPaymentId NVARCHAR(200),
        TokenPackId INT NULL FOREIGN KEY REFERENCES TokenPacks(Id),
        CreatedAt DATETIME DEFAULT GETDATE(),
        ApprovedAt DATETIME NULL
    );
    CREATE INDEX IX_Payments_UserId ON Payments (UserId);
    CREATE INDEX IX_Payments_Status ON Payments (Status);
END
GO

-- Add PriceUSD column to PaymentPlans
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PaymentPlans') AND name = 'PriceUSD')
BEGIN
    ALTER TABLE PaymentPlans ADD PriceUSD DECIMAL(10,2) DEFAULT 0;
END
GO

-- Set USD prices for existing plans
UPDATE PaymentPlans SET PriceUSD = 15 WHERE Classes = 1 AND PriceUSD = 0;
UPDATE PaymentPlans SET PriceUSD = 60 WHERE Classes = 5 AND PriceUSD = 0;
UPDATE PaymentPlans SET PriceUSD = 100 WHERE Classes = 10 AND PriceUSD = 0;
GO

-- Add PayPal and provider columns to Payments
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'PaymentProvider')
BEGIN
    ALTER TABLE Payments ADD PaymentProvider NVARCHAR(20) DEFAULT 'mercadopago';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'PayPalOrderId')
BEGIN
    ALTER TABLE Payments ADD PayPalOrderId NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'PayPalCaptureId')
BEGIN
    ALTER TABLE Payments ADD PayPalCaptureId NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'TransferReceiptUrl')
BEGIN
    ALTER TABLE Payments ADD TransferReceiptUrl NVARCHAR(500) NULL;
END
GO

-- Fix NULL PaymentProvider in old records
UPDATE Payments SET PaymentProvider = 'mercadopago' WHERE PaymentProvider IS NULL;
GO

-- Seed payment plans
IF NOT EXISTS (SELECT * FROM PaymentPlans WHERE Name = 'Clase individual')
BEGIN
    INSERT INTO PaymentPlans (Name, Description, Classes, Price, Currency, Active, DisplayOrder) VALUES
    ('Clase individual', '1 clase particular', 1, 20000, 'ARS', 1, 1);
END
GO

IF NOT EXISTS (SELECT * FROM PaymentPlans WHERE Name = 'Pack 5 clases')
BEGIN
    INSERT INTO PaymentPlans (Name, Description, Classes, Price, Currency, Active, DisplayOrder) VALUES
    ('Pack 5 clases', '5 clases particulares', 5, 80000, 'ARS', 1, 2);
END
GO

IF NOT EXISTS (SELECT * FROM PaymentPlans WHERE Name = 'Pack 10 clases')
BEGIN
    INSERT INTO PaymentPlans (Name, Description, Classes, Price, Currency, Active, DisplayOrder) VALUES
    ('Pack 10 clases', '10 clases particulares', 10, 140000, 'ARS', 1, 3);
END
GO

-- Changelog tables
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DailyChangeSummaries')
BEGIN
    CREATE TABLE DailyChangeSummaries (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Date DATE NOT NULL,
        GeneralSummary NVARCHAR(MAX) NOT NULL,
        TotalCommits INT NOT NULL DEFAULT 0,
        TotalGroups INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETDATE()
    );
    CREATE UNIQUE INDEX IX_DailyChangeSummaries_Date ON DailyChangeSummaries(Date);
    PRINT 'Table DailyChangeSummaries created';
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CommitGroups')
BEGIN
    CREATE TABLE CommitGroups (
        Id INT PRIMARY KEY IDENTITY(1,1),
        DailySummaryId INT NOT NULL,
        GroupTitle NVARCHAR(200) NOT NULL,
        GroupSummary NVARCHAR(MAX) NOT NULL,
        Tags NVARCHAR(500) NOT NULL DEFAULT '',
        CommitsJson NVARCHAR(MAX) NOT NULL DEFAULT '[]',
        DisplayOrder INT NOT NULL DEFAULT 0,
        Author NVARCHAR(100) NULL,
        CONSTRAINT FK_CommitGroups_DailyChangeSummaries FOREIGN KEY (DailySummaryId)
            REFERENCES DailyChangeSummaries(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_CommitGroups_DailySummaryId ON CommitGroups(DailySummaryId);
    PRINT 'Table CommitGroups created';
END

-- DatabaseBackups table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DatabaseBackups')
BEGIN
    CREATE TABLE DatabaseBackups (
        Id INT PRIMARY KEY IDENTITY(1,1),
        FileName NVARCHAR(500) NOT NULL,
        DatabaseName NVARCHAR(100) NOT NULL DEFAULT 'AIcoding',
        SizeBytes BIGINT NOT NULL DEFAULT 0,
        Type NVARCHAR(50) NOT NULL DEFAULT 'manual',
        Status NVARCHAR(50) NOT NULL DEFAULT 'completed',
        ErrorMessage NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE()
    );
    CREATE INDEX IX_DatabaseBackups_CreatedAt ON DatabaseBackups(CreatedAt);
    PRINT 'Table DatabaseBackups created';
END
GO

-- Seed backup schedule settings
IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'BackupScheduleEnabled')
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('BackupScheduleEnabled', 'false');
IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'BackupScheduleHours')
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('BackupScheduleHours', '24');
IF NOT EXISTS (SELECT * FROM AppSettings WHERE [Key] = 'BackupRetentionDays')
    INSERT INTO AppSettings ([Key], [Value]) VALUES ('BackupRetentionDays', '30');
GO

PRINT 'Database initialized successfully';
GO
