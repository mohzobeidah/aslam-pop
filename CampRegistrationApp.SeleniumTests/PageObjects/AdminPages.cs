using OpenQA.Selenium;

namespace CampRegistrationApp.SeleniumTests.PageObjects;

public class AdminLoginPage : BasePage
{
    public AdminLoginPage(Infrastructure.TestBase test) : base(test) { }

    private static By NationalIdInput => By.Id("NationalId");
    private static By PasswordInput => By.Id("Password");
    private static By LoginButton => By.CssSelector("button[type='submit'], .btn-login");
    private static By ErrorMessage => By.CssSelector(".validation-summary-errors, .alert-danger");

    public void GoTo()
    {
        NavigateTo("/Admin/Login");
        Test.WaitForPageLoad();
    }

    public void Login(string nationalId, string password)
    {
        Test.Type(NationalIdInput, nationalId);
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

    public bool IsOnLoginPage()
    {
        return Test.GetCurrentUrl().Contains("/Admin/Login");
    }
}

public class AdminDashboardPage : BasePage
{
    public AdminDashboardPage(Infrastructure.TestBase test) : base(test) { }

    private static By StatsCards => By.CssSelector(".stats-card, .dashboard-stat, .card");
    private static By TotalRegistrations => By.CssSelector("#totalRegistrations, .stat-registrations");
    private static By TotalAdmins => By.CssSelector("#totalAdmins, .stat-admins");
    private static By TotalSectors => By.CssSelector("#totalSectors, .stat-sectors");
    private static By SectorTable => By.CssSelector(".sector-table, table");
    private static By RegistrationsLink => By.LinkText("الطلبات");
    private static By RefugeesLink => By.LinkText("اللاجئين");
    private static By AdminManagementLink => By.LinkText("إدارة المشرفين");
    private static By SectorManagementLink => By.LinkText("القطاعات");
    private static By ProjectManagementLink => By.LinkText("المشاريع");
    private static By AssistanceLink => By.LinkText("المساعدات");
    private static By AuditLogsLink => By.LinkText("سجل التدقيق");
    private static By NotificationsLink => By.LinkText("الإشعارات");
    private static By NotificationBadge => By.CssSelector(".notification-badge, .badge");
    private static By LogoutButton => By.CssSelector("a[href*='Admin/Logout'], button:contains('تسجيل خروج')");
    private static By DashboardHeading => By.CssSelector("h1, h2, .dashboard-title");

    public void GoTo()
    {
        NavigateTo("/Admin/Dashboard");
        Test.WaitForPageLoad();
    }

    public string GetHeading()
    {
        return Test.GetText(DashboardHeading);
    }

    public bool IsStatsDisplayed()
    {
        return Test.IsElementPresent(StatsCards);
    }

    public bool IsSectorTableDisplayed()
    {
        return Test.IsElementPresent(SectorTable);
    }

    public void ClickRegistrations()
    {
        Test.Click(RegistrationsLink);
        Test.WaitForPageLoad();
    }

    public void ClickRefugees()
    {
        Test.Click(RefugeesLink);
        Test.WaitForPageLoad();
    }

    public void ClickAdminManagement()
    {
        Test.Click(AdminManagementLink);
        Test.WaitForPageLoad();
    }

    public void ClickSectorManagement()
    {
        Test.Click(SectorManagementLink);
        Test.WaitForPageLoad();
    }

    public void ClickProjects()
    {
        Test.Click(ProjectManagementLink);
        Test.WaitForPageLoad();
    }

    public void ClickAssistance()
    {
        Test.Click(AssistanceLink);
        Test.WaitForPageLoad();
    }

    public void ClickAuditLogs()
    {
        Test.Click(AuditLogsLink);
        Test.WaitForPageLoad();
    }

    public void ClickNotifications()
    {
        Test.Click(NotificationsLink);
        Test.WaitForPageLoad();
    }

    public void ClickLogout()
    {
        Test.Click(LogoutButton);
        Test.WaitForPageLoad();
    }

    public bool IsNotificationBadgeDisplayed()
    {
        return Test.IsElementPresent(NotificationBadge);
    }

    public string GetNotificationBadgeText()
    {
        return Test.GetText(NotificationBadge);
    }

    public int GetStatRegistrations()
    {
        return int.Parse(Test.GetText(TotalRegistrations));
    }
}

public class AdminRegistrationsPage : BasePage
{
    public AdminRegistrationsPage(Infrastructure.TestBase test) : base(test) { }

