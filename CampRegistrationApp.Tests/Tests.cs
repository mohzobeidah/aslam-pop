using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using CampRegistrationApp.Controllers;
using CampRegistrationApp.Services;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CampRegistrationApp.Tests;

public class Helpers
{
    public static ApplicationDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static void SetupControllerContext(Controller controller, int? sessionInt = null, string key = "key")
    {
        var http = new DefaultHttpContext();
        http.Session = new TestSession();
        if (sessionInt.HasValue) http.Session.SetInt32(key, sessionInt.Value);
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
    }

    public static string HashPassword(string password)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

    public static void SeedLookups(ApplicationDbContext db)
    {
        if (!db.Sectors.Any())
        {
            db.Sectors.AddRange(
                new Sector { Name = "A" }, new Sector { Name = "B" },
                new Sector { Name = "C" }, new Sector { Name = "D" }
            );
        }
        if (!db.HealthStatuses.Any())
        {
            db.HealthStatuses.AddRange(
                new HealthStatus { Name = "سليم" }, new HealthStatus { Name = "مريض" }
            );
        }
        if (!db.ChronicDiseases.Any())
        {
            db.ChronicDiseases.AddRange(
                new ChronicDisease { Name = "سكري" }, new ChronicDisease { Name = "ضغط" }
            );
        }
        if (!db.DisabilityTypes.Any())
        {
            db.DisabilityTypes.AddRange(
                new DisabilityType { Name = "حركية" }, new DisabilityType { Name = "سمعية" }
            );
        }
        if (!db.Desires.Any())
        {
            db.Desires.AddRange(
                new Desire { Name = "خيم" }, new Desire { Name = "اغطية" },
                new Desire { Name = "فرشات" }, new Desire { Name = "ادوات مطبخ" },
                new Desire { Name = "شوادر" }, new Desire { Name = "ملابس" },
                new Desire { Name = "طرد صحي" }
            );
        }
        db.SaveChanges();
    }

    public static RegistrationViewModel GetValidViewModel()
    {
        return new RegistrationViewModel
        {
            Head = new PersonViewModel
            {
                FirstName = "محمد", SecondName = "أحمد", ThirdName = "علي",
                LastName = "السيد", IdNumber = "123456789",
                DateOfBirth = new DateTime(1990, 1, 1), Gender = "ذكر",
                HealthStatus = "سليم"
            },
            CurrentStep = 1,
            SectorId = 1,
            PhoneNumber = "0591234567"
        };
    }
}

// =========================================================================
// RECORD ID GENERATOR
// =========================================================================
public class RecordIdGeneratorTests
{
    [Fact]
    public async Task GenerateUniqueIdAsync_Returns8CharString()
    {
        var db = Helpers.CreateDbContext("idtest1");
        var gen = new RecordIdGenerator(db);
        var id = await gen.GenerateUniqueIdAsync();
        Assert.Equal(8, id.Length);
    }

    [Fact]
    public async Task GenerateUniqueIdAsync_UsesValidChars()
    {
        var db = Helpers.CreateDbContext("idtest2");
        var gen = new RecordIdGenerator(db);
        var id = await gen.GenerateUniqueIdAsync();
        var valid = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        Assert.All(id, c => Assert.Contains(c, valid));
    }

    [Fact]
    public async Task GenerateUniqueIdAsync_DoesNotCollide()
    {
        var db = Helpers.CreateDbContext("idtest3");
        db.FamilyRegistrations.Add(new FamilyRegistration
        {
            RecordId = "XXXXXXX1",
            FamilyHead = new Person { FirstName = "A", IdNumber = "111111111", DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم" }
        });
        db.SaveChanges();

        var gen = new RecordIdGenerator(db);
        var id = await gen.GenerateUniqueIdAsync();
        Assert.NotEqual("XXXXXXX1", id);
    }
}

// =========================================================================
// REGISTRATION CONTROLLER
// =========================================================================
public class RegistrationControllerTests
{
    // ---- Index ----
    [Fact]
    public async Task Index_ReturnsViewWithModel()
    {
        var db = Helpers.CreateDbContext("reg1");
        Helpers.SeedLookups(db);
        var idGen = new Mock<IRecordIdGenerator>();
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var notifService = new Mock<INotificationService>();
        var auditService = new Mock<IAuditService>();
        var validator = new Mock<IRegistrationValidationService>();
        var compression = new Mock<IFileCompressionService>();
        var rateLimiter = new Mock<IRateLimiterService>();
        var ctrl = new RegistrationController(db, idGen.Object, env.Object, notifService.Object, auditService.Object, validator.Object, compression.Object, rateLimiter.Object);

        var result = await ctrl.Index();
        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<RegistrationViewModel>(view.Model);
    }

    // ---- CheckId ----
    [Fact]
    public async Task CheckId_ReturnsFalse_WhenIdNotExists()
    {
        var db = Helpers.CreateDbContext("reg2");
        var idGen = new Mock<IRecordIdGenerator>();
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var notifService = new Mock<INotificationService>();
        var auditService = new Mock<IAuditService>();
        var validator = new Mock<IRegistrationValidationService>();
        var compression = new Mock<IFileCompressionService>();
        var rateLimiter = new Mock<IRateLimiterService>();
        var ctrl = new RegistrationController(db, idGen.Object, env.Object, notifService.Object, auditService.Object, validator.Object, compression.Object, rateLimiter.Object);
        Helpers.SetupControllerContext(ctrl);

        var result = ctrl.CheckId("999999999");
        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var exists = json.Value?.GetType().GetProperty("exists")?.GetValue(json.Value);
        Assert.Equal(false, exists);
    }

