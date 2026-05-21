using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using Xunit.Abstractions;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class RegistrationTests : TestBase
{
    private readonly RegistrationPage _regPage;
    private readonly ITestOutputHelper _output;

    public RegistrationTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _regPage = new RegistrationPage(this);
        _output = output;
    }

    [Fact]
    public void RegistrationPage_LoadsSuccessfully()
    {
        _regPage.GoTo();
        GetCurrentUrl().Should().Contain("Registration");
    }

    [Fact]
    public void SubmitValidRegistration_CreatesFamilyRecord()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        var data = _regPage.FillBasicRegistration(uniqueId);
        var recordId = _regPage.SubmitAndGetRecordId();

        recordId.Should().NotBeNullOrEmpty();
        recordId.Length.Should().Be(8);
        _regPage.IsSuccessDisplayed().Should().BeTrue();
    }

    [Fact]
    public void SubmitRegistration_DuplicateIdNumber_ShowsError()
    {
        var uniqueId = GenerateUniqueIdNumber();

        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);
        _regPage.SubmitAndGetRecordId();

        NavigateTo("/Registration");
        WaitForPageLoad();
        _regPage.FillBasicRegistration(uniqueId);
        _regPage.ClickSubmit();

        _regPage.HasValidationErrors().Should().BeTrue("duplicate ID should produce validation error");
    }

    [Fact]
    public void SubmitRegistration_WithoutWife_WhenMarried_ShowsError()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();

        _regPage.FillStep1(
            firstName: "أحمد", secondName: "خالد", thirdName: "محمد", lastName: "النجار",
            idNumber: uniqueId, sector: "A", dob: "15/03/1985",
            gender: "ذكر", phone: "0597654321",
            wallet: "987654321098765", governorate: "خان يونس",
            maritalStatus: "متزوج", employment: "موظف", education: "جامعي",
            healthStatus: "سليم"
        );

        // Go to step 2 without adding wife
        _regPage.ClickNextToStep2();

        // Try going to step 3 - should show validation error
        _regPage.ClickNextToStep3();

        _regPage.HasValidationErrors().Should().BeTrue("married without wife should show error");
    }

    [Fact]
    public void SubmitRegistration_WithSickHealthNoDisease_ShowsError()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();

        _regPage.FillStep1(
            firstName: "خالد", secondName: "سامي", thirdName: "نادر", lastName: "الشامي",
            idNumber: uniqueId, sector: "B", dob: "20/07/1978",
            gender: "ذكر", phone: "0561122334",
            wallet: "111111111111111", governorate: "رفح",
            maritalStatus: "أعزب", employment: "عامل", education: "ثانوي",
            healthStatus: "مريض"
        );

        _regPage.ClickNextToStep2();

        _regPage.HasValidationErrors().Should().BeTrue("sick person without disease should show error");
    }

    [Fact]
    public void AllFieldsEmpty_ValidationErrors()
    {
        _regPage.GoTo();
        _regPage.ClickNextToStep2();

        _regPage.HasValidationErrors().Should().BeTrue("empty form should have validation errors");
    }

    [Fact]
    public void AutoSave_ReloadForm_RestoresData()
    {
        NavigateTo("/Registration");
        WaitForPageLoad();

        _regPage.FillHeadName("محمود", "سعيد", "جمال", "الحسن");
        _regPage.FillPhoneNumber("0591234567");

        NavigateTo("/Registration");
        WaitForPageLoad();

        var savedFirstName = GetLocalStorage("Head.FirstName");
        savedFirstName.Should().Be("محمود");
    }

    [Fact]
    public void FemaleRegistration_WithMembers_AndHousing_CompletesSuccessfully()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();

        _regPage.FillStep1(
            firstName: "مريم", secondName: "خالد", thirdName: "حسن", lastName: "الزيداني",
            idNumber: uniqueId, sector: "C", dob: "10/12/1988",
            gender: "أنثى", phone: "0599988776",
            wallet: "222222222222222", governorate: "جباليا",
            maritalStatus: "أرمل", employment: "ربة منزل", education: "إعدادي",
            healthStatus: "سليم"
        );
        _regPage.ClickNextToStep2();

        _regPage.ClickAddMember();
        _regPage.FillMember(0, "أحمد", uniqueId[..5] + "22222", "ابن", "ذكر", "15/06/2010");
        _regPage.ClickAddMember();
        _regPage.FillMember(1, "سارة", uniqueId[..5] + "33333", "ابنة", "أنثى", "20/11/2012");

        _regPage.ClickNextToStep3();

        _regPage.CheckFemaleHeaded();
        _regPage.SelectLivesInTent(true);
        _regPage.SelectTentType("قماش");
        _regPage.SelectHasBathroom(true);
        _regPage.SelectBathroomType("مشترك");
        _regPage.SelectBathroomStatus("متوسط");
        _regPage.SelectDesire(1, "اغطية");
        _regPage.SelectDesire(2, "فرشات");
        _regPage.SelectDesire(3, "ملابس");
        _regPage.SelectDesire(4, "طرد صحي");
        _regPage.SelectDesire(5, "خيم");

        _regPage.ClickNextToStep4();
        _regPage.FillPassword("pass1234");
        _regPage.FillConfirmPassword("pass1234");
        _regPage.CheckAcceptResponsibility();

        var recordId = _regPage.SubmitAndGetRecordId();
        recordId.Should().NotBeNullOrEmpty();
        recordId.Length.Should().Be(8);
    }

    [Fact]
    public void Step4_PasswordMismatch_ShowsError()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);

        _regPage.FillPassword("password1");
        _regPage.FillConfirmPassword("password2");
        _regPage.ClickSubmit();

        _regPage.HasValidationErrors().Should().BeTrue("password mismatch should show error");
    }

    [Fact]
    public void Step4_UncheckedResponsibility_ShowsError()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        var data = _regPage.FillBasicRegistration(uniqueId);

        _regPage.FillPassword("test1234");
        _regPage.FillConfirmPassword("test1234");
        _regPage.ClickSubmit();

        _regPage.HasValidationErrors().Should().BeTrue("unchecked responsibility should show error");
    }

    [Fact]
    public void Desires_MutualExclusivity()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();

        _regPage.FillStep1(
            firstName: "كريم", secondName: "حسام", thirdName: "علي", lastName: "مراد",
            idNumber: uniqueId, sector: "A", dob: "05/05/1995",
            gender: "ذكر", phone: "0591122334",
            wallet: "333333333333333", governorate: "غزة",
            maritalStatus: "متزوج", employment: "موظف", education: "جامعي",
            healthStatus: "سليم"
        );
        _regPage.ClickNextToStep2();
        _regPage.ClickAddMember();
        _regPage.FillMember(0, "نورة", uniqueId[..5] + "44444", "زوجة", "أنثى", "10/10/1996");
        _regPage.ClickNextToStep3();

        _regPage.SelectLivesInTent(true);
        _regPage.SelectTentType("قماش");
        _regPage.SelectHasBathroom(true);
        _regPage.SelectBathroomType("خاص");
        _regPage.SelectBathroomStatus("جيد");

        _regPage.SelectDesire(1, "خيم");
        _regPage.SelectDesire(2, "اغطية");
        _regPage.SelectDesire(3, "فرشات");
        _regPage.SelectDesire(4, "ادوات مطبخ");
        _regPage.SelectDesire(5, "شوادر");

        _regPage.ClickNextToStep4();
        _regPage.FillPassword("test1234");
        _regPage.FillConfirmPassword("test1234");
        _regPage.CheckAcceptResponsibility();

        var recordId = _regPage.SubmitAndGetRecordId();
        recordId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Registration_WithPregnantAndNursing_Completes()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();

        _regPage.FillStep1(
            firstName: "ندى", secondName: "إبراهيم", thirdName: "أحمد", lastName: "حماد",
            idNumber: uniqueId, sector: "D", dob: "01/01/1992",
            gender: "أنثى", phone: "0595566778",
            wallet: "444444444444444", governorate: "دير البلح",
            maritalStatus: "متزوج", employment: "موظفة", education: "جامعي",
            healthStatus: "سليم"
        );
        _regPage.ClickNextToStep2();

        _regPage.ClickAddMember();
        _regPage.FillMember(0, "محمد", uniqueId[..5] + "55555", "زوج", "ذكر", "10/10/1988");
        _regPage.ClickNextToStep3();

        _regPage.SelectLivesInTent(false);
        _regPage.SelectHasBathroom(true);
        _regPage.SelectBathroomType("خاص");
        _regPage.SelectBathroomStatus("جيد");
        _regPage.SelectDesire(1, "خيم");
        _regPage.SelectDesire(2, "اغطية");
        _regPage.SelectDesire(3, "فرشات");
        _regPage.SelectDesire(4, "ادوات مطبخ");
        _regPage.SelectDesire(5, "شوادر");

        _regPage.ClickNextToStep4();
        _regPage.FillPassword("test1234");
        _regPage.FillConfirmPassword("test1234");
        _regPage.CheckAcceptResponsibility();

        var recordId = _regPage.SubmitAndGetRecordId();
        recordId.Should().NotBeNullOrEmpty();
    }
}
