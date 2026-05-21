using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CampRegistrationApp.SeleniumTests.Infrastructure;

[CollectionDefinition("SeleniumTests", DisableParallelization = true)]
public class SeleniumTestCollection : ICollectionFixture<CustomWebApplicationFactory> { }

public class TestBase : IDisposable
{
    public readonly CustomWebApplicationFactory Factory;
    public readonly IWebDriver Driver;
    public readonly WebDriverWait Wait;
    public readonly string BaseUrl;

    public TestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        BaseUrl = factory.GetServerUrl().TrimEnd('/');
        Driver = WebDriverFactory.CreateChromeDriver(headless: false);
        Driver.Manage().Window.Maximize();
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
    }

    public void Dispose()
    {
        try
        {
            Driver.Quit();
            Driver.Dispose();
        }
        catch { /* ignore cleanup errors */ }
    }

    public void NavigateTo(string relativeUrl)
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}{relativeUrl}");
    }

    public void WaitForElement(By by, int seconds = 10)
    {
        new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds))
            .Until(d => d.FindElement(by).Displayed);
    }

    public void Click(By by)
    {
        WaitForElement(by);
        Driver.FindElement(by).Click();
    }

    public void Type(By by, string text)
    {
        WaitForElement(by);
        var el = Driver.FindElement(by);
        el.Clear();
        el.SendKeys(text);
    }

    public string GetText(By by)
    {
        WaitForElement(by);
        return Driver.FindElement(by).Text;
    }

    public bool IsElementPresent(By by)
    {
        try
        {
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
            return Driver.FindElements(by).Count > 0;
        }
        finally
        {
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }
    }

    public void SelectDropdown(By by, string optionText)
    {
        var select = new SelectElement(Driver.FindElement(by));
        select.SelectByText(optionText);
    }

    public void TakeScreenshot(string name)
    {
        try
        {
            var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
            var path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "Screenshots",
                $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            screenshot.SaveAsFile(path);
        }
        catch { /* ignore screenshot errors */ }
    }

    public void WaitForPageLoad()
    {
        Wait.Until(d => ((IJavaScriptExecutor)d)
            .ExecuteScript("return document.readyState").Equals("complete"));
    }

    public string GetPageTitle()
    {
        return Driver.Title;
    }

    public string GetCurrentUrl()
    {
        return Driver.Url;
    }

    public string GetAlertTextAndAccept()
    {
        Wait.Until(d => d.SwitchTo().Alert() != null);
        var alert = Driver.SwitchTo().Alert();
        var text = alert.Text;
        alert.Accept();
        return text;
    }

    public void ExecuteScript(string script, params object[] args)
    {
        ((IJavaScriptExecutor)Driver).ExecuteScript(script, args);
    }

    public string GetLocalStorage(string key)
    {
        return ((IJavaScriptExecutor)Driver)
            .ExecuteScript($"return localStorage.getItem('{key}')") as string ?? "";
    }

    public void ScrollToElement(By by)
    {
        var element = Driver.FindElement(by);
        ((IJavaScriptExecutor)Driver)
            .ExecuteScript("arguments[0].scrollIntoView(true);", element);
    }

    public string GenerateValidPalestinianId()
    {
        var rng = new Random();
        var digits = new int[8];
        for (int i = 0; i < 8; i++)
            digits[i] = rng.Next(0, 10);
        int[] weights = [1, 2, 1, 2, 1, 2, 1, 2];
        int sum = 0;
        for (int i = 0; i < 8; i++)
        {
            int product = digits[i] * weights[i];
            sum += product >= 10 ? (product / 10) + (product % 10) : product;
        }
        int checkDigit = (10 - (sum % 10)) % 10;
        return string.Concat(digits.Select(d => d.ToString())) + checkDigit;
    }

    public string GenerateUniqueIdNumber() => GenerateValidPalestinianId();

    public string UploadTestFile(string extension = ".pdf")
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_file_{Guid.NewGuid()}{extension}");
        File.WriteAllText(tempFile, "Test file content for upload");
        return tempFile;
    }
}
