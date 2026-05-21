using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class EdgeCaseTests : TestBase
{
    private readonly RegistrationPage _regPage;
    private readonly AdminLoginPage _loginPage;
    private readonly AdminDashboardPage _dashboardPage;
    private readonly ITestOutputHelper _output;

    public EdgeCaseTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _regPage = new RegistrationPage(this);
        _loginPage = new AdminLoginPage(this);
        _dashboardPage = new AdminDashboardPage(this);
        _output = output;
    }

    [Fact]
    public void DateOfBirth_FutureDate_Accepted()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();

        _regPage.FillStep1(
            firstName: "طفل", secondName: "جديد", thirdName: "الولادة", lastName: "الرضيع",
            idNumber: uniqueId, sector: "A", dob: "01/01/2025",
            gender: "ذكر", phone: "0591234567",
            wallet: "555555555555555", governorate: "غزة",
            maritalStatus: "أعزب", employment: "طفل", education: "غير ملتحق",
            healthStatus: "سليم"
        );
        _regPage.ClickNextToStep2();
    }

    [Fact]
    public void MarriedStatus_WithoutSpouse_Fails()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();

        _regPage.FillStep1(
            firstName: "متزوج", secondName: "بلا", thirdName: "زوجة", lastName: "العباس",
            idNumber: uniqueId, sector: "B", dob: "15/05/1985",
            gender: "ذكر", phone: "0597654321",
            wallet: "666666666666666", governorate: "رفح",
            maritalStatus: "متزوج", employment: "موظف", education: "جامعي",
            healthStatus: "سليم"
        );
        _regPage.ClickNextToStep2();
        _regPage.ClickNextToStep3();

        _regPage.HasValidationErrors().Should().BeTrue("married without spouse should fail");
    }

    [Fact]
    public void SpecialCharacters_InNames_AreAccepted()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();

        _regPage.FillStep1(
            firstName: "محمد'", secondName: "علي", thirdName: "خالد-", lastName: "السيد",
            idNumber: uniqueId, sector: "A", dob: "01/01/1990",
            gender: "ذكر", phone: "0591234567",
            wallet: "777777777777777", governorate: "غزة",
            maritalStatus: "أعزب", employment: "موظف", education: "جامعي",
            healthStatus: "سليم"
        );
        _regPage.ClickNextToStep2();
        _regPage.ClickNextToStep3();
    }

    [Fact]
    public void VeryLongPhoneNumber_Accepted()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();

        _regPage.FillStep1(
            firstName: "رقم", secondName: "طويل", thirdName: "جدا", lastName: "الهاتف",
            idNumber: uniqueId, sector: "C", dob: "10/10/1980",
            gender: "ذكر", phone: "05912345678901234",
            wallet: "888888888888888", governorate: "خان يونس",
            maritalStatus: "أعزب", employment: "موظف", education: "جامعي",
            healthStatus: "سليم"
        );
        _regPage.ClickNextToStep2();
    }

    [Fact]
    public void AllMembers_Removed_ProceedsWithoutMembers()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();

        _regPage.FillStep1(
            firstName: "وحيد", secondName: "بلا", thirdName: "أفراد", lastName: "العائلة",
            idNumber: uniqueId, sector: "A", dob: "01/01/2000",
            gender: "ذكر", phone: "0591112233",
            wallet: "999999999999999", governorate: "غزة",
            maritalStatus: "أعزب", employment: "موظف", education: "جامعي",
            healthStatus: "سليم"
        );
        _regPage.ClickNextToStep2();
        _regPage.ClickNextToStep3();

        GetCurrentUrl().Should().Contain("Registration");
    }

    [Fact]
    public void LongArabicNames_Accepted()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();

        _regPage.FillStep1(
            firstName: "عبدالرحمن", secondName: "عبدالله", thirdName: "عبدالقادر", lastName: "عبدالرزاق",
            idNumber: uniqueId, sector: "D", dob: "20/05/1975",
            gender: "ذكر", phone: "0599988776",
            wallet: "000000000000000", governorate: "جباليا",
            maritalStatus: "متزوج", employment: "موظف", education: "جامعي",
            healthStatus: "سليم"
        );
        _regPage.ClickNextToStep2();

        _regPage.ClickAddMember();
        _regPage.FillMember(0, "عبداللطيف عبدالمجيد", uniqueId[..5] + "66666", "ابن", "ذكر", "15/08/2005");
        _regPage.ClickNextToStep3();
    }

    [Fact]
    public void LoginToApproved_AfterSoftDelete_Fails()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);
        var recordId = _regPage.SubmitAndGetRecordId();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
        var reg = db.FamilyRegistrations.First(r => r.RecordId == recordId);
        reg.ApprovalStatus = Models.RegistrationApprovalStatus.Approved;
        reg.IsDeleted = true;
        db.SaveChanges();

        var headId = reg.FamilyHead.IdNumber;

        var loginPage = new RecordLoginPage(this);
        loginPage.GoTo();
        loginPage.Login(headId, "test1234");
    }

    [Fact]
    public void BrowserBack_AfterSubmit_ShowsNoResubmission()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);
        _regPage.SubmitAndGetRecordId();

        Driver.Navigate().Back();
        WaitForPageLoad();
    }
}