    private static By StatusFilter => By.Id("status");
    private static By SectorFilter => By.Id("sector");
    private static By FilterButton => By.CssSelector("button[type='submit'], .btn-filter");
    private static By RegistrationRows => By.CssSelector("table tbody tr, .registration-row");
    private static By ApproveButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-approve, .approve-btn-{index}");
    private static By RejectButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-reject, .reject-btn-{index}");
    private static By RemoveButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-remove, .remove-btn-{index}");
    private static By EditButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-edit, .edit-btn-{index}");
    private static By EmptyMessage => By.CssSelector(".empty-message, .no-data, .alert-info");
    private static By RecordCount => By.CssSelector(".record-count, .total-count");

    public void GoTo()
    {
        NavigateTo("/Admin/Registrations");
        Test.WaitForPageLoad();
    }

    public void FilterByStatus(string status)
    {
        Test.SelectDropdown(StatusFilter, status);
        Test.Click(FilterButton);
        Test.WaitForPageLoad();
    }

    public void FilterBySector(string sector)
    {
        Test.SelectDropdown(SectorFilter, sector);
        Test.Click(FilterButton);
        Test.WaitForPageLoad();
    }

    public int GetRegistrationCount()
    {
        return Test.Driver.FindElements(RegistrationRows).Count;
    }

    public void ApproveRegistration(int index = 0)
    {
        Test.Click(ApproveButton(index));
        Test.WaitForPageLoad();
    }

    public void RejectRegistration(int index = 0)
    {
        Test.Click(RejectButton(index));
        Test.WaitForPageLoad();
    }

    public void RemoveRefugee(int index = 0)
    {
        Test.Click(RemoveButton(index));
        Test.WaitForPageLoad();
    }

    public void ClickEdit(int index = 0)
    {
        Test.Click(EditButton(index));
        Test.WaitForPageLoad();
    }

    public bool HasRegistrations()
    {
        return GetRegistrationCount() > 0;
    }

    public bool IsEmpty()
    {
        return Test.IsElementPresent(EmptyMessage);
    }

    public string GetEmptyMessage()
    {
        return Test.GetText(EmptyMessage);
    }
}

public class AdminRefugeesPage : BasePage
{
    public AdminRefugeesPage(Infrastructure.TestBase test) : base(test) { }

    private static By SearchInput => By.CssSelector("input[type='search'], #search");
    private static By SectorFilter => By.Id("sector");
    private static By StatusFilter => By.Id("status");
    private static By SearchButton => By.CssSelector("button[type='submit'], .btn-search");
    private static By RefugeeRows => By.CssSelector("table tbody tr, .refugee-row");
    private static By ExportExcelButton => By.CssSelector("a:contains('تصدير إكسل'), .btn-export");
    private static By RefugeeLink(int index) => By.CssSelector($"tr:nth-child({index + 1}) a, .refugee-link-{index}");
    private static By EditButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-edit, .edit-btn-{index}");
    private static By EmptyMessage => By.CssSelector(".empty-message, .no-data");
    private static By PendingCount => By.CssSelector(".count-pending");
    private static By ApprovedCount => By.CssSelector(".count-approved");
    private static By RejectedCount => By.CssSelector(".count-rejected");

    public void GoTo()
    {
        NavigateTo("/Admin/Refugees");
        Test.WaitForPageLoad();
    }

    public void Search(string query)
    {
        Test.Type(SearchInput, query);
        Test.Click(SearchButton);
        Test.WaitForPageLoad();
    }

    public void FilterBySector(string sector)
    {
        Test.SelectDropdown(SectorFilter, sector);
        Test.Click(SearchButton);
        Test.WaitForPageLoad();
    }

    public void FilterByStatus(string status)
    {
        Test.SelectDropdown(StatusFilter, status);
        Test.Click(SearchButton);
        Test.WaitForPageLoad();
    }

    public int GetRefugeeCount()
    {
        return Test.Driver.FindElements(RefugeeRows).Count;
    }

    public void ClickRefugee(int index = 0)
    {
        Test.Click(RefugeeLink(index));
        Test.WaitForPageLoad();
    }

    public void ClickEdit(int index = 0)
    {
        Test.Click(EditButton(index));
        Test.WaitForPageLoad();
    }

