using OpenQA.Selenium;

namespace CampRegistrationApp.SeleniumTests.PageObjects;

public class RegistrationPage : BasePage
{
    public RegistrationPage(Infrastructure.TestBase test) : base(test) { }

    // Step navigation
    private static By Step1Tab => By.CssSelector("[data-step='1'], #step-1-tab, .step-1");
    private static By Step2Tab => By.CssSelector("[data-step='2'], #step-2-tab, .step-2");
    private static By Step3Tab => By.CssSelector("[data-step='3'], #step-3-tab, .step-3");
    private static By Step4Tab => By.CssSelector("[data-step='4'], #step-4-tab, .step-4");
    private static By NextButton => By.CssSelector("button[type='button']:contains('التالي'), .btn-next, #nextBtn");
    private static By PrevButton => By.CssSelector("button[type='button']:contains('السابق'), .btn-prev, #prevBtn");
    private static By SubmitButton => By.CssSelector("button[type='submit'], #submitBtn, .btn-submit");

    // Step 1 - Family Head
    private static By FirstName => By.Id("Head_FirstName");
    private static By SecondName => By.Id("Head_SecondName");
    private static By ThirdName => By.Id("Head_ThirdName");
    private static By LastName => By.Id("Head_LastName");
    private static By IdNumber => By.Id("Head_IdNumber");
    private static By Sector => By.Id("Head_Sector");
    private static By DateOfBirth => By.Id("Head_DateOfBirth");
    private static By GenderMale => By.CssSelector("input[name='Head.Gender'][value='ذكر']");
    private static By GenderFemale => By.CssSelector("input[name='Head.Gender'][value='أنثى']");
    private static By PhoneNumber => By.Id("Head_PhoneNumber");
    private static By Wallet => By.Id("Head_Wallet");
    private static By OriginalGovernorate => By.Id("Head_OriginalGovernorate");
    private static By MaritalStatus => By.Id("Head_MaritalStatus");
    private static By EmploymentStatus => By.Id("Head_EmploymentStatus");
    private static By EducationLevel => By.Id("Head_EducationLevel");
    private static By HealthStatusHealthy => By.CssSelector("input[name='Head.HealthStatus'][value='سليم']");
    private static By HealthStatusSick => By.CssSelector("input[name='Head.HealthStatus'][value='مريض']");
    private static By HasInjuryYes => By.CssSelector("input[name='Head.HasInjury'][value='True']");
    private static By IsPrisoner => By.Id("Head_IsPrisoner");
    private static By HeadIdImageUpload => By.CssSelector("input[type='file'][accept*='image'], #headIdImage");
    private static By NextStep1Button => By.CssSelector("#step-1 .btn-next, .step1-next, #toStep2");

    // Step 2 - Family Members
    private static By AddMemberButton => By.CssSelector(".add-member, #addMember, button:contains('إضافة فرد')");
    private static By MemberFirstName(int index) => By.Id($"Members_{index}__FirstName");
    private static By MemberIdNumber(int index) => By.Id($"Members_{index}__IdNumber");
    private static By MemberRelationship(int index) => By.Id($"Members_{index}__RelationshipToHead");
    private static By MemberGender(int index) => By.Name($"Members[{index}].Gender");
    private static By MemberDateOfBirth(int index) => By.Id($"Members_{index}__DateOfBirth");
    private static By RemoveMemberButton(int index) => By.CssSelector($"#member-{index} .remove-member, .remove-member-{index}");
    private static By NextStep2Button => By.CssSelector("#step-2 .btn-next, .step2-next, #toStep3");

    // Step 3 - Housing & Special Cases
    private static By IsChildHeaded => By.Id("IsChildHeaded");
    private static By ChildHeadedDetails => By.Id("ChildHeadedDetails");
    private static By IsFemaleHeaded => By.Id("IsFemaleHeaded");
    private static By IsHusbandAbroad => By.Id("IsHusbandAbroad");
    private static By LivesInTentYes => By.CssSelector("input[name='LivesInTent'][value='True']");
    private static By LivesInTentNo => By.CssSelector("input[name='LivesInTent'][value='False']");
    private static By TentType => By.Id("TentType");
    private static By HasBathroomYes => By.CssSelector("input[name='HasBathroom'][value='True']");
    private static By BathroomType => By.Id("BathroomType");
    private static By BathroomStatus => By.Id("BathroomStatus");
    private static By DesireRank(int rank) => By.Id($"DesireIds_{rank - 1}");
    private static By NextStep3Button => By.CssSelector("#step-3 .btn-next, .step3-next, #toStep4");

