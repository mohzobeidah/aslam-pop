# AGENTS.md — Camp Registration (ASP.NET Core MVC)

## Overview
Camp family registration system implemented as an **ASP.NET Core MVC** application (`CampRegistrationApp/`): .NET 10, SQL Server (LocalDB), Entity Framework Core, full admin/nomination/project/report/financial/complaint system.

## How to run / deploy
### ASP.NET Core
- Build: `dotnet build`
- Run: `dotnet run --project CampRegistrationApp/CampRegistrationApp.csproj`
- Test: `dotnet test`
- Ports: HTTP `localhost:5392`, HTTPS `localhost:7126`
- Production: CI/CD via `.github/workflows/publish.yml`, Azure deploy via `.deployment`

### Playwright E2E Tests (`CampRegistrationApp.PlaywrightTests/`)
- **Setup**: `cd CampRegistrationApp.PlaywrightTests && npm install && npx playwright install chromium`
- **Run**: `BASE_URL=http://localhost:5392 npx playwright test` (app must be running on that URL)
- **Run headed (debug)**: `BASE_URL=http://localhost:5392 npx playwright test --headed`
- **HTML report**: After run, open `playwright-report/index.html`
- **Tests**: 30 test files covering registration, admin CRUD, approvals, rejections, edit, record login/password, projects/nominations, assistance, reports, complaints, notifications, audit logs, sectors, dashboard, authorization, file download, error pages.
- **Helpers**: Valid Palestinian ID generation, selector constants
- Requires the ASP.NET Core app to be running

## ASP.NET Core Architecture
### Data Model
- **Person**: Shared entity for family head and members (4-part name, ID, DOB, gender, health, maternity, prisoner flag, BathroomStatus, MotherIdNumber, etc.). Sector, PhoneNumber, Wallet/WalletType are on FamilyRegistration, not Person.
- **FamilyRegistration**: Links to head (Person), members (FamilyMembers), housing/special-case fields (HasBathroom, BathroomType, BathroomStatus), sector, phone, wallet, approval workflow, and **Refugee Desires** (ranked dropdowns stored as FamilyDesire join records). Has soft delete fields: `IsDeleted`, `DeletedById`, `DeletedAt`.
- **FamilyMember**: Join table `FamilyRegistration → Person` with `RelationshipToHead`.
- **Attachment**: File metadata (`MedicalReport` or `IDImage`), stores relative paths.
- **Admin**: Login with `AdminRole` (`Admin`=super, `Mandoob`=sector-limited), session-based auth, SHA256 password hashing.
- **Sector**: Camp sector with name, camp, coordinates, area, tent/bathroom counts.
- **Project**: Aid campaign with Name, StartDate, EndDate, RequiredCount, Status (Draft/Active/Closed), soft delete, rowversion concurrency. Created with per-sector quotas (`ProjectSectorQuota`).
- **Nomination**: Links a Person to a Project via Sector with quota enforcement, soft delete, rowversion concurrency.
- **AuditLog**: Immutable audit trail with JSON old/new values and automatic source tracking (Web/Mobile).
- **Notification**: Per-admin notification system with bell icon polling.

### Registration Flow (4 Steps + Submit)
1. Step 1: Family Head info + health + documents + injury (always visible) + BathroomStatus
2. Step 2: Family Members (dynamic add/remove). Validation: if MaritalStatus=متزوج, at least one member must have RelationshipToHead=زوجة.
3. Step 3: Housing & Special Cases + **Bathroom section** (HasBathroom, BathroomType, BathroomStatus) + **Refugee Desires** (الرغبات — ranked dropdowns)
4. Step 4: Review & Confirm + **StatusNotes** textarea for additional notes + password creation

### Admin Edit
- **AdminEditRegistration** GET: loads any registration (Pending/Approved/Rejected), reuses `Record/Edit.cshtml` view
- **AdminUpdateRegistration** POST: saves changes (head, members, desires) with audit log (via `RegistrationChangeTracker`) + mandoob notification
- Accessible for all statuses (Approved/Rejected are no longer blocked)
- Edit button shown in `RefugeeDetails` page, `Registrations` list, and `Refugees` list for all statuses

### Refugee Desires
- Ranked dropdowns (الرغبة رقم 1, 2, ...) populated from `Desires` DB table
- Each select corresponds to a rank position; selections are mutually exclusive per rank
- Stored as `FamilyDesire` join records with Order (rank position) and DesireId
- **Model binding**: Before submit, comma-separated hidden input is converted to indexed inputs (`DesireIds[0]`, `DesireIds[1]`, ...) for proper `List<int>` binding