    public void ClickExportExcel()
    {
        Test.Click(ExportExcelButton);
    }

    public bool HasRefugees()
    {
        return GetRefugeeCount() > 0;
    }

    public bool IsEmpty()
    {
        return Test.IsElementPresent(EmptyMessage);
    }
}

public class RefugeeDetailsPage : BasePage
{
    public RefugeeDetailsPage(Infrastructure.TestBase test) : base(test) { }

    private static By HeadName => By.CssSelector(".head-name, #headName");
    private static By RecordId => By.CssSelector(".record-id, #recordId");
    private static By IdNumber => By.CssSelector(".id-number, #idNumber");
    private static By Sector => By.CssSelector(".sector, #sector");
    private static By StatusBadge => By.CssSelector(".status-badge, .approval-status");
    private static By MembersSection => By.CssSelector("#members, .members-section");
    private static By DesiresSection => By.CssSelector("#desires, .desires-section");
    private static By BathroomSection => By.CssSelector("#bathroom, .bathroom-section");
    private static By HousingSection => By.CssSelector("#housing, .housing-section");
    private static By EditButton => By.CssSelector("a:contains('تعديل'), .btn-edit");
    private static By BackButton => By.CssSelector("a:contains('عودة'), .btn-back");

    public void GoTo(int registrationId)
    {
        NavigateTo($"/Admin/RefugeeDetails/{registrationId}");
        Test.WaitForPageLoad();
    }

    public string GetHeadName()
    {
        return Test.GetText(HeadName);
    }

    public string GetRecordId()
    {
        return Test.GetText(RecordId);
    }

    public string GetStatus()
    {
        return Test.GetText(StatusBadge);
    }

    public bool HasMembersSection()
    {
        return Test.IsElementPresent(MembersSection);
    }

    public bool HasDesiresSection()
    {
        return Test.IsElementPresent(DesiresSection);
    }

    public bool HasBathroomSection()
    {
        return Test.IsElementPresent(BathroomSection);
    }

    public bool HasHousingSection()
    {
        return Test.IsElementPresent(HousingSection);
    }

    public void ClickEdit()
    {
        Test.Click(EditButton);
        Test.WaitForPageLoad();
    }

    public bool HasEditButton()
    {
        return Test.IsElementPresent(EditButton);
    }
}

public class AdminCrudPage : BasePage
{
    public AdminCrudPage(Infrastructure.TestBase test) : base(test) { }

    private static By AdminRows => By.CssSelector("table tbody tr, .admin-row");
    private static By CreateButton => By.LinkText("إضافة مشرف");
    private static By NameInput => By.Id("Name");
    private static By NationalIdInput => By.Id("NationalId");
    private static By MobileInput => By.Id("Mobile");
    private static By RoleSelect => By.Id("Role");
    private static By SectorSelect => By.Id("SectorId");
    private static By PasswordInput => By.Id("Password");
    private static By SaveButton => By.CssSelector("button[type='submit'], .btn-save");
    private static By EditButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-edit");
    private static By DeleteButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-delete");
    private static By SuccessMessage => By.CssSelector(".alert-success");
    private static By EmptyMessage => By.CssSelector(".empty-message, .no-data");

    public void GoTo()
    {
        NavigateTo("/Admin");
        Test.WaitForPageLoad();
    }

    public void GoToCreate()
    {
        NavigateTo("/Admin/Create");
        Test.WaitForPageLoad();
    }

    public int GetAdminCount()
    {
        return Test.Driver.FindElements(AdminRows).Count;
    }

    public void ClickCreate()
    {
        Test.Click(CreateButton);
        Test.WaitForPageLoad();
    }

    public void FillCreateForm(string name, string nationalId, string mobile, string role, string? password = null)
    {
        Test.Type(NameInput, name);
        Test.Type(NationalIdInput, nationalId);
        Test.Type(MobileInput, mobile);
        Test.SelectDropdown(RoleSelect, role);
        if (!string.IsNullOrEmpty(password))
            Test.Type(PasswordInput, password);
    }

    public void ClickSave()
    {
        Test.Click(SaveButton);
        Test.WaitForPageLoad();
    }

    public void ClickEdit(int index = 0)
    {
        Test.Click(EditButton(index));
        Test.WaitForPageLoad();
    }

    public void ClickDelete(int index = 0)
    {
        Test.Click(DeleteButton(index));
        Test.WaitForPageLoad();
    }

