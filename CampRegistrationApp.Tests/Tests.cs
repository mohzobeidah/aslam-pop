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
        db.SaveChanges();
    }

    public static RegistrationViewModel GetValidViewModel()
    {
        return new RegistrationViewModel
        {
            Head = new PersonViewModel
            {
                FirstName = "محمد", SecondName = "أحمد", ThirdName = "علي",
                LastName = "السيد", IdNumber = "123456789", Sector = "A",
                DateOfBirth = new DateTime(1990, 1, 1), Gender = "ذكر",
                HealthStatus = "سليم"
            },
            CurrentStep = 1
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
            FamilyHead = new Person { FirstName = "A", IdNumber = "111111111", Sector = "A", DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم" }
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
        var ctrl = new RegistrationController(db, idGen.Object, env.Object, notifService.Object);

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
        var ctrl = new RegistrationController(db, idGen.Object, env.Object, notifService.Object);

        var result = await ctrl.CheckId("999999999");
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
            FirstName = "A", IdNumber = "123456789", Sector = "A",
            DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        db.SaveChanges();

        var idGen = new Mock<IRecordIdGenerator>();
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var notifService = new Mock<INotificationService>();
        var ctrl = new RegistrationController(db, idGen.Object, env.Object, notifService.Object);

        var result = await ctrl.CheckId("123456789");
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
            FirstName = "Existing", IdNumber = "123456789", Sector = "A",
            DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
        });
        db.SaveChanges();

        var idGen = new Mock<IRecordIdGenerator>();
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var notifService = new Mock<INotificationService>();
        var ctrl = new RegistrationController(db, idGen.Object, env.Object, notifService.Object);

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

        var ctrl = new RegistrationController(db, idGen.Object, env.Object, notifService.Object);

        var model = Helpers.GetValidViewModel();
        var result = await ctrl.Submit(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Success", view.ViewName);

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
            Sector = "A", DateOfBirth = new DateTime(1980, 1, 1),
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
            IsChildHeaded = false, LivesInTent = false, HasBathroom = false,
            NeedsDiapers = false, HasMultipleFamiliesInTent = false
        };
        db.FamilyRegistrations.Add(reg);
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Login_Approved_RedirectsToEdit()
    {
        var db = SetupDbWithRegistration(RegistrationApprovalStatus.Approved);
        var notifService = new Mock<INotificationService>();
        var ctrl = new RecordController(db, notifService.Object);
        Helpers.SetupControllerContext(ctrl);

        var result = await ctrl.Login("999999999", "mypassword");
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirect.ActionName);
    }

    [Fact]
    public async Task Login_Pending_ShowsError()
    {
        var db = SetupDbWithRegistration(RegistrationApprovalStatus.Pending);
        var notifService = new Mock<INotificationService>();
        var ctrl = new RecordController(db, notifService.Object);
        Helpers.SetupControllerContext(ctrl);

        var result = await ctrl.Login("999999999", "mypassword");
        var view = Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_Rejected_ShowsError()
    {
        var db = SetupDbWithRegistration(RegistrationApprovalStatus.Rejected);
        var notifService = new Mock<INotificationService>();
        var ctrl = new RecordController(db, notifService.Object);
        Helpers.SetupControllerContext(ctrl);

        var result = await ctrl.Login("999999999", "mypassword");
        var view = Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_WrongPassword_ShowsError()
    {
        var db = SetupDbWithRegistration(RegistrationApprovalStatus.Approved);
        var notifService = new Mock<INotificationService>();
        var ctrl = new RecordController(db, notifService.Object);
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
        var notifService = new Mock<INotificationService>();
        var ctrl = new RecordController(db, notifService.Object);
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
        var notifService = new Mock<INotificationService>();
        var ctrl = new RecordController(db, notifService.Object);
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
        var notifService = new Mock<INotificationService>();
        var ctrl = new RecordController(db, notifService.Object);
        Helpers.SetupControllerContext(ctrl);

        var result = await ctrl.Edit();
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
    }

    [Fact]
    public async Task Login_EmptyFields_ShowsError()
    {
        var db = Helpers.CreateDbContext("rec_empty");
        var notifService = new Mock<INotificationService>();
        var ctrl = new RecordController(db, notifService.Object);
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

        var ctrl = new AdminController(db);
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
        var head = new Person
        {
            FirstName = "رب", LastName = "عائلة", IdNumber = Guid.NewGuid().ToString()[..9],
            Sector = sector, DateOfBirth = DateTime.Today, Gender = "ذكر", HealthStatus = "سليم"
        };
        db.Persons.Add(head);
        db.SaveChanges();

        var reg = new FamilyRegistration
        {
            RecordId = Guid.NewGuid().ToString()[..8],
            FamilyHeadId = head.Id,
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
        var ctrl = new AdminController(db);
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
    public async Task RejectRegistration_SetsStatus()
    {
        var (ctrl, http, db) = SetupAdmin();
        var reg = CreateReg(db, "A", RegistrationApprovalStatus.Pending);

        var result = await ctrl.RejectRegistration(reg.Id);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Registrations", redirect.ActionName);

        db.Entry(reg).Reload();
        Assert.Equal(RegistrationApprovalStatus.Rejected, reg.ApprovalStatus);
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