### Admin Change Password
- **`GET/POST /Admin/ChangePassword`**: Self-service password change for admins
- **Force change on login**: If admin password matches their national ID, they are redirected to ChangePassword on login (`RequiresPasswordChange()` check)
- **Same password guard**: New password must differ from national ID
- **Layout nav**: "تغيير كلمة المرور" link in desktop and mobile nav bars
- **Audit**: Logged as `ChangePassword` action with forced vs voluntary distinction

### Refugee Force Password Change
- **`GET/POST /Record/ChangePassword`**: Force change if refugee password equals their ID number
- On login (`RecordController.Login`): sets `MustChangePassword` session flag when password == ID
- Blocks `Edit` and `UploadFile` actions until password is changed
- `ReturnEditWithPasswordChangeRequired()` helper shows edit view with forced change banner

### Soft Delete / Remove from Camp
- **RemoveRefugee** (`AdminController.RemoveRefugee`): POST action for approved registrations only — sets `IsDeleted = true`, `DeletedById`, `DeletedAt`
- **Global query filter**: `HasQueryFilter(f => !f.IsDeleted)` on `FamilyRegistration` — all normal queries exclude deleted records
- **DeletedRegistrations page** (`AdminController.DeletedRegistrations`): Lists soft-deleted registrations using `.IgnoreQueryFilters().Where(f => f.IsDeleted)`, with sector filter, shows who deleted and when
- **RestoreRegistration** (`AdminController.RestoreRegistration`): POST action — sets `IsDeleted = false`, clears `DeletedById`/`DeletedAt`, **requires reason** (min 3 chars, validated server-side)
- **Restore modal**: Popup with reason textarea, identical pattern to the rejection modal in `Registrations.cshtml`
- **Audit**: Both removal and restoration are logged with full details (who, when, reason)
- **Nav**: "المحذوفة" link in both desktop and mobile nav bars, and in the dashboard action bar (super admin)
- **CanAccessRegistrationAsync fix**: Uses `.IgnoreQueryFilters()` so ماندوب can access their sector's deleted registrations

### Error Pages (Unified)
- **`HomeController.Error()`**: Accepts optional `?statusCode=` parameter — renders themed error page for 404, 403, 401, 400, and 500+
- **`Program.cs`**: `UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}")` intercepts all HTTP errors
- **`Error.cshtml`**: Unified view with:
  - Large color-coded status code number (yellow=404, orange=403, red=500)
  - Distinct SVG icons per error type (magnifying glass, prohibition, lock, warning triangle)
  - Tailored Arabic messages for each status
  - Request ID displayed only for server errors (500+)
  - "إبلاغ عن المشكلة" button only for server errors
  - "العودة للرئيسية" button on all pages
- **`Forbid()` issue**: `StatusCode(403)` used instead of `Forbid()` — the app has no `AddAuthentication` middleware configured

### Rejection with Reason
- **Rejection requires reason**: `RejectRegistration` POST now requires a `reason` parameter (validated server-side, min 3 chars)
- **UI**: Modal popup with reason textarea on both `Registrations` list and `RefugeeDetails` page
- **Fields**: `FamilyRegistration.RejectedById`, `RejectedAt`, `RejectionReason` (stored in DB)
- **Display**: Rejection info shown in `Registrations` list (name, date, reason preview with tooltip) and `RefugeeDetails` page
- **Re-approve clears rejection**: Approving a previously-rejected registration clears `RejectedById`, `RejectedAt`, `RejectionReason` and adds previous rejection data to audit log
- **Audit**: Full rejection details (reason, rejecter name, timestamp) logged in audit

### Complaint / Ticket System
- **Complaint System**: Fully implemented for both admin and refugee users.
- **Refugee side** (`RecordController`): `MyComplaints` — list personal complaints, `MyComplaintDetails` — view details/replies, `CreateComplaint` — submit new complaint with subject, description, attachment (ticket-based).
- **Admin side** (`ComplaintController`): `Index` — filterable grid (status, date range), `Details` — view complaint + respond, `Respond` — update status (Open/InProgress/Resolved/Closed), `Delete` — soft delete.
- **Model**: `Complaint` entity — `TicketId` (unique 8-char ID), Subject, Description, AttachmentPath, `FamilyRegistrationId` (optional link), `Status` (Pending/InProgress/Resolved/Closed), `AdminResponse`, `ResolvedById`, `ResolvedAt`.
- **Id Generator**: `ComplaintIdGenerator` creates unique ticket IDs (same charset as Record IDs).
- **Database**: `Complaints` table created via raw SQL `IF NOT EXISTS` in `Program.cs`. Has indexes on `TicketId` (unique) and `FamilyRegistrationId`.
- **Views**: `Views/Complaint/{Index,Details,Create,Confirmation}.cshtml` + `Views/Record/MyComplaints.cshtml`.
- **Playwright tests**: `tests/complaint.spec.ts` covers create, list, respond, delete flows.

