using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using CampRegistrationApp.Data;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class NotificationAndAuditLogTests : TestBase
{
    private readonly AdminLoginPage _loginPage;
    private readonly AdminDashboardPage _dashboardPage;
    private readonly AdminNotificationsPage _notificationsPage;
    private readonly AdminAuditLogsPage _auditLogsPage;
    private readonly RegistrationPage _regPage;
    private readonly ITestOutputHelper _output;

    public NotificationAndAuditLogTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _loginPage = new AdminLoginPage(this);
        _dashboardPage = new AdminDashboardPage(this);
        _notificationsPage = new AdminNotificationsPage(this);
        _auditLogsPage = new AdminAuditLogsPage(this);
        _regPage = new RegistrationPage(this);
        _output = output;
    }

    private void LoginAsAdmin()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
    }

    [Fact]
    public void NewRegistration_CreatesNotification_ForAdmin()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);
        _regPage.SubmitAndGetRecordId();

        LoginAsAdmin();
        _dashboardPage.ClickNotifications();
        _notificationsPage.GetNotificationCount().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void NotificationBadge_DisplaysAfterNewRegistration()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);
        _regPage.SubmitAndGetRecordId();

        LoginAsAdmin();
        _dashboardPage.IsNotificationBadgeDisplayed().Should().BeTrue();
    }

    [Fact]
    public void MarkAllNotificationsRead_ClearsBadge()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);
        _regPage.SubmitAndGetRecordId();

        LoginAsAdmin();
        _dashboardPage.ClickNotifications();
        _notificationsPage.MarkAllAsRead();
        _notificationsPage.IsEmpty().Should().BeTrue();
    }

    [Fact]
    public void AuditLogs_ShowsEntries_AfterRegistration()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);
        _regPage.SubmitAndGetRecordId();

        LoginAsAdmin();
        _dashboardPage.ClickAuditLogs();
        _auditLogsPage.HasLogs().Should().BeTrue();
    }

    [Fact]
    public void AuditLogs_FilterByTable_Works()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);
        _regPage.SubmitAndGetRecordId();

        LoginAsAdmin();
        _dashboardPage.ClickAuditLogs();
        _auditLogsPage.FilterByTable("FamilyRegistration");
        _auditLogsPage.HasLogs().Should().BeTrue();
    }

    [Fact]
    public void AuditLogs_FilterByAction_Works()
    {
        var uniqueId = GenerateUniqueIdNumber();
        _regPage.GoTo();
        _regPage.FillBasicRegistration(uniqueId);
        _regPage.SubmitAndGetRecordId();

        LoginAsAdmin();
        _dashboardPage.ClickAuditLogs();
        _auditLogsPage.FilterByAction("Create");
        _auditLogsPage.HasLogs().Should().BeTrue();
    }

    [Fact]
    public void AuditLogs_Pagination_Works()
    {
        for (int i = 0; i < 3; i++)
        {
            var uniqueId = GenerateUniqueIdNumber();
            _regPage.GoTo();
            _regPage.FillBasicRegistration(uniqueId);
            _regPage.SubmitAndGetRecordId();
        }

        LoginAsAdmin();
        _dashboardPage.ClickAuditLogs();
        _auditLogsPage.HasPagination().Should().BeTrue("multiple audit entries should trigger pagination");
    }

    [Fact]
    public void AuditLogs_RecordsAdminActions()
    {
        _loginPage.GoTo();
        _loginPage.Login("admin", "admin123");
        WaitForPageLoad();
        _dashboardPage.ClickRegistrations();

        LoginAsAdmin();
        _dashboardPage.ClickAuditLogs();
        _auditLogsPage.HasLogs().Should().BeTrue();
    }
}
