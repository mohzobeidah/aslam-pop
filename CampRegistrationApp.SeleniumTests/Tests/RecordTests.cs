using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class RecordTests : TestBase
{
    private readonly RegistrationPage _regPage;
    private readonly RecordSearchPage _searchPage;
    private readonly RecordLoginPage _loginPage;
    private readonly RecordEditPage _editPage;
    private readonly ITestOutputHelper _output;

    public RecordTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _regPage = new RegistrationPage(this);
        _searchPage = new RecordSearchPage(this);
        _loginPage = new RecordLoginPage(this);
        _editPage = new RecordEditPage(this);
        _output = output;
    }

    private string CreateApprovedRegistration()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);

        // Need to approve via DB for login test
        var recordId = _regPage.SubmitAndGetRecordId();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
        var reg = db.FamilyRegistrations.First(r => r.RecordId == recordId);
        reg.ApprovalStatus = Models.RegistrationApprovalStatus.Approved;
        var admin = db.Admins.First();
        reg.ApprovedById = admin.Id;
        reg.ApprovedAt = DateTime.UtcNow;
        db.SaveChanges();

        return recordId;
    }

    [Fact]
    public void RecordSearch_LoadsSuccessfully()
    {
        _searchPage.GoTo();
        GetCurrentUrl().Should().Contain("Record/Search");
    }

    [Fact]
    public void RecordSearch_WithInvalidId_ShowsNotFound()
    {
        _searchPage.GoTo();
        _searchPage.Search("XXXXXXXX", "123456789");
        _searchPage.IsNotFound().Should().BeTrue();
    }

    [Fact]
    public void RecordLogin_WithApprovedRecord_RedirectsToEdit()
    {
        var recordId = CreateApprovedRegistration();

        _loginPage.GoTo();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
        var reg = db.FamilyRegistrations.First(r => r.RecordId == recordId);

        _loginPage.Login(reg.FamilyHead.IdNumber, "test1234");
        GetCurrentUrl().Should().Contain("Record/Edit");
    }

    [Fact]
    public void RecordLogin_WithWrongPassword_ShowsError()
    {
        var recordId = CreateApprovedRegistration();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
        var reg = db.FamilyRegistrations.First(r => r.RecordId == recordId);

        _loginPage.GoTo();
        _loginPage.Login(reg.FamilyHead.IdNumber, "wrongpassword");
        _loginPage.HasError().Should().BeTrue();
    }

    [Fact]
    public void RecordLogin_EmptyFields_ShowsError()
    {
        _loginPage.GoTo();
        _loginPage.Login("", "");
        _loginPage.HasError().Should().BeTrue();
    }

    [Fact]
    public void RecordEdit_SavesChanges()
    {
        var recordId = CreateApprovedRegistration();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
        var reg = db.FamilyRegistrations.First(r => r.RecordId == recordId);

        _loginPage.GoTo();
        _loginPage.Login(reg.FamilyHead.IdNumber, "test1234");
        WaitForPageLoad();

        _editPage.UpdateFirstName("محمود");
        _editPage.ClickUpdate();
        _editPage.IsSuccess().Should().BeTrue();
    }

    [Fact]
    public void RecordLogout_ClearsSession_RedirectsToSearch()
    {
        var recordId = CreateApprovedRegistration();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
        var reg = db.FamilyRegistrations.First(r => r.RecordId == recordId);

        _loginPage.GoTo();
        _loginPage.Login(reg.FamilyHead.IdNumber, "test1234");
        WaitForPageLoad();

        _loginPage.ClickLogout();
        var url = GetCurrentUrl();
        (url.Contains("Record/Search") || url == BaseUrl + "/").Should().BeTrue();
    }
}
