using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Middleware;
using CampRegistrationApp.Services;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
var skipDb = builder.Configuration.GetValue<bool>("Database:SkipRegistration")
    || Environment.GetEnvironmentVariable("DATABASE_SKIP") == "true";
if (!skipDb && !string.IsNullOrEmpty(connStr))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connStr));
}

builder.Services.AddScoped<IRecordIdGenerator, RecordIdGenerator>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INominationService, NominationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDummyDataService, DummyDataService>();
builder.Services.AddScoped<IAssistanceService, AssistanceService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IComplaintIdGenerator, ComplaintIdGenerator>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Ensure Database is created for development
if (!builder.Configuration.GetValue<bool>("Database:SkipRegistration")
    && Environment.GetEnvironmentVariable("DATABASE_SKIP") != "true")
{
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    // Ensure new tables exist (since EnsureCreated won't add to existing DB)
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sectors')
        CREATE TABLE [Sectors] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [Name] nvarchar(256) NOT NULL,
            [Camp] nvarchar(256) NULL,
            [Coordinate] nvarchar(256) NULL,
            [Area] nvarchar(256) NULL,
            [ManufacturedTentsCount] int NOT NULL DEFAULT 0,
            [HandmadeTentsCount] int NOT NULL DEFAULT 0,
            [BathroomsCount] int NOT NULL DEFAULT 0
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Admins')
        CREATE TABLE [Admins] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [Name] nvarchar(256) NOT NULL,
            [NationalId] nvarchar(256) NOT NULL,
            [Mobile] nvarchar(256) NOT NULL,
            [PasswordHash] nvarchar(max) NOT NULL,
            [Role] int NOT NULL DEFAULT 1,
            [SectorId] int NULL,
            CONSTRAINT [FK_Admins_Sectors_SectorId] FOREIGN KEY ([SectorId]) REFERENCES [Sectors]([Id])
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Admins_NationalId')
        CREATE UNIQUE INDEX [IX_Admins_NationalId] ON [Admins] ([NationalId])");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sectors_Name')
        CREATE UNIQUE INDEX [IX_Sectors_Name] ON [Sectors] ([Name])");

    // Add columns for existing databases (EnsureCreated won't alter existing tables)
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Persons') AND name = 'IsPrisoner')
        ALTER TABLE [Persons] ADD [IsPrisoner] bit NOT NULL DEFAULT 0");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Persons') AND name = 'Nationality')
        ALTER TABLE [Persons] ADD [Nationality] nvarchar(256) NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Persons') AND name = 'MotherIdNumber')
        ALTER TABLE [Persons] ADD [MotherIdNumber] nvarchar(50) NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Admins') AND name = 'IsActive')
        ALTER TABLE [Admins] ADD [IsActive] bit NOT NULL DEFAULT 1");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AuditLogs') AND name = 'Role')
        ALTER TABLE [AuditLogs] ADD [Role] nvarchar(50) NOT NULL DEFAULT ''");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AuditLogs') AND name = 'SectorId')
        ALTER TABLE [AuditLogs] ADD [SectorId] int NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AuditLogs') AND name = 'IPAddress')
        ALTER TABLE [AuditLogs] ADD [IPAddress] nvarchar(50) NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AuditLogs') AND name = 'Source')
        ALTER TABLE [AuditLogs] ADD [Source] nvarchar(20) NOT NULL DEFAULT 'Web'");

    // Add columns for existing FamilyRegistrations table
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'NeedsDiapers')
        ALTER TABLE [FamilyRegistrations] ADD [NeedsDiapers] bit NOT NULL DEFAULT 0");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'DiaperDetails')
        ALTER TABLE [FamilyRegistrations] ADD [DiaperDetails] nvarchar(max) NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'HasMultipleFamiliesInTent')
        ALTER TABLE [FamilyRegistrations] ADD [HasMultipleFamiliesInTent] bit NOT NULL DEFAULT 0");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'AdditionalFamiliesCount')
        ALTER TABLE [FamilyRegistrations] ADD [AdditionalFamiliesCount] int NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'IsHusbandAbroad')
        ALTER TABLE [FamilyRegistrations] ADD [IsHusbandAbroad] bit NOT NULL DEFAULT 0");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'SectorId')
            EXEC('ALTER TABLE [FamilyRegistrations] ADD [SectorId] int NULL')");
    db.Database.ExecuteSqlRaw(@"
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'SectorId')
        AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'Sector')
        BEGIN
            DECLARE @sql nvarchar(max) = N'
                UPDATE [FamilyRegistrations] SET [SectorId] = COALESCE((SELECT [Id] FROM [Sectors] WHERE [Name] = f.[Sector]), (SELECT TOP 1 [Id] FROM [Sectors]))
                FROM [FamilyRegistrations] f';
            EXEC sp_executesql @sql;
            ALTER TABLE [FamilyRegistrations] ALTER COLUMN [SectorId] int NOT NULL;
        END");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_FamilyRegistrations_Sectors_SectorId')
            ALTER TABLE [FamilyRegistrations] ADD CONSTRAINT [FK_FamilyRegistrations_Sectors_SectorId] FOREIGN KEY ([SectorId]) REFERENCES [Sectors]([Id]) ON DELETE CASCADE");
    db.Database.ExecuteSqlRaw(@"
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'Sector')
            ALTER TABLE [FamilyRegistrations] DROP COLUMN [Sector]");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'PhoneNumber')
        ALTER TABLE [FamilyRegistrations] ADD [PhoneNumber] nvarchar(max) NOT NULL DEFAULT ''");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'Wallet')
        ALTER TABLE [FamilyRegistrations] ADD [Wallet] nvarchar(max) NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'WalletType')
        ALTER TABLE [FamilyRegistrations] ADD [WalletType] nvarchar(max) NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'IsDeleted')
        ALTER TABLE [FamilyRegistrations] ADD [IsDeleted] bit NOT NULL DEFAULT 0");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'DeletedById')
        ALTER TABLE [FamilyRegistrations] ADD [DeletedById] int NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'DeletedAt')
        ALTER TABLE [FamilyRegistrations] ADD [DeletedAt] datetime2 NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_FamilyRegistrations_Admins_DeletedById')
        ALTER TABLE [FamilyRegistrations] ADD CONSTRAINT [FK_FamilyRegistrations_Admins_DeletedById]
        FOREIGN KEY ([DeletedById]) REFERENCES [Admins]([Id])");

    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProjectSectorQuotas')
        CREATE TABLE [ProjectSectorQuotas] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [ProjectId] int NOT NULL,
            [SectorId] int NOT NULL,
            [MaxCount] int NOT NULL DEFAULT 0,
            CONSTRAINT [FK_PSQ_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
            CONSTRAINT [FK_PSQ_Sectors_SectorId] FOREIGN KEY ([SectorId]) REFERENCES [Sectors]([Id]) ON DELETE CASCADE
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProjectSectorQuotas_ProjectId_SectorId')
        CREATE UNIQUE INDEX [IX_ProjectSectorQuotas_ProjectId_SectorId] ON [ProjectSectorQuotas] ([ProjectId], [SectorId])");

    // Create Desires and FamilyDesires tables for existing databases
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Desires')
        CREATE TABLE [Desires] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [Name] nvarchar(200) NOT NULL
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FamilyDesires')
        CREATE TABLE [FamilyDesires] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [FamilyRegistrationId] int NOT NULL,
            [DesireId] int NOT NULL,
            [Order] int NOT NULL,
            CONSTRAINT [FK_FD_FamilyRegistrations_FamilyRegistrationId] FOREIGN KEY ([FamilyRegistrationId]) REFERENCES [FamilyRegistrations]([Id]) ON DELETE CASCADE,
            CONSTRAINT [FK_FD_Desires_DesireId] FOREIGN KEY ([DesireId]) REFERENCES [Desires]([Id])
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FamilyDesires_FamilyRegistrationId_DesireId')
        CREATE UNIQUE INDEX [IX_FamilyDesires_FamilyRegistrationId_DesireId] ON [FamilyDesires] ([FamilyRegistrationId], [DesireId])");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Desires_Name')
        CREATE UNIQUE INDEX [IX_Desires_Name] ON [Desires] ([Name])");

    // Seed default sectors if none exist
    if (!db.Sectors.Any())
    {
        db.Sectors.AddRange(
            new Sector { Name = "A", Camp = "", Coordinate = "", Area = "", ManufacturedTentsCount = 0, HandmadeTentsCount = 0, BathroomsCount = 0 },
            new Sector { Name = "B", Camp = "", Coordinate = "", Area = "", ManufacturedTentsCount = 0, HandmadeTentsCount = 0, BathroomsCount = 0 },
            new Sector { Name = "C", Camp = "", Coordinate = "", Area = "", ManufacturedTentsCount = 0, HandmadeTentsCount = 0, BathroomsCount = 0 },
            new Sector { Name = "D", Camp = "", Coordinate = "", Area = "", ManufacturedTentsCount = 0, HandmadeTentsCount = 0, BathroomsCount = 0 }
        );
        db.SaveChanges();
    }

    // Add approval columns for existing databases
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'ApprovalStatus')
        ALTER TABLE [FamilyRegistrations] ADD [ApprovalStatus] int NOT NULL DEFAULT 0");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'ApprovedById')
        ALTER TABLE [FamilyRegistrations] ADD [ApprovedById] int NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FamilyRegistrations') AND name = 'ApprovedAt')
        ALTER TABLE [FamilyRegistrations] ADD [ApprovedAt] datetime2 NULL");

    // Create new lookup tables for existing databases
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HealthStatuses')
        CREATE TABLE [HealthStatuses] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [Name] nvarchar(100) NOT NULL
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChronicDiseases')
        CREATE TABLE [ChronicDiseases] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [Name] nvarchar(100) NOT NULL
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DisabilityTypes')
        CREATE TABLE [DisabilityTypes] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [Name] nvarchar(100) NOT NULL
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HealthStatuses_Name')
        CREATE UNIQUE INDEX [IX_HealthStatuses_Name] ON [HealthStatuses] ([Name])");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChronicDiseases_Name')
        CREATE UNIQUE INDEX [IX_ChronicDiseases_Name] ON [ChronicDiseases] ([Name])");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DisabilityTypes_Name')
        CREATE UNIQUE INDEX [IX_DisabilityTypes_Name] ON [DisabilityTypes] ([Name])");

    // Seed lookup tables if empty
    if (!db.HealthStatuses.Any())
    {
        db.HealthStatuses.AddRange(
            new HealthStatus { Name = "سليم" },
            new HealthStatus { Name = "مريض" }
        );
        db.SaveChanges();
    }

    if (!db.ChronicDiseases.Any())
    {
        db.ChronicDiseases.AddRange(
            new ChronicDisease { Name = "السكري" },
            new ChronicDisease { Name = "ضغط الدم المرتفع" },
            new ChronicDisease { Name = "أمراض القلب" },
            new ChronicDisease { Name = "الربو" },
            new ChronicDisease { Name = "السرطان" },
            new ChronicDisease { Name = "أمراض الكلى المزمنة" },
            new ChronicDisease { Name = "التصلب اللويحي" },
            new ChronicDisease { Name = "الصرع" },
            new ChronicDisease { Name = "ثلاسيميا" },
            new ChronicDisease { Name = "أخرى" }
        );
        db.SaveChanges();
    }

    if (!db.DisabilityTypes.Any())
    {
        db.DisabilityTypes.AddRange(
            new DisabilityType { Name = "شلل نصفي أو كلي" },
            new DisabilityType { Name = "بتر الأطراف" },
            new DisabilityType { Name = "ضعف أو فقدان البصر" },
            new DisabilityType { Name = "ضعف أو فقدان السمع" },
            new DisabilityType { Name = "التوحد" },
            new DisabilityType { Name = "متلازمة داون" },
            new DisabilityType { Name = "إعاقة ذهنية" }
        );
        db.SaveChanges();
    }

    // Seed Desires
    var essentialDesires = new[] { "خيم", "اغطية", "فرشات", "ادوات مطبخ", "شوادر", "ملابس", "طرد صحي", "طرد غذائي" };
    foreach (var name in essentialDesires)
    {
        if (!db.Desires.Any(d => d.Name == name))
        {
            db.Desires.Add(new Desire { Name = name });
        }
    }
    db.SaveChanges();

    // Create Projects, Nominations, AuditLogs tables for existing databases
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Projects')
        CREATE TABLE [Projects] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [Name] nvarchar(200) NOT NULL,
            [StartDate] datetime2 NOT NULL,
            [EndDate] datetime2 NOT NULL,
            [RequiredCount] int NOT NULL DEFAULT 0,
            [Status] int NOT NULL DEFAULT 0,
            [Notes] nvarchar(max) NULL,
            [CreatedById] int NOT NULL,
            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
            [IsDeleted] bit NOT NULL DEFAULT 0,
            [RowVersion] rowversion NOT NULL,
            CONSTRAINT [FK_Projects_Admins_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Admins]([Id])
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Nominations')
        CREATE TABLE [Nominations] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [ProjectId] int NOT NULL,
            [PersonId] int NOT NULL,
            [SectorId] int NOT NULL,
            [DelegateId] int NOT NULL,
            [Status] int NOT NULL DEFAULT 0,
            [Notes] nvarchar(max) NULL,
            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
            [UpdatedAt] datetime2 NULL,
            [ApprovedById] int NULL,
            [ApprovedAt] datetime2 NULL,
            [IsDeleted] bit NOT NULL DEFAULT 0,
            [RowVersion] rowversion NOT NULL,
            CONSTRAINT [FK_Nominations_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]),
            CONSTRAINT [FK_Nominations_Persons_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Persons]([Id]),
            CONSTRAINT [FK_Nominations_Sectors_SectorId] FOREIGN KEY ([SectorId]) REFERENCES [Sectors]([Id]),
            CONSTRAINT [FK_Nominations_Admins_DelegateId] FOREIGN KEY ([DelegateId]) REFERENCES [Admins]([Id]),
            CONSTRAINT [FK_Nominations_Admins_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Admins]([Id])
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
        CREATE TABLE [AuditLogs] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [UserId] int NOT NULL,
            [Action] nvarchar(256) NOT NULL,
            [TableName] nvarchar(256) NOT NULL,
            [RecordId] nvarchar(256) NULL,
            [OldValues] nvarchar(max) NULL,
            [NewValues] nvarchar(max) NULL,
            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE()
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogs_CreatedAt')
        CREATE INDEX [IX_AuditLogs_CreatedAt] ON [AuditLogs] ([CreatedAt])");

    // Create new aid-management tables for existing databases
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Assistances')
        CREATE TABLE [Assistances] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [Name] nvarchar(200) NOT NULL,
            [AssistanceType] nvarchar(200) NOT NULL DEFAULT '',
            [Source] nvarchar(200) NOT NULL DEFAULT '',
            [AssistanceDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
            [Description] nvarchar(max) NULL,
            [SectorId] int NOT NULL,
            [Status] int NOT NULL DEFAULT 0,
            [CreatedById] int NOT NULL,
            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
            [ApprovedById] int NULL,
            [ApprovedAt] datetime2 NULL,
            [AttachmentsPath] nvarchar(max) NULL,
            [IsDeleted] bit NOT NULL DEFAULT 0,
            CONSTRAINT [FK_Assistances_Sectors_SectorId] FOREIGN KEY ([SectorId]) REFERENCES [Sectors]([Id]),
            CONSTRAINT [FK_Assistances_Admins_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Admins]([Id]),
            CONSTRAINT [FK_Assistances_Admins_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Admins]([Id])
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AssistanceBeneficiaries')
        CREATE TABLE [AssistanceBeneficiaries] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [AssistanceId] int NOT NULL,
            [FullName] nvarchar(200) NOT NULL,
            [NationalId] nvarchar(50) NOT NULL,
            [Phone] nvarchar(50) NOT NULL DEFAULT '',
            [FileNumber] nvarchar(100) NOT NULL DEFAULT '',
            [FamilyName] nvarchar(200) NOT NULL DEFAULT '',
            [City] nvarchar(200) NOT NULL DEFAULT '',
            [SectorId] int NOT NULL,
            [FamilyCount] int NOT NULL DEFAULT 0,
            [BenefitType] nvarchar(200) NOT NULL DEFAULT '',
            [Status] int NOT NULL DEFAULT 0,
            [Notes] nvarchar(max) NULL,
            [CreatedById] int NOT NULL,
            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
            [UpdatedAt] datetime2 NULL,
            [ImportId] int NULL,
            [IsDeleted] bit NOT NULL DEFAULT 0,
            CONSTRAINT [FK_AB_Assistances_AssistanceId] FOREIGN KEY ([AssistanceId]) REFERENCES [Assistances]([Id]),
            CONSTRAINT [FK_AB_Sectors_SectorId] FOREIGN KEY ([SectorId]) REFERENCES [Sectors]([Id]),
            CONSTRAINT [FK_AB_Admins_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Admins]([Id])
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AB_NationalId_AssistanceId')
        CREATE UNIQUE INDEX [IX_AB_NationalId_AssistanceId] ON [AssistanceBeneficiaries] ([NationalId], [AssistanceId]) WHERE [IsDeleted] = 0");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AssistanceImports')
        CREATE TABLE [AssistanceImports] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [FileName] nvarchar(500) NOT NULL,
            [ImportedById] int NOT NULL,
            [SectorId] int NOT NULL,
            [ImportedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
            [TotalRows] int NOT NULL DEFAULT 0,
            [SuccessRows] int NOT NULL DEFAULT 0,
            [FailedRows] int NOT NULL DEFAULT 0,
            [DuplicateRows] int NOT NULL DEFAULT 0,
            [ErrorFilePath] nvarchar(max) NULL,
            CONSTRAINT [FK_AI_Admins_ImportedById] FOREIGN KEY ([ImportedById]) REFERENCES [Admins]([Id]),
            CONSTRAINT [FK_AI_Sectors_SectorId] FOREIGN KEY ([SectorId]) REFERENCES [Sectors]([Id])
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AssistanceBeneficiaries') AND name = 'ImportId')
        ALTER TABLE [AssistanceBeneficiaries] ADD [ImportId] int NULL");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AB_ImportId')
        CREATE INDEX [IX_AB_ImportId] ON [AssistanceBeneficiaries] ([ImportId])");

    // Create Notifications table for existing databases
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
        CREATE TABLE [Notifications] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [AdminId] int NOT NULL,
            [Message] nvarchar(500) NOT NULL,
            [Link] nvarchar(500) NULL,
            [IsRead] bit NOT NULL DEFAULT 0,
            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
            CONSTRAINT [FK_Notifications_Admins_AdminId] FOREIGN KEY ([AdminId]) REFERENCES [Admins]([Id]) ON DELETE CASCADE
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_AdminId_IsRead')
        CREATE INDEX [IX_Notifications_AdminId_IsRead] ON [Notifications] ([AdminId], [IsRead])");

    // Create Complaints table for existing databases
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Complaints')
        CREATE TABLE [Complaints] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [TicketId] nvarchar(8) NOT NULL,
            [Subject] nvarchar(200) NOT NULL,
            [Message] nvarchar(max) NOT NULL,
            [SenderName] nvarchar(100) NULL,
            [SenderPhone] nvarchar(20) NULL,
            [Status] int NOT NULL DEFAULT 0,
            [AdminResponse] nvarchar(max) NULL,
            [ResolvedById] int NULL,
            [ResolvedAt] datetime2 NULL,
            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
            [IsDeleted] bit NOT NULL DEFAULT 0,
            CONSTRAINT [FK_Complaints_Admins_ResolvedById] FOREIGN KEY ([ResolvedById]) REFERENCES [Admins]([Id])
        )");
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Complaints_TicketId')
        CREATE UNIQUE INDEX [IX_Complaints_TicketId] ON [Complaints] ([TicketId])");

    // Seed default super admin if none exist
    if (!db.Admins.Any())
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("admin123")));
        db.Admins.Add(new Admin
        {
            Name = "المدير العام",
            NationalId = "admin",
            Mobile = "0000000000",
            PasswordHash = hash,
            Role = AdminRole.Admin
        });
        db.SaveChanges();
    }

    if (builder.Configuration.GetValue<bool>("DummyData:EnableDummyData"))
    {
        var dummyDataService = scope.ServiceProvider.GetRequiredService<IDummyDataService>();
        await dummyDataService.SeedDummyDataAsync();
    }
}
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseSession();
app.UseExceptionLogging();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