    public bool IsSuccess()
    {
        return Test.IsElementPresent(SuccessMessage);
    }

    public bool IsEmpty()
    {
        return Test.IsElementPresent(EmptyMessage);
    }
}

public class AdminSectorsPage : BasePage
{
    public AdminSectorsPage(Infrastructure.TestBase test) : base(test) { }

    private static By SectorRows => By.CssSelector("table tbody tr, .sector-row");
    private static By CreateButton => By.LinkText("إضافة قطاع");
    private static By NameInput => By.Id("Name");
    private static By CampInput => By.Id("Camp");
    private static By CoordinateInput => By.Id("Coordinate");
    private static By AreaInput => By.Id("Area");
    private static By ManufacturedTentsInput => By.Id("ManufacturedTentsCount");
    private static By HandmadeTentsInput => By.Id("HandmadeTentsCount");
    private static By BathroomsInput => By.Id("BathroomsCount");
    private static By SaveButton => By.CssSelector("button[type='submit'], .btn-save");
    private static By EditButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-edit");
    private static By DeleteButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-delete");
    private static By AssignMandoobButton => By.CssSelector(".btn-assign-mandoob, a:contains('تعيين مندوب')");
    private static By MandoobSearchInput => By.Id("mandoobSearch");
    private static By SuccessMessage => By.CssSelector(".alert-success");

    public void GoTo()
    {
        NavigateTo("/Admin/Sectors");
        Test.WaitForPageLoad();
    }

    public void GoToCreate()
    {
        NavigateTo("/Admin/CreateSector");
        Test.WaitForPageLoad();
    }

    public int GetSectorCount()
    {
        return Test.Driver.FindElements(SectorRows).Count;
    }

    public void CreateSector(string name, string camp, string coordinate, string area,
        int manufacturedTents = 0, int handmadeTents = 0, int bathrooms = 0)
    {
        Test.Type(NameInput, name);
        Test.Type(CampInput, camp);
        Test.Type(CoordinateInput, coordinate);
        Test.Type(AreaInput, area);
        Test.Type(ManufacturedTentsInput, manufacturedTents.ToString());
        Test.Type(HandmadeTentsInput, handmadeTents.ToString());
        Test.Type(BathroomsInput, bathrooms.ToString());
    }

    public void ClickSave()
    {
        Test.Click(SaveButton);
        Test.WaitForPageLoad();
    }

    public void ClickEdit(int index = 0)
    {
        Test.Click(EditButton(index));
        Test.WaitForPageLoad();
    }

    public void ClickDelete(int index = 0)
    {
        Test.Click(DeleteButton(index));
        Test.WaitForPageLoad();
    }

    public void ClickAssignMandoob()
    {
        Test.Click(AssignMandoobButton);
        Test.WaitForPageLoad();
    }

    public bool IsSuccess()
    {
        return Test.IsElementPresent(SuccessMessage);
    }
}

public class AdminAuditLogsPage : BasePage
{
    public AdminAuditLogsPage(Infrastructure.TestBase test) : base(test) { }

    private static By LogRows => By.CssSelector("table tbody tr, .log-row");
    private static By ActionFilter => By.Id("action");
    private static By TableFilter => By.Id("table");
    private static By FilterButton => By.CssSelector("button[type='submit'], .btn-filter");
    private static By JsonOldValues => By.CssSelector(".old-values, .json-old");
    private static By JsonNewValues => By.CssSelector(".new-values, .json-new");
    private static By PaginationLinks => By.CssSelector(".pagination a, .page-link");
    private static By EmptyMessage => By.CssSelector(".empty-message, .no-data");

    public void GoTo()
    {
        NavigateTo("/Admin/AuditLogs");
        Test.WaitForPageLoad();
    }

    public int GetLogCount()
    {
        return Test.Driver.FindElements(LogRows).Count;
    }

    public void FilterByAction(string action)
    {
        Test.SelectDropdown(ActionFilter, action);
        Test.Click(FilterButton);
        Test.WaitForPageLoad();
    }

    public void FilterByTable(string table)
    {
        Test.SelectDropdown(TableFilter, table);
        Test.Click(FilterButton);
        Test.WaitForPageLoad();
    }

    public bool HasLogs()
    {
        return GetLogCount() > 0;
    }

    public bool IsEmpty()
    {
        return Test.IsElementPresent(EmptyMessage);
    }

