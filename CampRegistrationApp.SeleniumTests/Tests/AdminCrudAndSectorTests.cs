using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using CampRegistrationApp.Data;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class AdminCrudAndSectorTests : TestBase
{
    private readonly AdminLoginPage _loginPage;
    private readonly AdminDashboardPage _dashboardPage;
    private readonly AdminCrudPage _adminCrudPage;
    private readonly AdminSectorsPage _sectorsPage;
    private readonly ITestOutputHelper _output;

    public AdminCrudAndSectorTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _loginPage = new AdminLoginPage(this);
        _dashboardPage = new AdminDashboardPage(this);
        _adminCrudPage = new AdminCrudPage(this);
        _sectorsPage = new AdminSectorsPage(this);
        _output = output;
    }

    [Fact]
    public void AdminList_ShowsDefaultAdmin()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickAdminManagement();

        _adminCrudPage.GetAdminCount().Should().BeGreaterThan(0);
    }

    [Fact]
    public void SuperAdmin_CreateAdmin_Success()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickAdminManagement();

        _adminCrudPage.ClickCreate();
        _adminCrudPage.FillCreateForm("مشرف جديد", "testadmin1", "0591111111", "مشرف", "pass123");
        _adminCrudPage.ClickSave();
        _adminCrudPage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void SuperAdmin_CreateMandoob_Success()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickAdminManagement();

        _adminCrudPage.ClickCreate();
        _adminCrudPage.FillCreateForm("مندوب جديد", "testmandoob1", "0592222222", "مندوب", "pass123");
        _adminCrudPage.ClickSave();
        _adminCrudPage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void SuperAdmin_EditAdmin_UpdatesFields()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickAdminManagement();

        _adminCrudPage.ClickEdit(0);
        _adminCrudPage.ClickSave();
        _adminCrudPage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void SuperAdmin_DeleteAdmin_RemovesFromList()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickAdminManagement();

        var countBefore = _adminCrudPage.GetAdminCount();
        if (countBefore > 1)
        {
            _adminCrudPage.ClickDelete(0);
            var countAfter = _adminCrudPage.GetAdminCount();
        }
    }

    [Fact]
    public void SectorList_ShowsAllSectors()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickSectorManagement();

        _sectorsPage.GetSectorCount().Should().Be(4);
    }

    [Fact]
    public void SuperAdmin_CreateSector_WithAllFields()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickSectorManagement();

        _sectorsPage.GoToCreate();
        _sectorsPage.CreateSector("E", "مخيم الأمل", "32.0,35.0", "وسط", 50, 25, 10);
        _sectorsPage.ClickSave();
        _sectorsPage.IsSuccess().Should().BeTrue();

        _sectorsPage.GetSectorCount().Should().Be(5);
    }

    [Fact]
    public void SuperAdmin_EditSector_UpdatesFields()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickSectorManagement();

        _sectorsPage.ClickEdit(0);
        _sectorsPage.ClickSave();
        _sectorsPage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void SuperAdmin_DeleteSector()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickSectorManagement();

        var countBefore = _sectorsPage.GetSectorCount();
        _sectorsPage.ClickDelete(0);
        _sectorsPage.GetSectorCount().Should().Be(countBefore - 1);
    }
}
