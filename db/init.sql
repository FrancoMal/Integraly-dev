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
(1, 'auditoria'), (1, 'config'), (1, 'perfil');
GO

-- Seed instructor permissions
INSERT INTO RolePermissions (RoleId, MenuKey) VALUES
(2, 'dashboard'), (2, 'calendario'), (2, 'mis-reservas'), (2, 'perfil');
GO

-- Seed usuario permissions
INSERT INTO RolePermissions (RoleId, MenuKey) VALUES
(3, 'dashboard'), (3, 'reservar'), (3, 'mis-reservas'), (3, 'perfil');
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

PRINT 'Database initialized successfully';
GO
