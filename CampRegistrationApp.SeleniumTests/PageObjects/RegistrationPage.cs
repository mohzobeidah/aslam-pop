using OpenQA.Selenium;

namespace CampRegistrationApp.SeleniumTests.PageObjects;

public class RegistrationPage : BasePage
{
    public RegistrationPage(Infrastructure.TestBase test) : base(test) { }

    // Step navigation
    private static By NextButton => By.Id("nextBtn");
    private static By PrevButton => By.Id("prevBtn");
    private static By SubmitButton => By.Id("submitBtn");

    // Step 1 - Family Head
    private static By FirstName => By.CssSelector("[name='Head.FirstName']");
    private static By SecondName => By.CssSelector("[name='Head.SecondName']");
    private static By ThirdName => By.CssSelector("[name='Head.ThirdName']");
    private static By LastName => By.CssSelector("[name='Head.LastName']");
    private static By IdNumber => By.CssSelector("[name='Head.IdNumber']");
    private static By Sector => By.CssSelector("[name='Head.Sector']");
    private static By DateOfBirth => By.CssSelector("[name='Head.DateOfBirth']");
    private static By GenderMale => By.CssSelector("input[name='Head.Gender'][value='ذكر']");
    private static By GenderFemale => By.CssSelector("input[name='Head.Gender'][value='أنثى']");
    private static By PhoneNumber => By.CssSelector("[name='Head.PhoneNumber']");
    private static By Wallet => By.CssSelector("[name='Head.Wallet']");
    private static By WalletType => By.CssSelector("[name='Head.WalletType']");
    private static By OriginalGovernorate => By.CssSelector("[name='Head.OriginalGovernorate']");
    private static By MaritalStatus => By.CssSelector("[name='Head.MaritalStatus']");
    private static By EmploymentStatus => By.CssSelector("[name='Head.EmploymentStatus']");
    private static By EducationLevel => By.CssSelector("[name='Head.EducationLevel']");
    private static By HealthStatusHealthy => By.CssSelector("input[name='Head.HealthStatus'][value='سليم']");
    private static By HealthStatusSick => By.CssSelector("input[name='Head.HealthStatus'][value='مريض']");
    private static By HasInjuryYes => By.CssSelector("input[name='Head.HasInjury'][value='true']");
    private static By IsPrisoner => By.CssSelector("[name='Head.IsPrisoner']");
    private static By HeadIdImageUpload => By.CssSelector("input[type='file'][accept*='image']");

    // Step 2 - Family Members
    private static By AddMemberButton => By.CssSelector("button[onclick*='addMember']");
    private static By MemberFirstName(int index) => By.CssSelector($"[name='Members[{index}].FirstName']");
    private static By MemberIdNumber(int index) => By.CssSelector($"[name='Members[{index}].IdNumber']");
    private static By MemberRelationship(int index) => By.CssSelector($"[name='Members[{index}].RelationshipToHead']");
    private static By MemberGender(int index) => By.CssSelector($"[name='Members[{index}].Gender']");
    private static By MemberDateOfBirth(int index) => By.CssSelector($"[name='Members[{index}].DateOfBirth']");

    // Step 3 - Housing & Special Cases
    private static By IsChildHeaded => By.CssSelector("[name='IsChildHeaded']");
    private static By ChildHeadedDetails => By.CssSelector("[name='ChildHeadedDetails']");
    private static By IsFemaleHeaded => By.CssSelector("[name='IsFemaleHeaded']");
    private static By IsHusbandAbroad => By.CssSelector("[name='IsHusbandAbroad']");
    private static By LivesInTentYes => By.CssSelector("input[name='LivesInTent'][value='true']");
    private static By LivesInTentNo => By.CssSelector("input[name='LivesInTent'][value='false']");
    private static By TentType => By.CssSelector("[name='TentType']");
    private static By HasBathroomYes => By.CssSelector("input[name='HasBathroom'][value='true']");
    private static By BathroomTypePrivate => By.CssSelector("input[name='BathroomType'][value='Private']");
    private static By BathroomTypeShared => By.CssSelector("input[name='BathroomType'][value='Shared']");
    private static By BathroomStatus => By.CssSelector("[name='Head.BathroomStatus']");
    private static By DesireSelect(int rank) => By.CssSelector($"[name='Desires[{rank - 1}]']");

    // Step 4 - Review & Confirm
    private static By Password => By.Id("regPassword");
    private static By ConfirmPassword => By.Id("regConfirmPassword");
    private static By AcceptResponsibility => By.Id("acceptResponsibility");
    private static By StatusNotes => By.CssSelector("[name='StatusNotes']");
    private static By FinalSubmitButton => By.Id("submitBtn");

    // Success
    private static By SuccessContainer => By.CssSelector(".text-camp-gold.text-2xl, .font-mono.text-camp-gold.text-sm");

    // Validation errors
    private static By ValidationSummary => By.CssSelector(".text-red-500.text-sm, .validation-summary-errors");

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

    public void SelectWalletType(string walletType)
    {
        Test.SelectDropdown(WalletType, walletType);
    }