    // Step 4 - Review & Confirm
    private static By Password => By.Id("Password");
    private static By ConfirmPassword => By.Id("ConfirmPassword");
    private static By AcceptResponsibility => By.Id("AcceptResponsibility");
    private static By StatusNotes => By.Id("StatusNotes");
    private static By FinalSubmitButton => By.CssSelector("button[type='submit'], #submitRegistration, .btn-submit");

    // Success
    private static By RecordIdDisplay => By.CssSelector(".record-id, #recordId, .success-record-id");
    private static By SuccessMessage => By.CssSelector(".success-message, .alert-success, h2:contains('تم')");

    // Validation errors
    private static By ValidationSummary => By.CssSelector(".validation-summary-errors, .text-danger");
    private static By FieldError(string fieldId) => By.CssSelector($"#{fieldId}.field-error, .field-validation-error[data-valmsg-for='{fieldId}']");

    public void GoTo()
    {
        NavigateTo("/Registration");
        Test.WaitForPageLoad();
    }

    // ===== Step 1 =====
    public void FillHeadName(string first, string second, string third, string last)
    {
        Test.Type(FirstName, first);
        Test.Type(SecondName, second);
        Test.Type(ThirdName, third);
        Test.Type(LastName, last);
    }

    public void FillHeadIdNumber(string id)
    {
        Test.Type(IdNumber, id);
    }

    public void SelectSector(string sector)
    {
        Test.SelectDropdown(Sector, sector);
    }

    public void FillDateOfBirth(string dob)
    {
        Test.Type(DateOfBirth, dob);
    }

    public void SelectGender(string gender)
    {
        if (gender == "ذكر")
            Test.Click(GenderMale);
        else
            Test.Click(GenderFemale);
    }

    public void FillPhoneNumber(string phone)
    {
        Test.Type(PhoneNumber, phone);
    }

    public void FillWallet(string wallet)
    {
        Test.Type(Wallet, wallet);
    }

    public void FillOriginalGovernorate(string gov)
    {
        Test.Type(OriginalGovernorate, gov);
    }

    public void SelectMaritalStatus(string status)
    {
        Test.SelectDropdown(MaritalStatus, status);
    }

    public void SelectEmploymentStatus(string status)
    {
        Test.SelectDropdown(EmploymentStatus, status);
    }

    public void SelectEducationLevel(string level)
    {
        Test.SelectDropdown(EducationLevel, level);
    }

    public void SelectHealthStatus(string status)
    {
        if (status == "سليم")
            Test.Click(HealthStatusHealthy);
        else
            Test.Click(HealthStatusSick);
    }

    public void ClickNextToStep2()
    {
        Test.Click(NextStep1Button);
        WaitForAjax();
    }

    public void FillStep1(string firstName, string secondName, string thirdName, string lastName,
        string idNumber, string sector, string dob, string gender, string phone,
        string wallet, string governorate, string maritalStatus, string employment, string education,
        string healthStatus = "سليم")
    {
        FillHeadName(firstName, secondName, thirdName, lastName);
        FillHeadIdNumber(idNumber);
        SelectSector(sector);
        FillDateOfBirth(dob);
        SelectGender(gender);
        FillPhoneNumber(phone);
        FillWallet(wallet);
        FillOriginalGovernorate(governorate);
        SelectMaritalStatus(maritalStatus);
        SelectEmploymentStatus(employment);
        SelectEducationLevel(education);
        SelectHealthStatus(healthStatus);
    }

    // ===== Step 2 =====
    public void ClickAddMember()
    {
        Test.Click(AddMemberButton);
        WaitForAjax();
    }

    public void FillMember(int index, string firstName, string idNumber, string relationship,
        string gender, string dob)
    {
        Test.Type(MemberFirstName(index), firstName);
        Test.Type(MemberIdNumber(index), idNumber);
        Test.SelectDropdown(MemberRelationship(index), relationship);
        if (gender == "ذكر")
            Test.Click(By.CssSelector($"input[name='Members[{index}].Gender'][value='ذكر']"));
        else
            Test.Click(By.CssSelector($"input[name='Members[{index}].Gender'][value='أنثى']"));
        Test.Type(MemberDateOfBirth(index), dob);
    }

