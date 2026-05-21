using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using Xunit.Abstractions;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class HomePageTests : TestBase
{
    private readonly HomePage _homePage;
    private readonly ITestOutputHelper _output;

    public HomePageTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _homePage = new HomePage(this);
        _output = output;
    }

    [Fact]
    public void HomePage_LoadsSuccessfully()
    {
        _homePage.GoTo();
        _homePage.IsWelcomeMessageDisplayed().Should().BeTrue();
    }

    [Fact]
    public void HomePage_ContainsRegisterLink()
    {
        _homePage.GoTo();
        _homePage.ClickRegisterLink();
        GetCurrentUrl().Should().Contain("Registration");
    }

    [Fact]
    public void HomePage_ContainsRecordSearchLink()
    {
        _homePage.GoTo();
        _homePage.ClickRecordSearchLink();
        GetCurrentUrl().Should().Contain("Record/Search");
    }

    [Fact]
    public void HomePage_ContainsAdminLoginLink()
    {
        _homePage.GoTo();
        _homePage.ClickAdminLoginLink();
        GetCurrentUrl().Should().Contain("Admin/Login");
    }

    [Fact]
    public void PrivacyPage_LoadsSuccessfully()
    {
        _homePage.GoTo();
        _homePage.ClickPrivacyLink();
        GetCurrentUrl().Should().Contain("Home/Privacy");
    }

    [Fact]
    public void ErrorPage_ReturnsView()
    {
        NavigateTo("/Home/Error");
        WaitForPageLoad();
        GetPageTitle().Should().NotBeNullOrEmpty();
    }
}
