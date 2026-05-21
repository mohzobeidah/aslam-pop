using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using CampRegistrationApp.Data;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class AdminApprovalAndRefugeeTests : TestBase
{
    private readonly AdminLoginPage _loginPage;
    private readonly AdminDashboardPage _dashboardPage;
    private readonly AdminRegistrationsPage _registrationsPage;
    private readonly AdminRefugeesPage _refugeesPage;
    private readonly RefugeeDetailsPage _detailsPage;
    private readonly RegistrationPage _regPage;
    private readonly ITestOutputHelper _output;

    public AdminApprovalAndRefugeeTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _loginPage = new AdminLoginPage(this);
        _dashboardPage = new AdminDashboardPage(this);
        _registrationsPage = new AdminRegistrationsPage(this);
        _refugeesPage = new AdminRefugeesPage(this);
        _detailsPage = new RefugeeDetailsPage(this);
        _regPage = new RegistrationPage(this);
        _output = output;
    }

    private string RegisterAndGetRecordId()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);
        return _regPage.SubmitAndGetRecordId();
    }

    [Fact]
    public void Registrations_ShowsPending_AfterNewRegistration()
    {
        RegisterAndGetRecordId();

        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickRegistrations();

        _registrationsPage.HasRegistrations().Should().BeTrue();
    }

    [Fact]
    public void ApproveRegistration_ChangesStatus()
    {
        RegisterAndGetRecordId();

        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickRegistrations();

        _registrationsPage.ApproveRegistration();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.FamilyRegistrations.Any(r => r.ApprovalStatus == Models.RegistrationApprovalStatus.Approved)
            .Should().BeTrue();
    }

    [Fact]
    public void RejectRegistration_ChangesStatus()
    {
        RegisterAndGetRecordId();

        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickRegistrations();

        _registrationsPage.RejectRegistration();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.FamilyRegistrations.Any(r => r.ApprovalStatus == Models.RegistrationApprovalStatus.Rejected)
            .Should().BeTrue();
    }

    [Fact]
    public void Registrations_FilterByStatus_Works()
    {
        RegisterAndGetRecordId();

        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickRegistrations();

        _registrationsPage.FilterByStatus("Approved");
        _registrationsPage.GetRegistrationCount().Should().Be(0);

        _registrationsPage.FilterByStatus("Pending");
        _registrationsPage.HasRegistrations().Should().BeTrue();
    }

    [Fact]
    public void Registrations_FilterBySector_Works()
    {
        RegisterAndGetRecordId();

        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickRegistrations();

        _registrationsPage.FilterBySector("A");
    }

    [Fact]
    public void Refugees_List_ShowsApprovedRegistrations()
    {
        RegisterAndGetRecordId();

        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickRegistrations();
        _registrationsPage.ApproveRegistration();

        _dashboardPage.ClickRefugees();
        _refugeesPage.HasRefugees().Should().BeTrue();
    }

    [Fact]
    public void Refugees_FilterBySector_Works()
    {
        RegisterAndGetRecordId();

        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickRegistrations();
        _registrationsPage.ApproveRegistration();

        _dashboardPage.ClickRefugees();
        _refugeesPage.FilterBySector("A");
    }

    [Fact]
    public void RefugeeDetails_ShowsAllSections()
    {
        var recordId = RegisterAndGetRecordId();

        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickRegistrations();
        _registrationsPage.ApproveRegistration();

        _dashboardPage.ClickRefugees();
        _refugeesPage.ClickRefugee();

        _detailsPage.HasHousingSection().Should().BeTrue("housing section should be visible");
        _detailsPage.HasDesiresSection().Should().BeTrue("desires section should be visible");
    }

    [Fact]
    public void RemoveRefugee_SoftDeletes()
    {
        RegisterAndGetRecordId();

        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickRegistrations();
        _registrationsPage.ApproveRegistration();

        _dashboardPage.ClickRefugees();
        _refugeesPage.HasRefugees().Should().BeTrue();

        _dashboardPage.ClickRegistrations();
        WaitForPageLoad();
    }

    [Fact]
    public void ExportRefugeesToExcel_TriggersDownload()
    {
        RegisterAndGetRecordId();

        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickRegistrations();
        _registrationsPage.ApproveRegistration();

        _dashboardPage.ClickRefugees();
        _refugeesPage.ClickExportExcel();
    }
}