    public void RemoveMember(int index)
    {
        Test.Click(RemoveMemberButton(index));
        WaitForAjax();
    }

    public void ClickNextToStep3()
    {
        Test.Click(NextStep2Button);
        WaitForAjax();
    }

    // ===== Step 3 =====
    public void CheckChildHeaded(string details = "")
    {
        Test.Click(IsChildHeaded);
        if (!string.IsNullOrEmpty(details))
            Test.Type(ChildHeadedDetails, details);
    }

    public void CheckFemaleHeaded()
    {
        Test.Click(IsFemaleHeaded);
    }

    public void CheckHusbandAbroad()
    {
        Test.Click(IsHusbandAbroad);
    }

    public void SelectLivesInTent(bool yes)
    {
        if (yes) Test.Click(LivesInTentYes);
        else Test.Click(LivesInTentNo);
    }

    public void SelectTentType(string type)
    {
        Test.SelectDropdown(TentType, type);
    }

    public void SelectHasBathroom(bool yes)
    {
        if (yes) Test.Click(HasBathroomYes);
    }

    public void SelectBathroomType(string type)
    {
        Test.SelectDropdown(BathroomType, type);
    }

    public void SelectBathroomStatus(string status)
    {
        Test.SelectDropdown(BathroomStatus, status);
    }

    public void SelectDesire(int rank, string desireName)
    {
        Test.SelectDropdown(DesireRank(rank), desireName);
    }

    public void ClickNextToStep4()
    {
        Test.Click(NextStep3Button);
        WaitForAjax();
    }

    // ===== Step 4 =====
    public void FillPassword(string password)
    {
        Test.Type(Password, password);
    }

    public void FillConfirmPassword(string password)
    {
        Test.Type(ConfirmPassword, password);
    }

    public void CheckAcceptResponsibility()
    {
        Test.Click(AcceptResponsibility);
    }

    public void FillStatusNotes(string notes)
    {
        Test.Type(StatusNotes, notes);
    }

    public void ClickSubmit()
    {
        Test.Click(FinalSubmitButton);
        Test.WaitForPageLoad();
    }

    // ===== Submit Full Registration =====
    public RegistrationData FillBasicRegistration(string uniqueId)
    {
        var data = new RegistrationData { IdNumber = uniqueId };

        FillStep1(
            firstName: "محمد", secondName: "أحمد", thirdName: "علي", lastName: "السيد",
            idNumber: uniqueId, sector: "A", dob: "01/01/1990",
            gender: "ذكر", phone: "0591234567",
            wallet: "123456789012345", governorate: "غزة",
            maritalStatus: "متزوج", employment: "موظف", education: "جامعي",
            healthStatus: "سليم"
        );
        ClickNextToStep2();

        ClickAddMember();
        FillMember(0, "فاطمة", uniqueId[..5] + "11111", "زوجة", "أنثى", "05/05/1995");
        ClickNextToStep3();

        SelectLivesInTent(true);
        SelectTentType("قماش");
        SelectHasBathroom(true);
        SelectBathroomType("خاص");
        SelectBathroomStatus("جيد");
        SelectDesire(1, "خيم");
        SelectDesire(2, "اغطية");
        SelectDesire(3, "فرشات");
        SelectDesire(4, "ادوات مطبخ");
        SelectDesire(5, "شوادر");
        ClickNextToStep4();

        FillPassword("test1234");
        FillConfirmPassword("test1234");
        CheckAcceptResponsibility();

        return data;
    }

    public string SubmitAndGetRecordId()
    {
        ClickSubmit();
        Test.WaitForElement(RecordIdDisplay, 15);
        return Test.GetText(RecordIdDisplay);
    }

    public bool IsSuccessDisplayed()
    {
        return Test.IsElementPresent(SuccessMessage);
    }

    public bool HasValidationErrors()
    {
        return Test.IsElementPresent(ValidationSummary);
    }

    public bool HasFieldError(string fieldId)
    {
        return Test.IsElementPresent(FieldError(fieldId));
    }

    public string GetValidationErrors()
    {
        return Test.IsElementPresent(ValidationSummary)
            ? Test.GetText(ValidationSummary)
            : "";
    }
}

public class RegistrationData
{
    public string IdNumber { get; set; } = "";
    public string? RecordId { get; set; }
}