### RegistrationChangeTracker (Detailed Audit Diffs)
- **`Services/RegistrationChangeTracker.cs`**: Static utility for before/after snapshots of registration data
- **`Snapshot`**: Captures head fields, registration fields, member fields, and desires before edit
- **`BuildDiffAsync()`**: Compares snapshot against `RegistrationViewModel` after changes, categorizes into:
  - Head changes (field-level old/new)
  - Registration changes (sector name resolution, field-level)
  - Members added/removed/modified (per-field diffs)
  - Desires changed (old vs new list)
- **Usage**: Both `RecordController.Update` (refugee edit) and `AdminController.AdminUpdateRegistration` (admin edit) use it
- **Audit payload**: `ToAuditPayload()` produces structured JSON with Arabic field labels, categorized by section (رب الأسرة, بيانات التسجيل, أفراد مضافون/محذوفون/معدّلون, الرغبات)
- **Empty diff handling**: If no fields changed, audit still logs "(بدون تغييرات فعلية)"

### Report Audit Logging
- **`ReportQueryDescriptor`**: Builds human-readable SQL-like query description for audit
- Both `Preview` and `ExportExcel` actions log to `AuditLog` with:
  - Action type: `PreviewReport` or `ExportExcel`
  - Filter parameters (SectorId, Status, Gender, HealthStatus, Search, Age range, NeedsDiapers)
  - Selected columns with Arabic labels
  - SQL-like WHERE clause summary
  - Row count
- **AdminSectorId** stored in session (login) for ماندوب sector-scoped reports

### Report System Improvements
- **`NeedsDiapers` filter**: New checkbox filter in report UI, filters registrations where `NeedsDiapers == true`
- **`DisplayColumns`**: `ReportViewModel` now has `DisplayColumns` (key + label) for consistent ordering between grid preview and Excel export
- **Dynamic key ordering**: Wife/Child member columns sorted by consistent `FieldOrder` (Name, IdNumber, DOB, Age, Gender, HealthStatus, etc.)
- **Excel export form**: Uses JavaScript to copy selected columns and filters from the preview form on submit (avoids stale filter values)
- **`NormalizeBathroomType()`**: Applied via `RegistrationConstants` in both `MapToViewModel` and `Update` to handle null/empty bathroom type

### Client-Side Validation (Registration wizards)
- **Step 1 check** (`validateStep1`): 13 required text/select fields + 2 radio groups (Gender, HealthStatus): FirstName, SecondName, ThirdName, LastName, IdNumber, Sector, DateOfBirth, PhoneNumber, Wallet, OriginalGovernorate, MaritalStatus, EmploymentStatus, EducationLevel. Field-specific Arabic error messages listed in alert.
- **Step 2 check** (`validateStep2`): Per member: FirstName, IdNumber, RelationshipToHead, Gender, DateOfBirth required. Also checks wife requirement if head married, and sick members require at least one disease/disability. Checks no duplicate ID numbers within the family.
- **Step 3 check** (`validateStep3`): All desire selects required. Conditionally: if LivesInTent=true, TentType required; if HasBathroom=true, BathroomType + BathroomStatus required.
- **Step 4 check** (`submitForm`): Calls `validateStep1() || validateStep2() || validateStep3()` before submitting. Also validates password (4+ chars, match), accept responsibility checkbox, ID/phone/wallet format.
- **Edit.cshtml** (`submitEditForm`): Same field-specific validation as Index.cshtml; uses `highlightError`/`clearHighlight` helpers.
- `highlightError(el)` adds `.field-error` class (red border); `clearHighlight(el)` removes it.

## Key Fixes Applied

### MemberViewModel — Sector/PhoneNumber/Wallet removed
- `MemberViewModel` is now a **standalone class** (no longer inherits `PersonViewModel`).
- Sector, PhoneNumber, and Wallet are only for the family head, never for members.
- Controllers no longer save/map these fields for member `Person` records.