    public bool HasPagination()
    {
        return Test.IsElementPresent(PaginationLinks);
    }
}

public class AdminNotificationsPage : BasePage
{
    public AdminNotificationsPage(Infrastructure.TestBase test) : base(test) { }

    private static By NotificationItems => By.CssSelector(".notification-item, .notif-row, li");
    private static By MarkReadButton(int index) => By.CssSelector($".notification-item:nth-child({index + 1}) .mark-read, .mark-read-{index}");
    private static By MarkAllReadButton => By.CssSelector(".mark-all-read, .btn-mark-all");
    private static By EmptyMessage => By.CssSelector(".empty-message, .no-data");
    private static By NotificationBadge => By.CssSelector(".notification-badge, .badge");

    public void GoTo()
    {
        NavigateTo("/Admin/Notifications");
        Test.WaitForPageLoad();
    }

    public int GetNotificationCount()
    {
        return Test.Driver.FindElements(NotificationItems).Count;
    }

    public void MarkAsRead(int index = 0)
    {
        Test.Click(MarkReadButton(index));
        Test.WaitForPageLoad();
    }

    public void MarkAllAsRead()
    {
        Test.Click(MarkAllReadButton);
        Test.WaitForPageLoad();
    }

    public bool IsEmpty()
    {
        return Test.IsElementPresent(EmptyMessage);
    }

    public bool HasBadge()
    {
        return Test.IsElementPresent(NotificationBadge);
    }
}

public class ProjectPage : BasePage
{
    public ProjectPage(Infrastructure.TestBase test) : base(test) { }

    private static By ProjectRows => By.CssSelector("table tbody tr, .project-row");
    private static By CreateButton => By.LinkText("إضافة مشروع");
    private static By NameInput => By.Id("Name");
    private static By StartDateInput => By.Id("StartDate");
    private static By EndDateInput => By.Id("EndDate");
    private static By RequiredCountInput => By.Id("RequiredCount");
    private static By DescriptionInput => By.Id("Description");
    private static By StatusSelect => By.Id("Status");
    private static By SaveButton => By.CssSelector("button[type='submit'], .btn-save");
    private static By ViewButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-view, .view-btn-{index}");
    private static By EditButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-edit, .edit-btn-{index}");
    private static By DeleteButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-delete, .delete-btn-{index}");
    private static By SectorQuotaSector(int index) => By.Id($"SectorQuotas_{index}_SectorId");
    private static By SectorQuotaMax(int index) => By.Id($"SectorQuotas_{index}_MaxCount");
    private static By AddQuotaButton => By.CssSelector(".add-quota, .btn-add-quota");
    private static By SuccessMessage => By.CssSelector(".alert-success");
    private static By EmptyMessage => By.CssSelector(".empty-message, .no-data");

    public void GoTo()
    {
        NavigateTo("/Project");
        Test.WaitForPageLoad();
    }

    public void GoToCreate()
    {
        NavigateTo("/Project/Create");
        Test.WaitForPageLoad();
    }

    public void GoToEdit(int id)
    {
        NavigateTo($"/Project/Edit/{id}");
        Test.WaitForPageLoad();
    }

    public int GetProjectCount()
    {
        return Test.Driver.FindElements(ProjectRows).Count;
    }

    public void FillCreateForm(string name, string startDate, string endDate, int requiredCount,
        string description, string status = "Draft")
    {
        Test.Type(NameInput, name);
        Test.Type(StartDateInput, startDate);
        Test.Type(EndDateInput, endDate);
        Test.Type(RequiredCountInput, requiredCount.ToString());
        Test.Type(DescriptionInput, description);
        Test.SelectDropdown(StatusSelect, status);
    }

    public void AddSectorQuota(int sectorIndex, string sectorName, int maxCount)
    {
        if (sectorIndex > 0)
            Test.Click(AddQuotaButton);
        Test.SelectDropdown(SectorQuotaSector(sectorIndex), sectorName);
        Test.Type(SectorQuotaMax(sectorIndex), maxCount.ToString());
    }

    public void ClickSave()
    {
        Test.Click(SaveButton);
        Test.WaitForPageLoad();
    }

    public void ClickView(int index = 0)
    {
        Test.Click(ViewButton(index));
        Test.WaitForPageLoad();
    }

    public void ClickEdit(int index = 0)
    {
        Test.Click(EditButton(index));
        Test.WaitForPageLoad();
    }

