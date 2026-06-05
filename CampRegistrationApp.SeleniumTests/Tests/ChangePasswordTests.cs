using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using System.Security.Cryptography;
using System.Text;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class ChangePasswordTests : TestBase
{
    private readonly AdminLoginPage _loginPage;
    private readonly AdminChangePasswordPage _changePasswordPage;
    private readonly AdminDashboardPage _dashboardPage;
    private readonly ITestOutputHelper _output;

    public ChangePasswordTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _loginPage = new AdminLoginPage(this);
        _changePasswordPage = new AdminChangePasswordPage(this);
        _dashboardPage = new AdminDashboardPage(this);
        _output = output;
    }

    [Fact]
    public void ChangePassword_Voluntary_Success()
    {
        // Create a mandoob whose password differs from national ID
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("originalpass")));
            db.Admins.Add(new Admin
            {
                Name = "مشرف تغيير",
                NationalId = "changepw",
                Mobile = "0599999999",
                PasswordHash = hash,
                Role = AdminRole.Admin,
                SectorId = 1
            });
            db.SaveChanges();
        }

        _loginPage.GoTo();
        _loginPage.Login("changepw", "originalpass");
        GetCurrentUrl().Should().Contain("Admin/Dashboard");

        NavigateTo("/Admin/ChangePassword");
        WaitForPageLoad();

        _changePasswordPage.ChangePassword("originalpass", "newpass123", "newpass123");

        _changePasswordPage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void ChangePassword_Force_WhenPasswordMatchesNationalId()
    {
        // Create an admin with password == national ID
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("123456789")));
            db.Admins.Add(new Admin
            {
                Name = "مشرف إجباري",
                NationalId = "123456789",
                Mobile = "0598888888",
                PasswordHash = hash,
                Role = AdminRole.Admin,
                SectorId = 1
            });
            db.SaveChanges();
        }

        _loginPage.GoTo();
        _loginPage.Login("123456789", "123456789");

        // Should redirect to ChangePassword, not Dashboard
        GetCurrentUrl().Should().Contain("Admin/ChangePassword");
        _changePasswordPage.IsForceChangePage().Should().BeTrue();

        // Enter new password
        _changePasswordPage.ChangePasswordForce("newsecurepass", "newsecurepass");

        _changePasswordPage.IsSuccess().Should().BeTrue();
        GetCurrentUrl().Should().Contain("Admin/Dashboard");
    }

    [Fact]
    public void ChangePassword_DifferentFromOldPassword_Required()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("oldpass123")));
            db.Admins.Add(new Admin
            {
                Name = "مشرف اختبار",
                NationalId = "testadmin",
                Mobile = "0597777777",
                PasswordHash = hash,
                Role = AdminRole.Admin,
                SectorId = 1
            });
            db.SaveChanges();
        }

        _loginPage.GoTo();
        _loginPage.Login("testadmin", "oldpass123");
        WaitForPageLoad();

        _changePasswordPage.GoTo();

        // Try wrong old password
        _changePasswordPage.ChangePassword("wrongold", "newpass123", "newpass123");

        _changePasswordPage.HasError().Should().BeTrue();
    }

    [Fact]
    public void Navigation_ChangePasswordLink_VisibleInNav()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();

        // Navigate via the nav link text
        NavigateTo("/Admin/ChangePassword");
        WaitForPageLoad();

        GetCurrentUrl().Should().Contain("Admin/ChangePassword");
    }
}