### Duplicate ID Number Validation
- **Server-side** (`RegistrationController.Submit`, `RecordController.Update`, `AdminController.AdminUpdateRegistration`):
  - Checks for duplicates within the submission (head vs members, member vs member)
  - Checks each ID against the DB for existing persons
  - In update flows, excludes the current head's ID from duplicate check
- **Client-side** (`validateStep2` in Index.cshtml, `submitEditForm` in Edit.cshtml):
  - Prevents adding members with the same ID as the head or another member

### Palestinian ID Check Digit Validation
- `validatePalestinianId(id)` validates both format (9 digits) and check digit:
  - Weights: 1, 2, 1, 2, 1, 2, 1, 2 on first 8 digits
  - For products >= 10, sum the individual digits
  - Check digit = `(10 - (sum % 10)) % 10`
  - Compared with the 9th digit

### Assistance System (Beneficiary Management)
- **Assistance**: Aid campaign with Name, Type, Source, Date, Sector, Status (Draft/Approved/Cancelled), soft delete.
- **AssistanceBeneficiary**: Standalone beneficiary record (not linked to Person entity) with FullName, NationalId, Phone, SectorId, BenefitType, Notes, Status.
- **Add Beneficiary**: Search by NationalId/name on Details page → selects from existing `Person` records (refugees + family members) → adds with one click (`AddBeneficiaryFromPerson`). No manual data entry.
- **Import**: Excel (.xlsx) bulk import with template download, duplicate NationalId detection per assistance, error reporting.
- **Export**: Download beneficiary list as Excel from Details page.
- **Controller**: `AssistanceController` with CRUD + `SearchPerson`/`AddBeneficiaryFromPerson`/`DeleteBeneficiary` + `Import`/`ExportBeneficiaries`.
- **Service**: `AssistanceService` handles business logic including `SearchPersonsAsync` and `AddBeneficiaryFromPersonAsync` (copies Person data → beneficiary); `ImportService` handles Excel parsing.

### MotherIdNumber Validation
- Added field to `Edit.cshtml` member template (was missing entirely)
- Added `onblur="validateMotherIdField(this)"` + `oninput="clearMotherIdError(this)"` + error span in both Index.cshtml and Edit.cshtml
- If provided, must be a valid Palestinian ID (9 digits + check digit); empty is allowed (optional field)

### WalletType Validation
- Server-side (`RegistrationValidationService.ValidateRegistration`): If `Wallet` is provided, `WalletType` is required
- `WalletType` dropdown options: بنك, بال بي, جوال بي
- Client-side: added to `validateStep1` in Index.cshtml
- Edit.cshtml also includes the WalletType field with the same validation

### Dashboard CTE (Demographic Statistics)
- `AdminController.Dashboard()` now uses a raw SQL CTE query instead of simple LINQ
- The `SectorDashboard` view model has 37 properties including detailed age/gender/disability breakdowns
- Categories: children under 2, 2-5, under 18; adults 18-60; elderly 60+; male/female disaggregation
- Disabled and chronic sickness counts per gender
- Soft-deleted registrations (`IsDeleted = 1`) are excluded
- Unit tests for Dashboard are skipped (require SQL Server — `SqlQueryRaw` unsupported by InMemory provider)

### NullReferenceException in Record Update
- Fixed crash in `Edit.cshtml` during POST `/Record/Update` when model binding fails.
- Added logic to recover `RegistrationViewModel` from session and ensure `ViewBag.HeadAttachments` are populated when returning to the view.

### Production Error Page
- Implemented a professional, themed error view at `/Home/Error` for non-development environments.
- Unified page handles 404, 403, 401, 400, and 500+ errors with color-coded icons and tailored Arabic messages.
- `UseStatusCodePagesWithReExecute` intercepts all HTTP errors before they reach the browser.
- `Forbid()` replaced with `StatusCode(403)` since no `AddAuthentication` middleware is configured.

### Report System (Dynamic Excel Reporting)
- **Controllers/ReportController.cs**: GET Index (column selection + filters), POST Preview (renders data grid), POST ExportExcel (generates .xlsx).
- **Services/IReportService.cs + ReportService.cs**: Column definitions (7 groups, 50+ columns), two query modes:
  - `GetFamilyReportAsync` — one row per family, dynamic wife/child expansion
  - `GetPersonReportAsync` — one row per person for Disabled/ChronicSick/Pregnant/Nursing reports
  - `GenerateExcelAsync` — Excel via ClosedXML with Arabic headers