    public void ClickDelete(int index = 0)
    {
        Test.Click(DeleteButton(index));
        Test.WaitForPageLoad();
    }

    public bool IsSuccess()
    {
        return Test.IsElementPresent(SuccessMessage);
    }

    public bool IsEmpty()
    {
        return Test.IsElementPresent(EmptyMessage);
    }
}

public class NominationPage : BasePage
{
    public NominationPage(Infrastructure.TestBase test) : base(test) { }

    private static By PersonSearchInput => By.Id("personSearch");
    private static By SearchButton => By.CssSelector(".btn-search-person, button:contains('بحث')");
    private static By PersonSearchResults => By.CssSelector(".search-results, .person-results");
    private static By AddPersonButton => By.CssSelector(".btn-add-person, .add-nomination");
    private static By NominationRows => By.CssSelector("table tbody tr, .nomination-row");
    private static By PersonIdInput => By.Id("PersonId");
    private static By DescriptionInput => By.Id("Description");
    private static By StatusSelect => By.Id("Status");
    private static By SaveButton => By.CssSelector("button[type='submit'], .btn-save");
    private static By DeleteButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-delete, .delete-btn-{index}");
    private static By QuotaInfo => By.CssSelector(".quota-info, .sector-quota");
    private static By ErrorMessage => By.CssSelector(".alert-danger, .text-danger");
    private static By EmptyMessage => By.CssSelector(".empty-message, .no-data");
    private static By DuplicateWarning => By.CssSelector(".duplicate-warning, .alert-warning");

    public void GoTo(int projectId)
    {
        NavigateTo($"/Nomination/Index?projectId={projectId}");
        Test.WaitForPageLoad();
    }

    public void SearchPerson(string query)
    {
        Test.Type(PersonSearchInput, query);
        Test.Click(SearchButton);
        WaitForAjax();
    }

    public bool HasSearchResults()
    {
        return Test.IsElementPresent(PersonSearchResults);
    }

    public void SelectFirstSearchResult()
    {
        Test.Click(PersonSearchResults);
        WaitForAjax();
    }

    public void AddNomination(int personId, string description = "")
    {
        Test.Type(PersonIdInput, personId.ToString());
        if (!string.IsNullOrEmpty(description))
            Test.Type(DescriptionInput, description);
        Test.Click(SaveButton);
        Test.WaitForPageLoad();
    }

    public int GetNominationCount()
    {
        return Test.Driver.FindElements(NominationRows).Count;
    }

    public void DeleteNomination(int index = 0)
    {
        Test.Click(DeleteButton(index));
        Test.WaitForPageLoad();
    }

    public bool HasError()
    {
        return Test.IsElementPresent(ErrorMessage);
    }

    public bool HasDuplicateWarning()
    {
        return Test.IsElementPresent(DuplicateWarning);
    }

    public bool IsEmpty()
    {
        return Test.IsElementPresent(EmptyMessage);
    }

    public bool HasQuotaInfo()
    {
        return Test.IsElementPresent(QuotaInfo);
    }
}

public class AssistancePage : BasePage
{
    public AssistancePage(Infrastructure.TestBase test) : base(test) { }

    private static By AssistanceRows => By.CssSelector("table tbody tr, .assistance-row");
    private static By CreateButton => By.LinkText("إضافة مساعدة");
    private static By NameInput => By.Id("Name");
    private static By AssistanceTypeInput => By.Id("AssistanceType");
    private static By SourceInput => By.Id("Source");
    private static By AssistanceDateInput => By.Id("AssistanceDate");
    private static By DescriptionInput => By.Id("Description");
    private static By SectorSelect => By.Id("SectorId");
    private static By SaveButton => By.CssSelector("button[type='submit'], .btn-save");
    private static By DetailsButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-details, .details-btn-{index}");
    private static By EditButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-edit, .edit-btn-{index}");
    private static By DeleteButton(int index) => By.CssSelector($"tr:nth-child({index + 1}) .btn-delete, .delete-btn-{index}");
    private static By ApproveButton => By.CssSelector(".btn-approve, a:contains('موافقة')");
    private static By CancelButton => By.CssSelector(".btn-cancel, a:contains('إلغاء')");
    private static By AddBeneficiaryButton => By.CssSelector(".btn-add-beneficiary, a:contains('إضافة مستفيد')");
    private static By BeneficiaryFullName => By.Id("FullName");
    private static By BeneficiaryNationalId => By.Id("NationalId");
    private static By BeneficiaryPhone => By.Id("Phone");
    private static By BeneficiarySaveButton => By.CssSelector("button[type='submit'], .btn-save-beneficiary");
    private static By BeneficiaryRows => By.CssSelector(".beneficiary-row, table tbody tr");
    private static By ImportButton => By.CssSelector("a:contains('استيراد'), .btn-import");
    private static By DownloadTemplateButton => By.CssSelector("a:contains('تحميل القالب'), .btn-template");
    private static By ImportFileInput => By.CssSelector("input[type='file']");
    private static By ImportSubmitButton => By.CssSelector("button[type='submit'], .btn-upload");
    private static By ExportBeneficiariesButton => By.CssSelector("a:contains('تصدير'), .btn-export");
    private static By SuccessMessage => By.CssSelector(".alert-success");
    private static By ErrorMessage => By.CssSelector(".alert-danger");
    private static By EmptyMessage => By.CssSelector(".empty-message, .no-data");
    private static By ImportHistoryLink => By.LinkText("سجل الاستيراد");

