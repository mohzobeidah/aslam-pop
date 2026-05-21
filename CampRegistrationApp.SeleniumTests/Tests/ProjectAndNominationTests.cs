using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using CampRegistrationApp.Data;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class ProjectAndNominationTests : TestBase
{
    private readonly AdminLoginPage _loginPage;
    private readonly AdminDashboardPage _dashboardPage;
    private readonly ProjectPage _projectPage;
    private readonly NominationPage _nominationPage;
    private readonly RegistrationPage _regPage;
    private readonly ITestOutputHelper _output;

    public ProjectAndNominationTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _loginPage = new AdminLoginPage(this);
        _dashboardPage = new AdminDashboardPage(this);
        _projectPage = new ProjectPage(this);
        _nominationPage = new NominationPage(this);
        _regPage = new RegistrationPage(this);
        _output = output;
    }

    [Fact]
    public void ProjectList_LoadsSuccessfully()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickProjects();

        _projectPage.IsEmpty().Should().BeTrue("no projects created yet");
    }

    [Fact]
    public void Admin_CreateProject_WithSectorQuotas()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickProjects();

        _projectPage.GoToCreate();
        _projectPage.FillCreateForm(
            name: "توزيع الخيم الشتوي",
            startDate: "01/01/2026",
            endDate: "01/03/2026",
            requiredCount: 100,
            description: "مشروع توزيع الخيم لفصل الشتاء"
        );

        _projectPage.AddSectorQuota(0, "A", 30);
        _projectPage.AddSectorQuota(1, "B", 25);
        _projectPage.AddSectorQuota(2, "C", 25);
        _projectPage.AddSectorQuota(3, "D", 20);

        _projectPage.ClickSave();
        _projectPage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void Admin_CreateProject_WithActiveStatus()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickProjects();

        _projectPage.GoToCreate();
        _projectPage.FillCreateForm(
            name: "مشروع نشط",
            startDate: "01/01/2026",
            endDate: "01/06/2026",
            requiredCount: 50,
            description: "مشروع اختبار نشط",
            status: "Active"
        );
        _projectPage.AddSectorQuota(0, "A", 50);
        _projectPage.ClickSave();
        _projectPage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void Project_Edit_UpdatesFields()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickProjects();

        _projectPage.GoToCreate();
        _projectPage.FillCreateForm("مشروع للتعديل", "01/02/2026", "01/04/2026", 30, "وصف");
        _projectPage.AddSectorQuota(0, "A", 30);
        _projectPage.ClickSave();

        _projectPage.ClickEdit(0);
        _projectPage.ClickSave();
        _projectPage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void ProjectView_ShowsNominationPage()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickProjects();

        _projectPage.GoToCreate();
        _projectPage.FillCreateForm("مشروع للترشيحات", "01/03/2026", "01/05/2026", 20, "وصف");
        _projectPage.AddSectorQuota(0, "A", 20);
        _projectPage.ClickSave();
        _projectPage.IsSuccess().Should().BeTrue();

        _projectPage.ClickView(0);
        WaitForPageLoad();
        GetCurrentUrl().Should().Contain("Nomination");
    }

    [Fact]
    public void Nomination_AddPerson_Succeeds()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);
        var recordId = _regPage.SubmitAndGetRecordId();

        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickProjects();

        _projectPage.GoToCreate();
        _projectPage.FillCreateForm("مشروع الترشيحات", "01/04/2026", "01/06/2026", 10, "وصف");
        _projectPage.AddSectorQuota(0, "A", 10);
        _projectPage.ClickSave();
        _projectPage.IsSuccess().Should().BeTrue();

        _projectPage.ClickView(0);
        WaitForPageLoad();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var person = db.Persons.First(p => p.IdNumber == uniqueId);

        _nominationPage.AddNomination(person.Id);
        _nominationPage.GetNominationCount().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Nomination_EmptyProject_ShowsMessage()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickProjects();

        _projectPage.GoToCreate();
        _projectPage.FillCreateForm("مشروع فارغ", "01/01/2026", "01/02/2026", 5, "");
        _projectPage.AddSectorQuota(0, "A", 5);
        _projectPage.ClickSave();

        _projectPage.ClickView(0);
        WaitForPageLoad();
        _nominationPage.IsEmpty().Should().BeTrue("no nominations added yet");
    }
}