    [Fact]
    public async Task CheckId_ReturnsTrue_WhenIdExists()
    {
        var db = Helpers.CreateDbContext("reg3");
        Helpers.SeedLookups(db);
        var head = new Person
        {
            FirstName = "A", IdNumber = "123456789",
            DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        db.SaveChanges();

        var idGen = new Mock<IRecordIdGenerator>();
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var notifService = new Mock<INotificationService>();
        var auditService = new Mock<IAuditService>();
        var validator = new Mock<IRegistrationValidationService>();
        var compression = new Mock<IFileCompressionService>();
        var rateLimiter = new Mock<IRateLimiterService>();
        var ctrl = new RegistrationController(db, idGen.Object, env.Object, notifService.Object, auditService.Object, validator.Object, compression.Object, rateLimiter.Object);
        Helpers.SetupControllerContext(ctrl);

        var result = ctrl.CheckId("123456789");
        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var exists = json.Value?.GetType().GetProperty("exists")?.GetValue(json.Value);
        Assert.Equal(true, exists);
    }

    // ---- Submit with duplicate ID ----
    [Fact]
    public async Task Submit_ReturnsError_WhenDuplicateId()
    {
        var db = Helpers.CreateDbContext("reg4");
        Helpers.SeedLookups(db);
        db.Persons.Add(new Person
        {
            FirstName = "Existing", IdNumber = "123456789",
            DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
        });
        db.SaveChanges();

        var idGen = new Mock<IRecordIdGenerator>();
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var notifService = new Mock<INotificationService>();
        var auditService = new Mock<IAuditService>();
        var validator = new Mock<IRegistrationValidationService>();
        validator.Setup(v => v.ValidateRegistration(It.IsAny<RegistrationViewModel>(), It.IsAny<ModelStateDictionary>())).Returns(true);
        var compression = new Mock<IFileCompressionService>();
        var rateLimiter = new Mock<IRateLimiterService>();
        var ctrl = new RegistrationController(db, idGen.Object, env.Object, notifService.Object, auditService.Object, validator.Object, compression.Object, rateLimiter.Object);

        var model = Helpers.GetValidViewModel();
        var result = await ctrl.Submit(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.False(ctrl.ModelState.IsValid);
    }

    // ---- Submit success ----
    [Fact]
    public async Task Submit_CreatesRegistration_WithPendingStatus()
    {
        var db = Helpers.CreateDbContext("reg5");
        Helpers.SeedLookups(db);
        var idGen = new Mock<IRecordIdGenerator>();
        idGen.Setup(g => g.GenerateUniqueIdAsync()).ReturnsAsync("TEST1234");
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var notifService = new Mock<INotificationService>();
        notifService.Setup(n => n.NotifyMandoobsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var auditService = new Mock<IAuditService>();
        auditService.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var validator = new Mock<IRegistrationValidationService>();
        validator.Setup(v => v.ValidateRegistration(It.IsAny<RegistrationViewModel>(), It.IsAny<ModelStateDictionary>())).Returns(true);
        var compression = new Mock<IFileCompressionService>();
        var rateLimiter = new Mock<IRateLimiterService>();

        var ctrl = new RegistrationController(db, idGen.Object, env.Object, notifService.Object, auditService.Object, validator.Object, compression.Object, rateLimiter.Object);
        Helpers.SetupControllerContext(ctrl);

        var model = Helpers.GetValidViewModel();

        ViewResult? view;
        try
        {
            var result = await ctrl.Submit(model);
            view = Assert.IsType<ViewResult>(result);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Controller threw exception: {ex}");
            return;
        }

        var errors = ctrl.ModelState.Values.SelectMany(v => v.Errors).ToList();
        Assert.True(view.ViewName == "Success", $"ViewName: '{view.ViewName}', ModelState errors: {string.Join("; ", errors.Select(e => e.ErrorMessage))}");

        var reg = db.FamilyRegistrations.Include(r => r.FamilyHead).First();
        Assert.Equal("TEST1234", reg.RecordId);
        Assert.Equal(RegistrationApprovalStatus.Pending, reg.ApprovalStatus);
        Assert.Equal("123456789", reg.FamilyHead.IdNumber);
    }
}

// =========================================================================
// RECORD CONTROLLER  (Login, Edit guard)
// =========================================================================
public class RecordControllerTests
{
    private static ApplicationDbContext SetupDbWithRegistration(RegistrationApprovalStatus status)
    {
        var db = Helpers.CreateDbContext($"rec_{status}_{Guid.NewGuid()}");
        Helpers.SeedLookups(db);
        var head = new Person
        {
            FirstName = "رب", LastName = "الأسرة", IdNumber = "999999999",
            DateOfBirth = new DateTime(1980, 1, 1),
            Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        db.SaveChanges();

        var reg = new FamilyRegistration
        {
            RecordId = "ABCD1234",
            FamilyHeadId = head.Id,
            PasswordHash = Helpers.HashPassword("mypassword"),
            ApprovalStatus = status,
            SectorId = 1,
            PhoneNumber = "0591234567",
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();
        return db;
    }

    private static RecordController CreateController(ApplicationDbContext db)
    {
        var audit = new Mock<IAuditService>();
        var notifService = new Mock<INotificationService>();
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var validator = new Mock<IRegistrationValidationService>();
        var compression = new Mock<IFileCompressionService>();
        var rateLimiter = new Mock<IRateLimiterService>();
        return new RecordController(db, audit.Object, notifService.Object, env.Object, validator.Object, compression.Object, rateLimiter.Object);
    }

    [Fact]
    public async Task Login_Approved_RedirectsToEdit()
    {
        var db = SetupDbWithRegistration(RegistrationApprovalStatus.Approved);
        var ctrl = CreateController(db);
        Helpers.SetupControllerContext(ctrl);

        var result = await ctrl.Login("999999999", "mypassword");
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirect.ActionName);
    }

    [Fact]
    public async Task Login_Pending_ShowsError()
    {
        var db = SetupDbWithRegistration(RegistrationApprovalStatus.Pending);
        var ctrl = CreateController(db);
        Helpers.SetupControllerContext(ctrl);

        var result = await ctrl.Login("999999999", "mypassword");
        var view = Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_Rejected_ShowsError()
    {
        var db = SetupDbWithRegistration(RegistrationApprovalStatus.Rejected);
        var ctrl = CreateController(db);
        Helpers.SetupControllerContext(ctrl);

        var result = await ctrl.Login("999999999", "mypassword");
        var view = Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_WrongPassword_ShowsError()
    {
        var db = SetupDbWithRegistration(RegistrationApprovalStatus.Approved);
        var ctrl = CreateController(db);
        Helpers.SetupControllerContext(ctrl);

        var result = await ctrl.Login("999999999", "wrongpassword");
        var view = Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
    }

    [Fact]
    public async Task Edit_WithApprovedSession_ReturnsView()
    {
        var db = SetupDbWithRegistration(RegistrationApprovalStatus.Approved);
        Helpers.SeedLookups(db);
        var ctrl = CreateController(db);
        var reg = db.FamilyRegistrations.First();
        Helpers.SetupControllerContext(ctrl, reg.Id, "EditRegistrationId");

        var result = await ctrl.Edit();
        var view = Assert.IsType<ViewResult>(result);
        Assert.Null(view.ViewName); // null = default action name "Edit"
    }

    [Fact]
    public async Task Edit_WithPendingSession_RedirectsToLogin()
    {
        var db = SetupDbWithRegistration(RegistrationApprovalStatus.Pending);
        Helpers.SeedLookups(db);
        var ctrl = CreateController(db);
        var reg = db.FamilyRegistrations.First();
        Helpers.SetupControllerContext(ctrl, reg.Id, "EditRegistrationId");

        var result = await ctrl.Edit();
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Null(ctrl.HttpContext!.Session.GetInt32("EditRegistrationId"));
    }

    [Fact]
    public async Task Edit_NoSession_RedirectsToLogin()
    {
        var db = Helpers.CreateDbContext("rec_no_sesh");
        Helpers.SeedLookups(db);
        var ctrl = CreateController(db);
        Helpers.SetupControllerContext(ctrl);

        var result = await ctrl.Edit();
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
    }

    [Fact]
    public async Task Login_EmptyFields_ShowsError()
    {
        var db = Helpers.CreateDbContext("rec_empty");
        var ctrl = CreateController(db);
        Helpers.SetupControllerContext(ctrl);

        var result = await ctrl.Login("", "");
        var view = Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
    }
}

// =========================================================================
// ADMIN CONTROLLER
// =========================================================================
public class AdminControllerTests
{
    private static (AdminController, DefaultHttpContext, ApplicationDbContext) SetupAdmin(string role = "Admin", string sector = "A")
    {
        var db = Helpers.CreateDbContext($"admin_{Guid.NewGuid()}");
        Helpers.SeedLookups(db);

        if (!db.Admins.Any())
        {
            var sectorEntity = db.Sectors.First(s => s.Name == sector);
            db.Admins.Add(new Admin
            {
                Name = "المدير", NationalId = "admin",
                Mobile = "0000000000", PasswordHash = Helpers.HashPassword("admin123"),
                Role = role == "Admin" ? AdminRole.Admin : AdminRole.Mandoob,
                SectorId = sectorEntity.Id
            });
            db.SaveChanges();
        }

        var auditService = new Mock<IAuditService>();
        var notifService = new Mock<INotificationService>();
        var validator = new Mock<IRegistrationValidationService>();
        var rateLimiter = new Mock<IRateLimiterService>();
        var ctrl = new AdminController(db, auditService.Object, notifService.Object, validator.Object, rateLimiter.Object);
        var http = new DefaultHttpContext();
        http.Session = new TestSession();
        var admin = db.Admins.First();
        http.Session.SetInt32("AdminId", admin.Id);
        http.Session.SetString("AdminName", admin.Name);
        http.Session.SetString("AdminRole", role);
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return (ctrl, http, db);
    }

    private static FamilyRegistration CreateReg(ApplicationDbContext db, string sector, RegistrationApprovalStatus status = RegistrationApprovalStatus.Pending)
    {
        var sectorEntity = db.Sectors.First(s => s.Name == sector);
        var head = new Person
        {
            FirstName = "رب", LastName = "عائلة", IdNumber = Guid.NewGuid().ToString()[..9],
            DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        db.SaveChanges();

        var reg = new FamilyRegistration
        {
            RecordId = Guid.NewGuid().ToString()[..8],
            FamilyHeadId = head.Id,
            SectorId = sectorEntity.Id,
            PhoneNumber = "0591234567",
            ApprovalStatus = status,
            PasswordHash = "x",
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();
        return reg;
    }

    // ---- Registrations list ----
    [Fact]
    public async Task Registrations_ShowsPendingOnly_ByDefault()
    {
        var (ctrl, http, db) = SetupAdmin();
        CreateReg(db, "A", RegistrationApprovalStatus.Pending);
        CreateReg(db, "A", RegistrationApprovalStatus.Approved);
        CreateReg(db, "A", RegistrationApprovalStatus.Pending);

        var result = await ctrl.Registrations();
        var view = Assert.IsType<ViewResult>(result);
        var list = Assert.IsType<List<RegistrationApprovalViewModel>>(view.Model);
        Assert.Equal(2, list.Count);
        Assert.All(list, r => Assert.Equal(RegistrationApprovalStatus.Pending, r.ApprovalStatus));
    }

    [Fact]
    public async Task Registrations_FiltersByStatus()
    {
        var (ctrl, http, db) = SetupAdmin();
        CreateReg(db, "A", RegistrationApprovalStatus.Pending);
        CreateReg(db, "A", RegistrationApprovalStatus.Approved);

        var result = await ctrl.Registrations(status: "Approved");
        var view = Assert.IsType<ViewResult>(result);
        var list = Assert.IsType<List<RegistrationApprovalViewModel>>(view.Model);
        Assert.Single(list);
        Assert.Equal(RegistrationApprovalStatus.Approved, list[0].ApprovalStatus);
    }

    [Fact]
    public async Task Registrations_FiltersBySector()
    {
        var (ctrl, http, db) = SetupAdmin();
        CreateReg(db, "A", RegistrationApprovalStatus.Pending);
        CreateReg(db, "B", RegistrationApprovalStatus.Pending);

        var result = await ctrl.Registrations(sector: "A");
        var view = Assert.IsType<ViewResult>(result);
        var list = Assert.IsType<List<RegistrationApprovalViewModel>>(view.Model);
        Assert.Single(list);
        Assert.Equal("A", list[0].Sector);
    }

    [Fact]
    public async Task Registrations_NotAuth_RedirectsToLogin()
    {
        var db = Helpers.CreateDbContext("admin_noauth");
        var auditService = new Mock<IAuditService>();
        var notifService = new Mock<INotificationService>();
        var validator = new Mock<IRegistrationValidationService>();
        var rateLimiter = new Mock<IRateLimiterService>();
        var ctrl = new AdminController(db, auditService.Object, notifService.Object, validator.Object, rateLimiter.Object);
        var http = new DefaultHttpContext();
        http.Session = new TestSession();
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };

        var result = await ctrl.Registrations();
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
    }

    // ---- Approve ----
    [Fact]
    public async Task ApproveRegistration_SetsStatusAndTracksApprover()
    {
        var (ctrl, http, db) = SetupAdmin();
        var reg = CreateReg(db, "A", RegistrationApprovalStatus.Pending);
        var adminId = http.Session.GetInt32("AdminId")!.Value;

        var result = await ctrl.ApproveRegistration(reg.Id);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Registrations", redirect.ActionName);

        db.Entry(reg).Reload();
        Assert.Equal(RegistrationApprovalStatus.Approved, reg.ApprovalStatus);
        Assert.Equal(adminId, reg.ApprovedById);
        Assert.NotNull(reg.ApprovedAt);
    }

    [Fact]
    public async Task ApproveRegistration_NotFound_ReturnsNotFound()
    {
        var (ctrl, http, db) = SetupAdmin();
        Assert.IsType<NotFoundResult>(await ctrl.ApproveRegistration(99999));
    }

    // ---- Reject ----
    [Fact]
    public async Task RejectRegistration_WithoutReason_RedirectsBack()
    {
        var (ctrl, http, db) = SetupAdmin();
        var reg = CreateReg(db, "A", RegistrationApprovalStatus.Pending);

        var result = await ctrl.RejectRegistration(reg.Id, null);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Registrations", redirect.ActionName);

        db.Entry(reg).Reload();
        Assert.Equal(RegistrationApprovalStatus.Pending, reg.ApprovalStatus);
    }

    [Fact]
    public async Task RejectRegistration_WithReason_SetsStatus()
    {
        var (ctrl, http, db) = SetupAdmin();
        var reg = CreateReg(db, "A", RegistrationApprovalStatus.Pending);
        var adminId = http.Session.GetInt32("AdminId")!.Value;

        var result = await ctrl.RejectRegistration(reg.Id, "سبب الرفض");
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Registrations", redirect.ActionName);

        db.Entry(reg).Reload();
        Assert.Equal(RegistrationApprovalStatus.Rejected, reg.ApprovalStatus);
        Assert.Equal("سبب الرفض", reg.RejectionReason);
        Assert.Equal(adminId, reg.RejectedById);
        Assert.NotNull(reg.RejectedAt);
    }

    [Fact]
    public async Task RejectRegistration_ShortReason_RedirectsBack()
    {
        var (ctrl, http, db) = SetupAdmin();
        var reg = CreateReg(db, "A", RegistrationApprovalStatus.Pending);

        var result = await ctrl.RejectRegistration(reg.Id, "ab");
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Registrations", redirect.ActionName);

        db.Entry(reg).Reload();
        Assert.Equal(RegistrationApprovalStatus.Pending, reg.ApprovalStatus);
    }

    // ---- Approve clears rejection ----
    [Fact]
    public async Task ApproveRegistration_ClearsRejectionFields()
    {
        var (ctrl, http, db) = SetupAdmin();
        var reg = CreateReg(db, "A", RegistrationApprovalStatus.Rejected);
        reg.RejectedById = http.Session.GetInt32("AdminId");
        reg.RejectedAt = DateTime.UtcNow.AddDays(-1);
        reg.RejectionReason = "سبب سابق";
        await db.SaveChangesAsync();

        await ctrl.ApproveRegistration(reg.Id);

        db.Entry(reg).Reload();
        Assert.Equal(RegistrationApprovalStatus.Approved, reg.ApprovalStatus);
        Assert.Null(reg.RejectedById);
        Assert.Null(reg.RejectedAt);
        Assert.Null(reg.RejectionReason);
    }

    // ---- Mandoob sector filtering ----
    [Fact]
    public async Task Registrations_MandoobOnlySeesOwnSector()
    {
        var (ctrl, http, db) = SetupAdmin(role: "Mandoob", sector: "A");
        CreateReg(db, "A", RegistrationApprovalStatus.Pending);
        CreateReg(db, "B", RegistrationApprovalStatus.Pending);

        var result = await ctrl.Registrations();
        var view = Assert.IsType<ViewResult>(result);
        var list = Assert.IsType<List<RegistrationApprovalViewModel>>(view.Model);
        Assert.Single(list);
        Assert.Equal("A", list[0].Sector);
    }
}

// =========================================================================
// WALLET TYPE VALIDATION
// =========================================================================
public class ValidationServiceTests
{
    [Fact]
    public void WalletType_Required_WhenWalletProvided()
    {
        var validator = new RegistrationValidationService();
        var model = Helpers.GetValidViewModel();
        model.Wallet = "123456789012345";
        model.WalletType = "";
        var state = new ModelStateDictionary();

        var result = validator.ValidateRegistration(model, state);

        Assert.False(result);
        Assert.True(state.ContainsKey("WalletType"));
    }

    [Fact]
    public void WalletType_NotRequired_WhenWalletNull()
    {
        var validator = new RegistrationValidationService();
        var model = Helpers.GetValidViewModel();
        model.Wallet = "";
        var state = new ModelStateDictionary();

        var result = validator.ValidateRegistration(model, state);

        Assert.True(result);
    }

    [Fact]
    public void WalletType_Valid_WhenBothProvided()
    {
        var validator = new RegistrationValidationService();
        var model = Helpers.GetValidViewModel();
        model.Wallet = "123456789012345";
        model.WalletType = "بنك";
        var state = new ModelStateDictionary();

        var result = validator.ValidateRegistration(model, state);

        Assert.True(result);
    }

    [Fact]
    public void MemberMaritalStatus_Required_IncludesMemberName()
    {
        var validator = new RegistrationValidationService();
        var model = Helpers.GetValidViewModel();
        model.Members.Add(new MemberViewModel
        {
            FirstName = "فاطمة",
            SecondName = "محمد",
            ThirdName = "أحمد",
            LastName = "السيد",
            IdNumber = "987654321",
            DateOfBirth = new DateTime(1995, 5, 5),
            Gender = "أنثى",
            RelationshipToHead = "زوجة",
            HealthStatus = "سليم",
            MaritalStatus = ""
        });
        var state = new ModelStateDictionary();

        var result = validator.ValidateRegistration(model, state);

        Assert.False(result);
        var error = state.Values.SelectMany(v => v.Errors).Single().ErrorMessage;
        Assert.Contains("فاطمة محمد أحمد السيد", error);
        Assert.Contains("الحالة الاجتماعية", error);
    }
}

// =========================================================================
// DASHBOARD CTE
// =========================================================================
public class AdminDashboardTests
{
    [Fact(Skip = "Requires SQL Server (SqlQueryRaw not supported with InMemory)")]
    public async Task Dashboard_ReturnsView_WithSectors()
    {
    }

    [Fact(Skip = "Requires SQL Server (SqlQueryRaw not supported with InMemory)")]
    public async Task Dashboard_ExcludesSoftDeletedRegistrations()
    {
    }
}

// =========================================================================
// ADMIN EDIT (no longer blocks non-Pending)
// =========================================================================
public class AdminEditTests
{
    [Fact]
    public async Task AdminEditRegistration_Approved_ReturnsView()
    {
        var db = Helpers.CreateDbContext($"adminedit_app_{Guid.NewGuid()}");
        Helpers.SeedLookups(db);
        db.Admins.Add(new Admin { Name = "Admin", NationalId = "admin", Mobile = "000", PasswordHash = "x", Role = AdminRole.Admin });
        db.SaveChanges();

        var sector = db.Sectors.First();
        var head = new Person
        {
            FirstName = "رب", LastName = "أسرة", IdNumber = "222222222",
            DateOfBirth = new DateTime(1980, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var reg = new FamilyRegistration
        {
            RecordId = "APPRVED1",
            FamilyHeadId = head.Id, SectorId = sector.Id, PhoneNumber = "0591234567",
            ApprovalStatus = RegistrationApprovalStatus.Approved,
            PasswordHash = "x", IsChildHeaded = false, LivesInTent = false,
            HasBathroom = false, NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();

        var auditService = new Mock<IAuditService>();
        var notifService = new Mock<INotificationService>();
        var validator = new Mock<IRegistrationValidationService>();
        var rateLimiter = new Mock<IRateLimiterService>();
        var ctrl = new AdminController(db, auditService.Object, notifService.Object, validator.Object, rateLimiter.Object);
        var http = new DefaultHttpContext();
        http.Session = new TestSession();
        var admin = db.Admins.First();
        http.Session.SetInt32("AdminId", admin.Id);
        http.Session.SetString("AdminName", admin.Name);
        http.Session.SetString("AdminRole", "Admin");
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var result = await ctrl.AdminEditRegistration(reg.Id);
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("~/Views/Record/Edit.cshtml", view.ViewName);
    }

    [Fact]
    public async Task AdminEditRegistration_Rejected_ReturnsView()
    {
        var db = Helpers.CreateDbContext($"adminedit_rej_{Guid.NewGuid()}");
        Helpers.SeedLookups(db);
        db.Admins.Add(new Admin { Name = "Admin", NationalId = "admin", Mobile = "000", PasswordHash = "x", Role = AdminRole.Admin });
        db.SaveChanges();

        var sector = db.Sectors.First();
        var head = new Person
        {
            FirstName = "رب", LastName = "أسرة", IdNumber = "333333333",
            DateOfBirth = new DateTime(1980, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var reg = new FamilyRegistration
        {
            RecordId = "REJECTD1",
            FamilyHeadId = head.Id, SectorId = sector.Id, PhoneNumber = "0591234567",
            ApprovalStatus = RegistrationApprovalStatus.Rejected,
            PasswordHash = "x", IsChildHeaded = false, LivesInTent = false,
            HasBathroom = false, NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();

        var auditService = new Mock<IAuditService>();
        var notifService = new Mock<INotificationService>();
        var validator = new Mock<IRegistrationValidationService>();
        var rateLimiter = new Mock<IRateLimiterService>();
        var ctrl = new AdminController(db, auditService.Object, notifService.Object, validator.Object, rateLimiter.Object);
        var http = new DefaultHttpContext();
        http.Session = new TestSession();
        var admin = db.Admins.First();
        http.Session.SetInt32("AdminId", admin.Id);
        http.Session.SetString("AdminName", admin.Name);
        http.Session.SetString("AdminRole", "Admin");
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var result = await ctrl.AdminEditRegistration(reg.Id);
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("~/Views/Record/Edit.cshtml", view.ViewName);
    }
}

// =========================================================================
// TEST SESSION (for mocking ISession)
// =========================================================================
public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _data = new();
    public string Id => "test";
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _data.Keys;

    public void Clear() => _data.Clear();
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void Remove(string key) => _data.Remove(key);
    public void Set(string key, byte[] value) => _data[key] = value;

    public bool TryGetValue(string key, out byte[] value)
        => _data.TryGetValue(key, out value!);
}

// =========================================================================
// REGISTRATION CHANGE TRACKER
// =========================================================================
public class RegistrationChangeTrackerTests
{
    [Fact]
    public async Task CaptureAsync_SnapshotHasHeadFields()
    {
        var db = Helpers.CreateDbContext($"rct_cap_{Guid.NewGuid()}");
        var head = new Person
        {
            FirstName = "محمد", LastName = "السيد", IdNumber = "123456789",
            DateOfBirth = new DateTime(1990, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var reg = new FamilyRegistration
        {
            RecordId = "RCT00001", FamilyHeadId = head.Id, SectorId = 1,
            PhoneNumber = "0591234567", PasswordHash = "x",
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();

        var snap = await RegistrationChangeTracker.CaptureAsync(reg);

        Assert.Equal("محمد", snap.Head["FirstName"]);
        Assert.Equal("123456789", snap.Head["IdNumber"]);
        Assert.Equal("ذكر", snap.Head["Gender"]);
    }

    [Fact]
    public async Task CaptureAsync_SnapshotHasRegistrationFields()
    {
        var db = Helpers.CreateDbContext($"rct_cap2_{Guid.NewGuid()}");
        var head = new Person
        {
            FirstName = "A", IdNumber = "111111111",
            DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var reg = new FamilyRegistration
        {
            RecordId = "RCT00002", FamilyHeadId = head.Id, SectorId = 1,
            PhoneNumber = "0591234567", PasswordHash = "x", Wallet = "12345",
            WalletType = "بنك", IsChildHeaded = false, LivesInTent = true,
            TentType = "Installation", HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();

        var snap = await RegistrationChangeTracker.CaptureAsync(reg);

        Assert.Equal("0591234567", snap.Registration["PhoneNumber"]);
        Assert.Equal("12345", snap.Registration["Wallet"]);
        Assert.Equal("بنك", snap.Registration["WalletType"]);
        Assert.Equal("Installation", snap.Registration["TentType"]);
    }

    [Fact]
    public async Task CaptureAsync_SnapshotHasMembers()
    {
        var db = Helpers.CreateDbContext($"rct_cap3_{Guid.NewGuid()}");
        var head = new Person
        {
            FirstName = "A", IdNumber = "111111111",
            DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var member = new Person
        {
            FirstName = "B", IdNumber = "222222222",
            DateOfBirth = DateTime.Today, Gender = "أنثى", HealthStatus = "سليم"
        };
        db.Persons.Add(member);
        var reg = new FamilyRegistration
        {
            RecordId = "RCT00003", FamilyHeadId = head.Id, SectorId = 1,
            PhoneNumber = "0591234567", PasswordHash = "x",
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.FamilyMembers.Add(new FamilyMember { RegistrationId = reg.Id, PersonId = member.Id, RelationshipToHead = "زوجة" });
        db.SaveChanges();

        var snap = await RegistrationChangeTracker.CaptureAsync(reg);

        Assert.Single(snap.Members);
        Assert.True(snap.Members.ContainsKey("222222222"));
        Assert.Equal("B", snap.Members["222222222"].Fields["FirstName"]);
        Assert.Equal("زوجة", snap.Members["222222222"].Fields["RelationshipToHead"]);
    }

    [Fact]
    public async Task BuildDiffAsync_DetectsHeadFieldChange()
    {
        var db = Helpers.CreateDbContext($"rct_diff1_{Guid.NewGuid()}");
        var head = new Person
        {
            FirstName = "قديم", LastName = "اسم", IdNumber = "333333333",
            DateOfBirth = new DateTime(1990, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var reg = new FamilyRegistration
        {
            RecordId = "RCT00010", FamilyHeadId = head.Id, SectorId = 1,
            PhoneNumber = "0591234567", PasswordHash = "x",
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();

        var snap = await RegistrationChangeTracker.CaptureAsync(reg);

        var model = new RegistrationViewModel
        {
            Head = new PersonViewModel
            {
                FirstName = "جديد", LastName = "اسم", IdNumber = "333333333",
                DateOfBirth = new DateTime(1990, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
            },
            SectorId = 1, PhoneNumber = "0591234567"
        };

        var diff = await RegistrationChangeTracker.BuildDiffAsync(db, snap, model);

        Assert.False(diff.IsEmpty);
        Assert.True(diff.Head.ContainsKey("Changes"));
        Assert.Equal("قديم", diff.Head["Changes"]["FirstName"].Old);
        Assert.Equal("جديد", diff.Head["Changes"]["FirstName"].New);
    }

    [Fact]
    public async Task BuildDiffAsync_DetectsRegistrationFieldChange()
    {
        var db = Helpers.CreateDbContext($"rct_diff2_{Guid.NewGuid()}");
        Helpers.SeedLookups(db);
        var head = new Person
        {
            FirstName = "A", IdNumber = "444444444",
            DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var reg = new FamilyRegistration
        {
            RecordId = "RCT00011", FamilyHeadId = head.Id, SectorId = 1,
            PhoneNumber = "0591234567", PasswordHash = "x",
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();

        var snap = await RegistrationChangeTracker.CaptureAsync(reg);

        var model = new RegistrationViewModel
        {
            Head = new PersonViewModel
            {
                FirstName = "A", IdNumber = "444444444",
                DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
            },
            SectorId = 2, PhoneNumber = "0599999999"
        };

        var diff = await RegistrationChangeTracker.BuildDiffAsync(db, snap, model);

        Assert.False(diff.IsEmpty);
        Assert.Contains("Changes", diff.Registration.Keys);
    }

    [Fact]
    public async Task BuildDiffAsync_DetectsMemberAdded()
    {
        var db = Helpers.CreateDbContext($"rct_diff3_{Guid.NewGuid()}");
        var head = new Person
        {
            FirstName = "A", IdNumber = "555555555",
            DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var reg = new FamilyRegistration
        {
            RecordId = "RCT00012", FamilyHeadId = head.Id, SectorId = 1,
            PhoneNumber = "0591234567", PasswordHash = "x",
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();

        var snap = await RegistrationChangeTracker.CaptureAsync(reg);

        var model = new RegistrationViewModel
        {
            Head = new PersonViewModel
            {
                FirstName = "A", IdNumber = "555555555",
                DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
            },
            SectorId = 1, PhoneNumber = "0591234567",
            Members = new List<MemberViewModel>
            {
                new() { FirstName = "جديد", IdNumber = "666666666", DateOfBirth = DateTime.Today, Gender = "أنثى", HealthStatus = "سليم", RelationshipToHead = "زوجة" }
            }
        };

        var diff = await RegistrationChangeTracker.BuildDiffAsync(db, snap, model);

        Assert.Single(diff.MembersAdded);
        Assert.Equal("666666666", diff.MembersAdded[0]["IdNumber"]);
    }

    [Fact]
    public async Task BuildDiffAsync_DetectsMemberRemoved()
    {
        var db = Helpers.CreateDbContext($"rct_diff4_{Guid.NewGuid()}");
        var head = new Person
        {
            FirstName = "A", IdNumber = "777777777",
            DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var member = new Person
        {
            FirstName = "B", IdNumber = "888888888",
            DateOfBirth = DateTime.Today, Gender = "أنثى", HealthStatus = "سليم"
        };
        db.Persons.Add(member);
        var reg = new FamilyRegistration
        {
            RecordId = "RCT00013", FamilyHeadId = head.Id, SectorId = 1,
            PhoneNumber = "0591234567", PasswordHash = "x",
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.FamilyMembers.Add(new FamilyMember { RegistrationId = reg.Id, PersonId = member.Id, RelationshipToHead = "زوجة" });
        db.SaveChanges();

        var snap = await RegistrationChangeTracker.CaptureAsync(reg);

        var model = new RegistrationViewModel
        {
            Head = new PersonViewModel
            {
                FirstName = "A", IdNumber = "777777777",
                DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
            },
            SectorId = 1, PhoneNumber = "0591234567"
        };

        var diff = await RegistrationChangeTracker.BuildDiffAsync(db, snap, model);

        Assert.Single(diff.MembersRemoved);
        Assert.Equal("888888888", diff.MembersRemoved[0]["IdNumber"]);
    }

    [Fact]
    public async Task BuildDiffAsync_EmptyDiff_WhenNoChanges()
    {
        var db = Helpers.CreateDbContext($"rct_diff5_{Guid.NewGuid()}");
        var head = new Person
        {
            FirstName = "A", LastName = "B", IdNumber = "999999999",
            DateOfBirth = new DateTime(1990, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var reg = new FamilyRegistration
        {
            RecordId = "RCT00014", FamilyHeadId = head.Id, SectorId = 1,
            PhoneNumber = "0591234567", PasswordHash = "x",
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();

        var snap = await RegistrationChangeTracker.CaptureAsync(reg);

        var model = new RegistrationViewModel
        {
            Head = new PersonViewModel
            {
                FirstName = "A", LastName = "B", IdNumber = "999999999",
                DateOfBirth = new DateTime(1990, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
            },
            SectorId = 1, PhoneNumber = "0591234567"
        };

        var diff = await RegistrationChangeTracker.BuildDiffAsync(db, snap, model);

        Assert.True(diff.IsEmpty);
    }

    [Fact]
    public async Task ToAuditPayload_ProducesStructuredOutput()
    {
        var db = Helpers.CreateDbContext($"rct_payload_{Guid.NewGuid()}");
        var head = new Person
        {
            FirstName = "قديم", IdNumber = "121212121",
            DateOfBirth = new DateTime(1990, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var reg = new FamilyRegistration
        {
            RecordId = "RCTPAYLD", FamilyHeadId = head.Id, SectorId = 1,
            PhoneNumber = "0591234567", PasswordHash = "x",
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();

        var snap = await RegistrationChangeTracker.CaptureAsync(reg);
        var model = new RegistrationViewModel
        {
            Head = new PersonViewModel
            {
                FirstName = "جديد", IdNumber = "121212121",
                DateOfBirth = new DateTime(1990, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
            },
            SectorId = 1, PhoneNumber = "0591234567"
        };

        var diff = await RegistrationChangeTracker.BuildDiffAsync(db, snap, model);
        var payload = RegistrationChangeTracker.ToAuditPayload(diff, "test", "رب الأسرة", "A", "RCTPAYLD");

        var dict = Assert.IsType<Dictionary<string, object?>>(payload);
        Assert.Contains("Meta", dict.Keys);
        Assert.Contains("رب الأسرة", dict.Keys);
    }

    [Fact]
    public async Task ToAuditPayload_EmptyDiff_ShowsNoChanges()
    {
        var db = Helpers.CreateDbContext($"rct_empty_{Guid.NewGuid()}");
        var head = new Person
        {
            FirstName = "ثابت", IdNumber = "131313131",
            DateOfBirth = new DateTime(1990, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        var reg = new FamilyRegistration
        {
            RecordId = "RCTEMPTY", FamilyHeadId = head.Id, SectorId = 1,
            PhoneNumber = "0591234567", PasswordHash = "x",
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();

        var snap = await RegistrationChangeTracker.CaptureAsync(reg);
        var model = new RegistrationViewModel
        {
            Head = new PersonViewModel
            {
                FirstName = "ثابت", IdNumber = "131313131",
                DateOfBirth = new DateTime(1990, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
            },
            SectorId = 1, PhoneNumber = "0591234567"
        };

        var diff = await RegistrationChangeTracker.BuildDiffAsync(db, snap, model);
        Assert.True(diff.IsEmpty);
        Assert.Empty(diff.Head);
        Assert.Empty(diff.Registration);
        Assert.Empty(diff.MembersAdded);
        Assert.Empty(diff.MembersRemoved);
    }
}

// =========================================================================
// ADMIN CHANGE PASSWORD
// =========================================================================
public class AdminChangePasswordTests
{
    [Fact]
    public async Task Login_PasswordMatchesNationalId_RedirectsToChangePassword()
    {
        var db = Helpers.CreateDbContext($"acp1_{Guid.NewGuid()}");
        var sector = new Sector { Name = "X" };
        db.Sectors.Add(sector);
        db.Admins.Add(new Admin
        {
            Name = "Test", NationalId = "123456789",
            PasswordHash = Helpers.HashPassword("123456789"),
            Role = AdminRole.Admin, SectorId = sector.Id
        });
        db.SaveChanges();

        var audit = new Mock<IAuditService>();
        audit.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var notif = new Mock<INotificationService>();
        var val = new Mock<IRegistrationValidationService>();
        var rate = new Mock<IRateLimiterService>();
        var ctrl = new AdminController(db, audit.Object, notif.Object, val.Object, rate.Object);
        var http = new DefaultHttpContext();
        http.Session = new TestSession();
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var result = await ctrl.Login("123456789", "123456789");
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ChangePassword", redirect.ActionName);
    }

    [Fact]
    public async Task Dashboard_ForcePasswordChange_RedirectsToChangePassword()
    {
        var db = Helpers.CreateDbContext($"acp2_{Guid.NewGuid()}");
        var sector = new Sector { Name = "X" };
        db.Sectors.Add(sector);
        var admin = new Admin
        {
            Name = "Test", NationalId = "admin", Mobile = "000",
            PasswordHash = Helpers.HashPassword("admin"),
            Role = AdminRole.Admin, SectorId = sector.Id
        };
        db.Admins.Add(admin);
        db.SaveChanges();

        var audit = new Mock<IAuditService>();
        var notif = new Mock<INotificationService>();
        var val = new Mock<IRegistrationValidationService>();
        var rate = new Mock<IRateLimiterService>();
        var ctrl = new AdminController(db, audit.Object, notif.Object, val.Object, rate.Object);
        var http = new DefaultHttpContext();
        http.Session = new TestSession();
        http.Session.SetInt32("AdminId", admin.Id);
        http.Session.SetString("AdminName", admin.Name);
        http.Session.SetString("AdminRole", "Admin");
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var result = await ctrl.Dashboard();
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ChangePassword", redirect.ActionName);
    }

    [Fact]
    public async Task ChangePassword_PreventsSameAsNationalId()
    {
        var db = Helpers.CreateDbContext($"acp3_{Guid.NewGuid()}");
        var sector = new Sector { Name = "X" };
        db.Sectors.Add(sector);
        var admin = new Admin
        {
            Name = "Test", NationalId = "123456789", Mobile = "000",
            PasswordHash = Helpers.HashPassword("oldpass"),
            Role = AdminRole.Admin, SectorId = sector.Id
        };
        db.Admins.Add(admin);
        db.SaveChanges();

        var audit = new Mock<IAuditService>();
        var notif = new Mock<INotificationService>();
        var val = new Mock<IRegistrationValidationService>();
        var rate = new Mock<IRateLimiterService>();
        var ctrl = new AdminController(db, audit.Object, notif.Object, val.Object, rate.Object);
        var http = new DefaultHttpContext();
        http.Session = new TestSession();
        http.Session.SetInt32("AdminId", admin.Id);
        http.Session.SetString("AdminName", admin.Name);
        http.Session.SetString("AdminRole", "Admin");
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var result = await ctrl.ChangePassword("oldpass", "123456789", "123456789");
        var view = Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
        Assert.Contains("يجب أن تكون مختلفة عن رقم الهوية", ctrl.ModelState[""]?.Errors[0].ErrorMessage ?? "");
    }

    [Fact]
    public async Task ChangePassword_Success_ReturnsToDashboard()
    {
        var db = Helpers.CreateDbContext($"acp4_{Guid.NewGuid()}");
        var sector = new Sector { Name = "X" };
        db.Sectors.Add(sector);
        var admin = new Admin
        {
            Name = "Test", NationalId = "123456789", Mobile = "000",
            PasswordHash = Helpers.HashPassword("oldpass"),
            Role = AdminRole.Admin, SectorId = sector.Id
        };
        db.Admins.Add(admin);
        db.SaveChanges();

        var audit = new Mock<IAuditService>();
        audit.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var notif = new Mock<INotificationService>();
        var val = new Mock<IRegistrationValidationService>();
        var rate = new Mock<IRateLimiterService>();
        var ctrl = new AdminController(db, audit.Object, notif.Object, val.Object, rate.Object);
        var http = new DefaultHttpContext();
        http.Session = new TestSession();
        http.Session.SetInt32("AdminId", admin.Id);
        http.Session.SetString("AdminName", admin.Name);
        http.Session.SetString("AdminRole", "Admin");
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var result = await ctrl.ChangePassword("oldpass", "newpass", "newpass");
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Dashboard", redirect.ActionName);
        Assert.Equal("تم تغيير كلمة المرور بنجاح", ctrl.TempData["Success"]);
    }
}

// =========================================================================
// RECORD FORCE PASSWORD CHANGE
// =========================================================================
public class RecordForcePasswordChangeTests
{
    private static FamilyRegistration CreateRegistrationWithPassword(ApplicationDbContext db, string password)
    {
        var head = new Person
        {
            FirstName = "رب", LastName = "أسرة", IdNumber = "123456789",
            DateOfBirth = new DateTime(1980, 1, 1), Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        db.SaveChanges();

        var reg = new FamilyRegistration
        {
            RecordId = "FRC00001", FamilyHeadId = head.Id, SectorId = 1,
            PhoneNumber = "0591234567", PasswordHash = Helpers.HashPassword(password),
            ApprovalStatus = RegistrationApprovalStatus.Approved,
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();
        return reg;
    }

    [Fact]
    public async Task Login_PasswordMatchesIdNumber_SetsMustChangeSession()
    {
        var db = Helpers.CreateDbContext($"rfpc1_{Guid.NewGuid()}");
        Helpers.SeedLookups(db);
        CreateRegistrationWithPassword(db, "123456789");

        var audit = new Mock<IAuditService>();
        audit.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var notif = new Mock<INotificationService>();
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var val = new Mock<IRegistrationValidationService>();
        var comp = new Mock<IFileCompressionService>();
        var rate = new Mock<IRateLimiterService>();
        var ctrl = new RecordController(db, audit.Object, notif.Object, env.Object, val.Object, comp.Object, rate.Object);
        var http = new DefaultHttpContext();
        http.Session = new TestSession();
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        await ctrl.Login("123456789", "123456789");

        var mustChange = http.Session.GetString("MustChangePassword");
        Assert.Equal("1", mustChange);
    }

    [Fact]
    public async Task Edit_WithPasswordMatch_ShowsMustChangeBanner()
    {
        var db = Helpers.CreateDbContext($"rfpc2_{Guid.NewGuid()}");
        Helpers.SeedLookups(db);
        var reg = CreateRegistrationWithPassword(db, "123456789");

        var audit = new Mock<IAuditService>();
        var notif = new Mock<INotificationService>();
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var val = new Mock<IRegistrationValidationService>();
        var comp = new Mock<IFileCompressionService>();
        var rate = new Mock<IRateLimiterService>();
        var ctrl = new RecordController(db, audit.Object, notif.Object, env.Object, val.Object, comp.Object, rate.Object);
        var http = new DefaultHttpContext();
        http.Session = new TestSession();
        http.Session.SetInt32("EditRegistrationId", reg.Id);
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var result = await ctrl.Edit();
        var view = Assert.IsType<ViewResult>(result);

        Assert.True((bool)(view.ViewData["MustChangePassword"] ?? false));
    }
}
