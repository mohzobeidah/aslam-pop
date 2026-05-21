using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Services;
using CampRegistrationApp.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace CampRegistrationApp.SeleniumTests.Infrastructure;

public class CustomWebApplicationFactory : IAsyncLifetime
{
    private WebApplication? _app;
    private readonly string _dbName = $"TestDb_{Guid.NewGuid():N}";

    public IServiceProvider Services => _app?.Services!;

    public int Port { get; } = GetRandomAvailablePort();

    private static int GetRandomAvailablePort()
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public string GetServerUrl() => $"http://127.0.0.1:{Port}";

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DATABASE_SKIP", "true");

        var contentRoot = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "..", "..", "CampRegistrationApp"));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRoot,
            EnvironmentName = "Development"
        });

        builder.WebHost.UseUrls($"http://127.0.0.1:{Port}");

        builder.Services.AddControllersWithViews()
            .AddApplicationPart(typeof(Program).Assembly)
            .AddRazorRuntimeCompilation();

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        builder.Services.AddScoped<IRecordIdGenerator, RecordIdGenerator>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddScoped<INominationService, NominationService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IDummyDataService, DummyDataService>();
        builder.Services.AddScoped<IAssistanceService, AssistanceService>();
        builder.Services.AddScoped<IImportService, ImportService>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(4);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        _app = builder.Build();

        // Seed in-memory DB
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            SeedTestData(db);
        }

        // Configure middleware
        _app.UseRouting();
        _app.UseSession();
        _app.UseStaticFiles();
        _app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        await _app.StartAsync();
    }

    private static void SeedTestData(ApplicationDbContext db)
    {
        var sectors = new List<Sector>
        {
            new() { Name = "A", Camp = "مخيم السلام", Coordinate = "31.5,34.5", Area = "شمالي", ManufacturedTentsCount = 100, HandmadeTentsCount = 50, BathroomsCount = 20 },
            new() { Name = "B", Camp = "مخيم السلام", Coordinate = "31.6,34.6", Area = "جنوبي", ManufacturedTentsCount = 80, HandmadeTentsCount = 40, BathroomsCount = 15 },
            new() { Name = "C", Camp = "مخيم السلام", Coordinate = "31.7,34.7", Area = "شرقي", ManufacturedTentsCount = 120, HandmadeTentsCount = 60, BathroomsCount = 25 },
            new() { Name = "D", Camp = "مخيم السلام", Coordinate = "31.8,34.8", Area = "غربي", ManufacturedTentsCount = 90, HandmadeTentsCount = 45, BathroomsCount = 18 },
        };
        db.Sectors.AddRange(sectors);

        db.HealthStatuses.AddRange(
            new HealthStatus { Name = "سليم" },
            new HealthStatus { Name = "مريض" }
        );

        db.ChronicDiseases.AddRange(
            new ChronicDisease { Name = "سكري" },
            new ChronicDisease { Name = "ضغط" },
            new ChronicDisease { Name = "قلب" },
            new ChronicDisease { Name = "ربو" },
            new ChronicDisease { Name = "فشل كلوي" },
            new ChronicDisease { Name = "سرطان" },
            new ChronicDisease { Name = "ثلاسيميا" },
            new ChronicDisease { Name = "أخرى" }
        );

        db.DisabilityTypes.AddRange(
            new DisabilityType { Name = "حركية" },
            new DisabilityType { Name = "سمعية" },
            new DisabilityType { Name = "بصرية" },
            new DisabilityType { Name = "إصابة حرب" }
        );

        var desires = new[] { "خيم", "اغطية", "فرشات", "ادوات مطبخ", "شوادر", "ملابس", "طرد صحي" };
        foreach (var name in desires)
            db.Desires.Add(new Desire { Name = name });

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

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
