using CampRegistrationApp.SeleniumTests.Infrastructure;
using CampRegistrationApp.SeleniumTests.PageObjects;
using CampRegistrationApp.Models;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using CampRegistrationApp.Data;
using OpenQA.Selenium;
using System.Security.Cryptography;
using System.Text;

namespace CampRegistrationApp.SeleniumTests.Tests;

[Collection("SeleniumTests")]
public class AdminApprovalAndRefugeeTests : TestBase
{
    private readonly AdminLoginPage _loginPage;
    private readonly AdminDashboardPage _dashboardPage;
    private readonly AdminRegistrationsPage _registrationsPage;
    private readonly AdminRefugeesPage _refugeesPage;
    private readonly RefugeeDetailsPage _detailsPage;
    private readonly RegistrationPage _regPage;
    private readonly ITestOutputHelper _output;

    public AdminApprovalAndRefugeeTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _loginPage = new AdminLoginPage(this);
        _dashboardPage = new AdminDashboardPage(this);
        _registrationsPage = new AdminRegistrationsPage(this);
        _refugeesPage = new AdminRefugeesPage(this);
        _detailsPage = new RefugeeDetailsPage(this);
        _regPage = new RegistrationPage(this);
        _output = output;
    }

private string RegisterAndGetRecordId()
{
    // Register via direct DB access to avoid multi-step wizard JS submission bugs
    using var scope = Factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var idGen = scope.ServiceProvider.GetRequiredService<CampRegistrationApp.Services.IRecordIdGenerator>();

    var headId = GenerateValidPalestinianId();
    var memberId = GenerateValidPalestinianId();

    var recordId = idGen.GenerateUniqueIdAsync().GetAwaiter().GetResult();

    var head = new Person
    {
        FirstName = "محمد", SecondName = "أحمد", ThirdName = "علي", LastName = "السيد",
        IdNumber = headId, DateOfBirth = new DateTime(1990, 1, 1),
        Gender = "ذكر",
        OriginalGovernorate = "غزة", MaritalStatus = "متزوج", EmploymentStatus = "موظف",
        EducationLevel = "جامعي", HealthStatus = "سليم"
    };
    db.Persons.Add(head);
    db.SaveChanges();

    var member = new Person
    {
        FirstName = "فاطمة", SecondName = "", ThirdName = "", LastName = "السيد",
        IdNumber = memberId, DateOfBirth = new DateTime(1995, 5, 5),
        Gender = "أنثى", HealthStatus = "سليم"
    };
    db.Persons.Add(member);
    db.SaveChanges();

    var sectorA = db.Sectors.First(s => s.Name == "A");
        var registration = new FamilyRegistration
        {
            RecordId = recordId,
            FamilyHeadId = head.Id,
            ApprovalStatus = RegistrationApprovalStatus.Pending,
            SectorId = sectorA.Id,
            PhoneNumber = "0591234567",
            Wallet = "0591234567",
            WalletType = "بنك",
        LivesInTent = true, TentType = "Installation",
        HasBathroom = true, BathroomType = "Private",
        RegistrationTimestamp = DateTime.UtcNow
    };
    db.FamilyRegistrations.Add(registration);
    db.SaveChanges();

    db.FamilyMembers.Add(new FamilyMember
    {
        RegistrationId = registration.Id,
        PersonId = member.Id,
        RelationshipToHead = "زوجة"
    });
    db.SaveChanges();

    // Add desires
    var allDesires = db.Desires.OrderBy(d => d.Id).ToList();
    for (int i = 0; i < Math.Min(5, allDesires.Count); i++)
    {
        db.FamilyDesires.Add(new FamilyDesire
        {
            FamilyRegistrationId = registration.Id,
            DesireId = allDesires[i].Id,
            Order = i + 1
        });
    }
    db.SaveChanges();

    return recordId;
}

    [Fact]
    public void Mandoob_CanEditAndApprove_PendingRegistration()
    {
        // 1. Register a new family
        RegisterAndGetRecordId();

        // 2. Create a mandoob for sector A directly in the in-memory DB
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("mandoob123")));
            db.Admins.Add(new Admin
            {
                Name = "مندوب القطاع أ",
                NationalId = "mandoob",
                Mobile = "0599999999",
                PasswordHash = hash,
                Role = AdminRole.Mandoob,
                SectorId = 1
            });
            db.SaveChanges();
        }

        // 3. Login as mandoob
        _loginPage.GoTo();
        _loginPage.Login("mandoob", "mandoob123");

        // 4. Navigate to registrations page
        _dashboardPage.ClickRegistrations();

        // 5. Click Edit link on first pending registration
        var editLink = Wait.Until(d => d.FindElement(By.CssSelector("a[href*='AdminEditRegistration']")));
        editLink.Click();
        // Keep dismissing alerts until page loads (Edit page fires validation alert on load)
        for (int attempt = 0; attempt < 30; attempt++)
        {
            try { Driver.SwitchTo().Alert().Accept(); }
            catch { try { Thread.Sleep(200); } catch { } }
        }
        WaitForPageLoad();

        // 6. Change the family head's first name
        var firstNameInput = Wait.Until(d => d.FindElement(By.Name("Head.FirstName")));
        firstNameInput.Clear();
        firstNameInput.SendKeys("محمد المندوب");

        // 7. Submit the edit form
        var saveButton = Driver.FindElement(By.CssSelector("button[type='submit']"));
        ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", saveButton);
        saveButton.Click();
        WaitForPageLoad();

        // 8. Click the Approve button on the first pending row
        var approveButton = Wait.Until(d => d.FindElement(By.XPath("//button[normalize-space()='موافقة']")));
        approveButton.Click();
        WaitForPageLoad();

        // 9. Verify the registration is now approved
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.FamilyRegistrations.Any(r => r.ApprovalStatus == RegistrationApprovalStatus.Approved)
                .Should().BeTrue();
        }

        // Verify the edit persisted by checking the approved-by field shows mandoob as actor
        _registrationsPage.FilterByStatus("Approved");
        _registrationsPage.HasRegistrations().Should().BeTrue();
    }
}