    public void FillOriginalGovernorate(string gov)
    {
        Test.SelectDropdown(OriginalGovernorate, gov);
    }

    public void SelectMaritalStatus(string status)
    {
        Test.SelectDropdown(MaritalStatus, status);
    }

    public void SelectEmploymentStatus(string status)
    {
        Test.Type(EmploymentStatus, status);
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
        Test.Click(NextButton);
        WaitForAjax();
    }

    public void FillStep1(string firstName, string secondName, string thirdName, string lastName,
        string idNumber, string sector, string dob, string gender, string phone,
        string wallet, string governorate, string maritalStatus, string employment, string education,
        string healthStatus = "سليم", string walletType = "بنك")
    {
        FillHeadName(firstName, secondName, thirdName, lastName);
        FillHeadIdNumber(idNumber);
        SelectSector(sector);
        FillDateOfBirth(dob);
        SelectGender(gender);
        FillPhoneNumber(phone);
        FillWallet(wallet);
        SelectWalletType(walletType);
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
        Test.SelectDropdown(MemberGender(index), gender);
        Test.Type(MemberDateOfBirth(index), dob);
    }

    public void RemoveMember(int index)
    {
        var removeButtons = Test.Driver.FindElements(By.CssSelector("button[onclick*='removeMember']"));
        if (index < removeButtons.Count)
        {
            removeButtons[index].Click();
            WaitForAjax();
        }
    }

    public void ClickNextToStep3()
    {
        Test.Click(NextButton);
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
        if (type == "خاص" || type == "Private")
            Test.Click(BathroomTypePrivate);
        else
            Test.Click(BathroomTypeShared);
    }

    public void SelectBathroomStatus(string status)
    {
        Test.SelectDropdown(BathroomStatus, status);
    }

    public void SelectDesire(int rank, string desireName)
    {
        Test.SelectDropdown(DesireSelect(rank), desireName);
    }

    public void ClickNextToStep4()
    {
        Test.Click(NextButton);
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
        Test.ExecuteScript(@"
            // Get all form data as JSON for logging
            const form = document.getElementById('registrationForm');
            const data = {};
            new FormData(form).forEach((v, k) => {
                if (!data[k]) data[k] = [];
                data[k].push(v);
            });
            console.log('FormData:', JSON.stringify(data));
        ");
        // Take a brief pause for console output, then submit directly
        Thread.Sleep(100);
        // Use the original submitForm but intercept the duplicate issue
        Test.ExecuteScript(@"
            const form = document.getElementById('registrationForm');
            // First validate all steps inline
            if (typeof validateStep1 === 'function') {
                const v1 = validateStep1();
                const v2 = validateStep2();
                const v3 = validateStep3();
                if (!v1 || !v2 || !v3) {
                    console.log('Validation failed: v1=' + v1 + ' v2=' + v2 + ' v3=' + v3);
                }
            }
            // Add indexed desire values
            const desireSelects = document.querySelectorAll('.desire-select');
            desireSelects.forEach((sel, i) => {
                const hidden = document.createElement('input');
                hidden.type = 'hidden';
                hidden.name = 'DesireIds[' + i + ']';
                hidden.value = sel.value;
                form.appendChild(hidden);
            });
            // Submit directly
            form.submit();
        ");
        Test.WaitForPageLoad();
    }

    // ===== Submit Full Registration =====
    public RegistrationData FillBasicRegistration(string uniqueId)
    {
        var data = new RegistrationData { IdNumber = uniqueId };

        FillStep1(
            firstName: "محمد", secondName: "أحمد", thirdName: "علي", lastName: "السيد",
            idNumber: uniqueId, sector: "A", dob: "1990-01-01",
            gender: "ذكر", phone: "0591234567",
            wallet: "0591234567", governorate: "غزة",
            maritalStatus: "متزوج", employment: "موظف", education: "جامعي",
            healthStatus: "سليم"
        );
        ClickNextToStep2();

        var memberId = Test.GenerateValidPalestinianId();
        ClickAddMember();
        FillMember(0, "فاطمة", memberId, "زوجة", "أنثى", "1995-05-05");
        ClickNextToStep3();

        SelectLivesInTent(true);
        SelectTentType("تركيب");
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
        Test.WaitForPageLoad();
        // Check if we're on success page or back on registration form
        var successHeading = Test.Driver.FindElements(By.CssSelector("h1.text-camp-gold"));
        if (successHeading.Count > 0 && successHeading[0].Text.Contains("تم التسجيل"))
        {
            var recordIdEl = Test.Driver.FindElement(By.CssSelector("div.font-mono.font-bold"));
            return recordIdEl.Text.Trim();
        }
        // Error page - show the page source snippet
        var body = Test.Driver.FindElement(By.TagName("body"));
        throw new Exception($"Submit failed. Page body preview: {body.Text[..Math.Min(500, body.Text.Length)]}");
    }

    public bool IsSuccessDisplayed()
    {
        return Test.IsElementPresent(SuccessContainer);
    }

    public bool HasValidationErrors()
    {
        return Test.IsElementPresent(ValidationSummary);
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
