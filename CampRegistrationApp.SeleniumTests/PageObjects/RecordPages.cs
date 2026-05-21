using OpenQA.Selenium;

namespace CampRegistrationApp.SeleniumTests.PageObjects;

public class RecordSearchPage : BasePage
{
    public RecordSearchPage(Infrastructure.TestBase test) : base(test) { }

    private static By RecordIdInput => By.Id("RecordId");
    private static By HeadIdNumberInput => By.Id("HeadIdNumber");
    private static By SearchButton => By.CssSelector("button[type='submit'], .btn-search");
    private static By NotFoundMessage => By.CssSelector(".alert-danger, .not-found, .text-danger");
    private static By LoginLink => By.LinkText("تسجيل الدخول للتعديل");
    private static By RedirectToLogin => By.CssSelector("a[href*='Record/Login']");

    public void GoTo()
    {
        NavigateTo("/Record/Search");
        Test.WaitForPageLoad();
    }

    public void Search(string recordId, string headIdNumber)
    {
        Test.Type(RecordIdInput, recordId);
        Test.Type(HeadIdNumberInput, headIdNumber);
        Test.Click(SearchButton);
        Test.WaitForPageLoad();
    }

    public bool IsNotFound()
    {
        return Test.IsElementPresent(NotFoundMessage);
    }

    public string GetNotFoundText()
    {
        return Test.GetText(NotFoundMessage);
    }

    public void ClickLoginLink()
    {
        Test.Click(RedirectToLogin);
        Test.WaitForPageLoad();
    }
}

public class RecordLoginPage : BasePage
{
    public RecordLoginPage(Infrastructure.TestBase test) : base(test) { }

    private static By IdNumberInput => By.Id("IdNumber");
    private static By PasswordInput => By.Id("Password");
    private static By LoginButton => By.CssSelector("button[type='submit'], .btn-login");
    private static By ErrorMessage => By.CssSelector(".validation-summary-errors, .alert-danger, .text-danger");
    private static By LogoutButton => By.CssSelector("a[href*='Record/Logout'], button:contains('تسجيل خروج')");

    public void GoTo()
    {
        NavigateTo("/Record/Login");
        Test.WaitForPageLoad();
    }

    public void Login(string idNumber, string password)
    {
        Test.Type(IdNumberInput, idNumber);
        Test.Type(PasswordInput, password);
        Test.Click(LoginButton);
        Test.WaitForPageLoad();
    }

    public bool HasError()
    {
        return Test.IsElementPresent(ErrorMessage);
    }

    public string GetErrorMessage()
    {
        return Test.GetText(ErrorMessage);
    }

    public void ClickLogout()
    {
        Test.Click(LogoutButton);
        Test.WaitForPageLoad();
    }
}

public class RecordEditPage : BasePage
{
    public RecordEditPage(Infrastructure.TestBase test) : base(test) { }

    private static By FirstName => By.Id("Head_FirstName");
    private static By PhoneNumber => By.Id("Head_PhoneNumber");
    private static By UpdateButton => By.CssSelector("button[type='submit'], .btn-update, #updateBtn");
    private static By SuccessMessage => By.CssSelector(".alert-success, .success-message");
    private static By ErrorMessage => By.CssSelector(".validation-summary-errors, .alert-danger");

    public void GoTo()
    {
        NavigateTo("/Record/Edit");
        Test.WaitForPageLoad();
    }

    public void UpdateFirstName(string name)
    {
        Test.Type(FirstName, name);
    }

    public void UpdatePhone(string phone)
    {
        Test.Type(PhoneNumber, phone);
    }

    public void ClickUpdate()
    {
        Test.Click(UpdateButton);
        Test.WaitForPageLoad();
    }

    public string GetFirstName()
    {
        return Test.GetText(FirstName);
    }

    public bool IsSuccess()
    {
        return Test.IsElementPresent(SuccessMessage);
    }

    public bool HasError()
    {
        return Test.IsElementPresent(ErrorMessage);
    }

    public string GetSuccessMessage()
    {
        return Test.GetText(SuccessMessage);
    }
}
