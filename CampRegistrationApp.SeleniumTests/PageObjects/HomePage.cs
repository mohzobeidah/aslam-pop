using OpenQA.Selenium;

namespace CampRegistrationApp.SeleniumTests.PageObjects;

public class HomePage : BasePage
{
    public HomePage(Infrastructure.TestBase test) : base(test) { }

    private static By WelcomeHeading => By.CssSelector("h1, h2, .welcome, .hero-title");
    private static By RegisterLink => By.LinkText("تسجيل عائلة");
    private static By RecordSearchLink => By.LinkText("الاستعلام عن قيد");
    private static By AdminLoginLink => By.LinkText("دخول المشرفين");
    private static By PrivacyLink => By.LinkText("سياسة الخصوصية");

    public void GoTo()
    {
        NavigateTo("/");
        Test.WaitForPageLoad();
    }

    public bool IsWelcomeMessageDisplayed()
    {
        return Test.IsElementPresent(WelcomeHeading);
    }

    public string GetWelcomeText()
    {
        return Test.GetText(WelcomeHeading);
    }

    public void ClickRegisterLink()
    {
        Test.Click(RegisterLink);
    }

    public void ClickRecordSearchLink()
    {
        Test.Click(RecordSearchLink);
    }

    public void ClickAdminLoginLink()
    {
        Test.Click(AdminLoginLink);
    }

    public void ClickPrivacyLink()
    {
        Test.Click(PrivacyLink);
    }
}