    public void GoTo()
    {
        NavigateTo("/Assistance");
        Test.WaitForPageLoad();
    }

    public void GoToCreate()
    {
        NavigateTo("/Assistance/Create");
        Test.WaitForPageLoad();
    }

    public void GoToEdit(int id)
    {
        NavigateTo($"/Assistance/Edit/{id}");
        Test.WaitForPageLoad();
    }

    public void GoToDetails(int id)
    {
        NavigateTo($"/Assistance/Details/{id}");
        Test.WaitForPageLoad();
    }

    public int GetAssistanceCount()
    {
        return Test.Driver.FindElements(AssistanceRows).Count;
    }

    public void FillCreateForm(string name, string type, string source, string date,
        string description, string sector)
    {
        Test.Type(NameInput, name);
        Test.Type(AssistanceTypeInput, type);
        Test.Type(SourceInput, source);
        Test.Type(AssistanceDateInput, date);
        Test.Type(DescriptionInput, description);
        Test.SelectDropdown(SectorSelect, sector);
    }

    public void ClickSave()
    {
        Test.Click(SaveButton);
        Test.WaitForPageLoad();
    }

    public void ClickDetails(int index = 0)
    {
        Test.Click(DetailsButton(index));
        Test.WaitForPageLoad();
    }

    public void ClickEdit(int index = 0)
    {
        Test.Click(EditButton(index));
        Test.WaitForPageLoad();
    }

    public void ClickDelete(int index = 0)
    {
        Test.Click(DeleteButton(index));
        Test.WaitForPageLoad();
    }

    public void ClickApprove()
    {
        Test.Click(ApproveButton);
        Test.WaitForPageLoad();
    }

    public void ClickCancel()
    {
        Test.Click(CancelButton);
        Test.WaitForPageLoad();
    }

    public void ClickAddBeneficiary()
    {
        Test.Click(AddBeneficiaryButton);
        WaitForAjax();
    }

    public void AddBeneficiary(string fullName, string nationalId, string phone)
    {
        Test.Type(BeneficiaryFullName, fullName);
        Test.Type(BeneficiaryNationalId, nationalId);
        Test.Type(BeneficiaryPhone, phone);
        Test.Click(BeneficiarySaveButton);
        Test.WaitForPageLoad();
    }

    public int GetBeneficiaryCount()
    {
        return Test.Driver.FindElements(BeneficiaryRows).Count;
    }

    public void GoToImport()
    {
        NavigateTo("/Assistance/Import");
        Test.WaitForPageLoad();
    }

    public void ImportExcel(string filePath)
    {
        Test.Type(ImportFileInput, filePath);
        Test.Click(ImportSubmitButton);
        Test.WaitForPageLoad();
    }

    public void ClickDownloadTemplate()
    {
        Test.Click(DownloadTemplateButton);
    }

    public void ClickExportBeneficiaries()
    {
        Test.Click(ExportBeneficiariesButton);
    }

    public bool IsSuccess()
    {
        return Test.IsElementPresent(SuccessMessage);
    }

    public bool HasError()
    {
        return Test.IsElementPresent(ErrorMessage);
    }

    public bool IsEmpty()
    {
        return Test.IsElementPresent(EmptyMessage);
    }
}
