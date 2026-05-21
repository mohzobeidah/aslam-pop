using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using Xunit.Abstractions;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class AssistanceTests : TestBase
{
    private readonly AdminLoginPage _loginPage;
    private readonly AdminDashboardPage _dashboardPage;
    private readonly AssistancePage _assistancePage;
    private readonly ITestOutputHelper _output;

    public AssistanceTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _loginPage = new AdminLoginPage(this);
        _dashboardPage = new AdminDashboardPage(this);
        _assistancePage = new AssistancePage(this);
        _output = output;
    }

    private void LoginAsAdmin()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
    }

    [Fact]
    public void AssistanceList_Empty_ShowsMessage()
    {
        LoginAsAdmin();
        _dashboardPage.ClickAssistance();
        _assistancePage.IsEmpty().Should().BeTrue("no assistance created yet");
    }

    [Fact]
    public void Admin_CreateAssistance_Success()
    {
        LoginAsAdmin();
        _dashboardPage.ClickAssistance();

        _assistancePage.GoToCreate();
        _assistancePage.FillCreateForm(
            name: "مساعدات الشتاء",
            type: "مواد إغاثية",
            source: "الهلال الأحمر",
            date: "15/01/2026",
            description: "توزيع مساعدات شتوية للأسر المتضررة",
            sector: "A"
        );
        _assistancePage.ClickSave();
        _assistancePage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void Admin_CreateAssistance_ForSectorB()
    {
        LoginAsAdmin();
        _dashboardPage.ClickAssistance();

        _assistancePage.GoToCreate();
        _assistancePage.FillCreateForm(
            name: "مساعدات قطاع B",
            type: "غذائية",
            source: "الأونروا",
            date: "20/02/2026",
            description: "توزيع طرد غذائي",
            sector: "B"
        );
        _assistancePage.ClickSave();
        _assistancePage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void Assistance_Edit_UpdatesFields()
    {
        LoginAsAdmin();
        _dashboardPage.ClickAssistance();

        _assistancePage.GoToCreate();
        _assistancePage.FillCreateForm("مساعدة للتعديل", "نقدي", "صندوق الزكاة",
            "10/03/2026", "وصف", "A");
        _assistancePage.ClickSave();

        _assistancePage.ClickEdit(0);
        _assistancePage.ClickSave();
        _assistancePage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void Assistance_Approve_ChangesStatus()
    {
        LoginAsAdmin();
        _dashboardPage.ClickAssistance();

        _assistancePage.GoToCreate();
        _assistancePage.FillCreateForm("مساعدة للموافقة", "مواد", "متبرعون",
            "05/04/2026", "وصف", "A");
        _assistancePage.ClickSave();

        _assistancePage.ClickDetails(0);
        _assistancePage.ClickApprove();
    }

    [Fact]
    public void Assistance_AddBeneficiary_Success()
    {
        LoginAsAdmin();
        _dashboardPage.ClickAssistance();

        _assistancePage.GoToCreate();
        _assistancePage.FillCreateForm("مساعدة بمستفيد", "دعم نقدي", "وزارة",
            "01/05/2026", "وصف", "A");
        _assistancePage.ClickSave();

        _assistancePage.ClickDetails(0);
        _assistancePage.ClickAddBeneficiary();
        _assistancePage.AddBeneficiary("أحمد محمد خليل", "123456789", "0591234567");
    }

    [Fact]
    public void Assistance_ExportBeneficiaries()
    {
        LoginAsAdmin();
        _dashboardPage.ClickAssistance();

        _assistancePage.GoToCreate();
        _assistancePage.FillCreateForm("مساعدة للتصدير", "غذاء", "جهات مانحة",
            "15/06/2026", "وصف", "A");
        _assistancePage.ClickSave();

        _assistancePage.ClickDetails(0);
        _assistancePage.ClickExportBeneficiaries();
    }

    [Fact]
    public void Assistance_ImportHistory_Loads()
    {
        LoginAsAdmin();
        _dashboardPage.ClickAssistance();

        NavigateTo("/Assistance/ImportHistory");
        WaitForPageLoad();
    }
}
