using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using System.Security.Cryptography;
using System.Text;

namespace CampRegistrationApp.SeleniumTests.Infrastructure;

public static class TestDataSeeder
{
    public static void Seed(ApplicationDbContext db)
    {
        if (db.Sectors.Any()) return;

        var sectors = new List<Sector>
        {
            new() { Name = "A", Camp = "مخيم السلام", Coordinate = "31.5,34.5", Area = "شمالي", ManufacturedTentsCount = 100, HandmadeTentsCount = 50, BathroomsCount = 20 },
            new() { Name = "B", Camp = "مخيم السلام", Coordinate = "31.6,34.6", Area = "جنوبي", ManufacturedTentsCount = 80, HandmadeTentsCount = 40, BathroomsCount = 15 },
            new() { Name = "C", Camp = "مخيم السلام", Coordinate = "31.7,34.7", Area = "شرقي", ManufacturedTentsCount = 120, HandmadeTentsCount = 60, BathroomsCount = 25 },
            new() { Name = "D", Camp = "مخيم السلام", Coordinate = "31.8,34.8", Area = "غربي", ManufacturedTentsCount = 90, HandmadeTentsCount = 45, BathroomsCount = 18 },
        };
        db.Sectors.AddRange(sectors);
        db.SaveChanges();

        db.HealthStatuses.AddRange(
            new HealthStatus { Name = "سليم" },
            new HealthStatus { Name = "مريض" }
        );
        db.SaveChanges();

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
        db.SaveChanges();

        db.DisabilityTypes.AddRange(
            new DisabilityType { Name = "حركية" },
            new DisabilityType { Name = "سمعية" },
            new DisabilityType { Name = "بصرية" },
            new DisabilityType { Name = "إصابة حرب" }
        );
        db.SaveChanges();

        var desires = new[] { "خيم", "اغطية", "فرشات", "ادوات مطبخ", "شوادر", "ملابس", "طرد صحي" };
        foreach (var name in desires)
            db.Desires.Add(new Desire { Name = name });
        db.SaveChanges();

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
}
