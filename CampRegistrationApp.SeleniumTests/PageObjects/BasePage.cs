namespace CampRegistrationApp.SeleniumTests.PageObjects;

public abstract class BasePage
{
    protected readonly Infrastructure.TestBase Test;
    protected readonly string BaseUrl;

    protected BasePage(Infrastructure.TestBase test)
    {
        Test = test;
        BaseUrl = test.BaseUrl;
    }

    public void NavigateTo(string relativeUrl)
    {
        Test.NavigateTo(relativeUrl);
    }

    public bool IsOnPage(string expectedUrlSuffix)
    {
        return Test.GetCurrentUrl().Contains(expectedUrlSuffix);
    }

    public string GetPageTitle()
    {
        return Test.GetPageTitle();
    }

    public void WaitForAjax()
    {
        System.Threading.Thread.Sleep(500);
    }
}