- **Models/ViewModels/ReportViewModel.cs**: `ColumnGroup`, `ColumnDef`, `ReportFilter`, `ReportRow` (Dictionary-based), `HeaderLabels` for Arabic column display.
- **Views/Report/Index.cshtml**: Left panel column checkboxes (group toggles), right panel filters (Sector, Status, Gender, HealthStatus, Age range, Search, IncludeMembers), preview table (max 50 rows), export button.
- **Nav**: "التقارير" link added to `_Layout.cshtml` desktop + mobile nav between "لوحة التحكم" and "سجل التدقيق".
- **Column ordering**: Grid collects column keys from ALL rows (not just first). Excel uses same insertion order (no `OrderBy` on Wife/Child keys) to match grid exactly.
- **Excel Arabic headers**: Table preview and Excel export both use Arabic column labels via `HeaderLabels` dictionary + `GenerateDynamicLabel` for Wife/Child/OtherMembers keys.

### Health/Disease Contradiction Validation
- **Client-side** (`toggleHealthSection` + `toggleMemberHealth` in Index.cshtml, Edit.cshtml): When user switches HealthStatus to "سليم", all disease/disability checkboxes are automatically cleared (unchecked).
- **Server-side** (`RegistrationValidationService.ValidateRegistration`): Rejects submission if HealthStatus = "سليم" with ChronicDiseases or DisabilityTypes present.
- Rationale: A person cannot be "سليم" (healthy) and have diseases at the same time.

### Nomination System
- **Nomination**: Links a `Person` to a `Project` via `Sector`. Soft-delete with `IsDeleted` flag.
- **Single Add** (`NominationController.AddRow`): Searches for any person (head or family member), adds nomination with sector lookup (head via `FamilyRegistration.Sector`, member via `FamilyMember→Registration→Sector`).
- **Bulk Add** (`NominationController.AddMultipleRows`): Selects multiple family heads from a full list with checkbox selection, sector filter, and search filter.
- **Excel Import** (`NominationController.ImportExcel`):
  - Uploads `.xlsx` with `NationalId` and `Notes` columns.
  - Accepts both **heads of family** (رب أسرة) and **family members** (أفراد عائلة).
  - For non-heads, adds a **warning** (`BulkImportResult.Warnings`) displayed as a blue info banner; sector is looked up via `FamilyMembers` join table.
  - Previously-skipped persons (already nominated, quota exceeded, not found, no sector) still produce **errors** displayed as a yellow banner.
  - Template download available via `DownloadImportTemplate`.
- **Delete Single** (`NominationController.DeleteRow`): Soft-deletes a single nomination row with audit log.
- **Delete All** (`NominationController.DeleteAllNominations`): Super-admin only — soft-deletes **all** nominations for a project with confirmation dialog. Logs count and project name to audit.
  - **UI**: Red "حذف الكل" button appears above the table when `IsAdmin == true` and rows exist.
- **Export Excel** (`NominationController.ExportExcel`): Downloads nomination list as `.xlsx` with family details (wives, housing, damage type). Logs to audit.
- **Model**: `Nomination` entity with `ProjectId`, `PersonId`, `SectorId`, `DelegateId`, `Status` (Draft/Submitted/Approved/Rejected/Cancelled), `IsDeleted`, `RowVersion`.
- **Service**: `INominationService` / `NominationService` — handles all business logic, quota enforcement, person search.
- **Quota enforcement**: `ProjectSectorQuota` table limits nominations per sector per project. Quota check applies in single add, bulk add, and Excel import.

### Nomination Audit Logging — Names not Numbers
- All nomination audit log entries (`AddNomination`, `AddMultipleNominations`, `DeleteNomination`, `ImportNominationsExcel`, `DeleteAllNominations`) must log **names** (project name, person name, sector name, admin name) instead of numeric IDs
- Logs use Arabic property names for `NewValues` (e.g. `المشروع`, `الشخص`, `القطاع`, `المسؤول`)
- Each entry fetches the related entity name from DB before logging, never logs raw `projectId`, `personId`, `sectorId` alone
- `RecordId` includes human-readable description like `المشروع:{projectName},الشخص:{personName}`

## Key Conventions
- All UI text in Arabic, RTL layout, Cairo font, dark theme (`#121212` + `#d4af37` gold).
- 8-char Record ID from charset `23456789ABCDEFGHJKLMNPQRSTUVWXYZ`.
- No input sanitization; no migrations (`EnsureCreated()` + raw SQL).
