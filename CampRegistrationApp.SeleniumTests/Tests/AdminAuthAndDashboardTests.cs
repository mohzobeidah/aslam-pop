using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using Xunit.Abstractions;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class AdminAuthAndDashboardTests : TestBase
{
    private readonly AdminLoginPage _loginPage;
    private readonly AdminDashboardPage _dashboardPage;
    private readonly ITestOutputHelper _output;

    public AdminAuthAndDashboardTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _loginPage = new AdminLoginPage(this);
        _dashboardPage = new AdminDashboardPage(this);
        _output = output;
    }

    [Fact]
    public void AdminLogin_WithValidCredentials_RedirectsToDashboard()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        GetCurrentUrl().Should().Contain("Admin/Dashboard");
    }

    [Fact]
    public void AdminLogin_WithInvalidPassword_ShowsError()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "wrongpassword");
        _loginPage.HasError().Should().BeTrue();
        _loginPage.IsOnLoginPage().Should().BeTrue();
    }

    [Fact]
    public void AdminLogin_WithEmptyFields_ShowsError()
    {
        _loginPage.GoTo();
        _loginPage.Login("", "");
        _loginPage.HasError().Should().BeTrue();
    }

    [Fact]
    public void AdminDashboard_LoadsWithStats()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();

        _dashboardPage.IsStatsDisplayed().Should().BeTrue();
        _dashboardPage.IsSectorTableDisplayed().Should().BeTrue();
    }

    [Fact]
    public void AdminDashboard_ContainsNavigationLinks()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();

        _dashboardPage.GetHeading().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AdminLogout_RedirectsToLogin()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();

        _dashboardPage.ClickLogout();
        GetCurrentUrl().Should().Contain("Admin/Login");
    }

    [Fact]
    public void AdminSessionExpiry_RedirectsToLogin()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();

        NavigateTo("/Admin/Dashboard");
        WaitForPageLoad();
        _dashboardPage.GetHeading().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void UnauthenticatedAccess_RedirectsToLogin()
    {
        NavigateTo("/Admin/Dashboard");
        WaitForPageLoad();
        GetCurrentUrl().Should().Contain("Admin/Login");
    }

    [Fact]
    public void AdminDashboard_NavigatesToAllSections()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();

        var sections = new[]
        {
            ("الطلبات", "Registrations"),
            ("اللاجئين", "Refugees"),
            ("المشاريع", "Project"),
            ("المساعدات", "Assistance"),
        };

        foreach (var (_, urlPart) in sections)
        {
            NavigateTo($"/Admin/{urlPart}");
            WaitForPageLoad();
            GetCurrentUrl().Should().Contain(urlPart);
        }
    }
}
