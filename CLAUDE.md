# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
This repository contains two versions of a Camp Family Registration application:
1. A Google Apps Script (GAS) web app (root directory `Code.gs` + `Index.html`).
2. An **ASP.NET Core MVC** application (`CampRegistrationApp/`).

## Common Commands
### ASP.NET Core (.NET 10)
- Build: `dotnet build`
- Run: `dotnet run --project CampRegistrationApp/CampRegistrationApp.csproj`
- Test: `dotnet test`

### Google Apps Script
- No local build/test tools.
- Deployment: Use the Google Apps Script editor (Extensions > Apps Script > Deploy > New deployment).
- Testing: Open the deployed web app URL or run `doGet()` in the GAS editor.

## Architecture
### Google Apps Script Version
- **Frontend**: `Index.html` (HTML5, Tailwind CSS, JS) using RTL layout and Cairo font.
- **Backend**: `Code.gs` (JavaScript/GAS) handling server-side logic, Google Sheets integration, and Google Drive file uploads.
- **Data Store**: Google Sheets (as a database) and Google Drive (for file storage).
- **Communication**: Client-to-server via `google.script.run`.

### ASP.NET Core Version (`CampRegistrationApp/`)
- **Framework**: .NET 10 MVC with Razor runtime compilation.
- **Structure**: Standard MVC pattern with `Controllers/`, `Models/`, and `Views/`.
- **Database**: SQL Server (LocalDB via `localhost\SQLEXPRESS`) via Entity Framework Core 10.
- **CI/CD**: GitHub Actions (`.github/workflows/publish.yml`) + Azure deployment (`.deployment`).
- **Database Name**: `CampRegistrationDb` (auto-created via `EnsureCreated()` on startup).
- **Static Assets**: Managed in `wwwroot/`, uploaded files stored under `wwwroot/uploads/registrations/`.
- **Session**: Used for admin authentication (`AdminId`, `AdminName`, `AdminRole`), refugee edit access (`EditRegistrationId`), and multi-step registration flow.
- **Port**: HTTP on `localhost:5392`, HTTPS on `localhost:7126`.
- **Production Error Page**: Custom themed error page at `/Home/Error` for production environments, displaying the Request ID for troubleshooting.

## ASP.NET Core Data Model (Entity Relationships)
```
Sector (1) ──< (N) Admin
Person (1) ──< (N) Attachment
FamilyRegistration (1) ──< (N) FamilyMember >── (1) Person
FamilyRegistration (1) >── (1) Person (as FamilyHead)
Project (1) ──< (N) Nomination >── (1) Person
Nomination >── (1) Sector, (1) Admin (Delegate), (1?) Admin (ApprovedBy)
Admin (1) ──< (N) Notification
Notification >── (0..1) Link (URL string)
```
- **Person**: Shared entity for both Head of Family and Family Members. Fields include name (4 parts), ID, sector, DOB, gender, phone, governorate, **Wallet (المحفظة)**, **BathroomStatus (جيد/متوسط/سيء)**, marital/employment/education status, health info (diseases, disabilities, injuries), prisoner flag (أسير), optional maternity fields (pregnancy, nursing).
- **FamilyRegistration**: Links to FamilyHead (Person), has list of Members, plus housing/special-case fields (tent, bathroom, child-headed, female-headed, external support, diaper needs, multiple families in tent) and **Refugee Needs** (NeedPriority enum for 7 aid items: Tents, Blankets, Mattresses, KitchenTools, Tarpaulins, Clothes, HygieneKit). Has unique 8-char `RecordId`. Approval workflow with `ApprovalStatus` (Pending/Approved/Rejected).
- **FamilyMember**: Join table linking `FamilyRegistration` → `Person` with a `RelationshipToHead` string.
- **Attachment**: File metadata linked to a Person (`MedicalReport` or `IDImage`), storing relative file paths.
- **Admin**: Login system with `AdminRole` enum (`Admin`=super, `Mandoob`=sector-limited). Linked optionally to a `Sector`.
- **Sector**: Camp sector entity with name, camp, coordinate, area, tent/bathroom counts. Has `Admins` collection.
- **Project**: Campaign/project entity for nominations. Fields: Name, StartDate, EndDate, RequiredCount, Status (Draft/Active/Closed), Notes. Has `CreatedBy` (Admin), soft delete + RowVersion.
- **Nomination**: Links a Person to a Project for a specific Sector. Status (Draft/Submitted/Approved/Rejected/Cancelled). Has `Delegate` (Admin submitter) and optional `ApprovedBy`. Soft delete + RowVersion.
- **AuditLog**: Immutable audit trail with UserId, Action, TableName, RecordId, OldValues/NewValues (JSON), CreatedAt. Includes automatic source tracking (Web/Mobile) via `AuditService`.
- **Notification**: Per-admin notification with Message, Link (URL), IsRead, CreatedAt.

## Navigation & Routes
| Route | Controller | Description |
|---|---|---|
| `/` | `HomeController.Index` | Landing page |
| `/Registration/Index` | `RegistrationController.Index` | Multi-step registration form (4 steps, sequential tab navigation) |
| `/Registration/Submit` | `RegistrationController.Submit` | Final submission, wraps in DB transaction; creates notification for sector mandoobs |
| `/Registration/UploadFile` | `RegistrationController.UploadFile` | AJAX file upload handler |
| `/Registration/CheckId` | `RegistrationController.CheckId` | JSON endpoint — check if ID number already registered |
| `/Record/Search` | `RecordController.Search` | Lookup existing registration by RecordId + head ID |
| `/Record/Find` | `RecordController.Find` | POST handler that finds and redirects to edit |
| `/Record/Login` | `RecordController.Login` | Refugee login page (ID + password) |
| `/Record/Edit` | `RecordController.Edit` | Edit registration data (session-guarded) |
| `/Record/Update` | `RecordController.Update` | Save edited registration data; creates notification for sector mandoobs |
| `/Record/Logout` | `RecordController.Logout` | Clear edit session |
| `/Admin/Login` | `AdminController.Login` | Admin authentication (POST: nationalId + password) |
| `/Admin/Dashboard` | `AdminController.Dashboard` | Admin dashboard with sector stats |
| `/Admin/Index` | `AdminController.Index` | Admin CRUD list (super admin only) |
| `/Admin/Create` | `AdminController.Create` | Create admin (super admin only) |
| `/Admin/Edit/{id}` | `AdminController.Edit` | Edit admin (super admin only) |
| `/Admin/Delete/{id}` | `AdminController.Delete` | Delete admin (POST, super admin only) |
| `/Admin/Sectors` | `AdminController.Sectors` | Sector CRUD list with mandoob names (super admin only) |
| `/Admin/CreateSector` | `AdminController.CreateSector` | Create sector (super admin only) |
| `/Admin/EditSector/{id}` | `AdminController.EditSector` | Edit sector + manage assigned mandoobs (super admin only) |
| `/Admin/AssignMandoob` | `AdminController.AssignMandoob` | POST — assign mandoob to sector |
| `/Admin/RemoveMandoob` | `AdminController.RemoveMandoob` | POST — remove mandoob from sector |
| `/Admin/DeleteSector/{id}` | `AdminController.DeleteSector` | Delete sector (POST, super admin only) |
| `/Admin/Registrations` | `AdminController.Registrations` | Approval workflow list (filter by status/sector) |
| `/Admin/ApproveRegistration/{id}` | `AdminController.ApproveRegistration` | POST — approve registration |
| `/Admin/RejectRegistration/{id}` | `AdminController.RejectRegistration` | POST — reject registration |
| `/Admin/Refugees` | `AdminController.Refugees` | Refugee list page with sector/search filters |
| `/Admin/RefugeeDetails/{id}` | `AdminController.RefugeeDetails` | Full refugee detail page with family member table |
| `/Admin/AdminEditRegistration/{id}` | `AdminController.AdminEditRegistration` | Admin edit refugee profile (only Pending status) |
| `/Admin/AdminUpdateRegistration` | `AdminController.AdminUpdateRegistration` | POST — save admin edits; audit log + mandoob notification |
| `/Admin/Notifications` | `AdminController.Notifications` | Notification list (last 50) |
| `/Admin/MarkNotificationRead/{id}` | `AdminController.MarkNotificationRead` | POST — mark single notification read |
| `/Admin/MarkAllNotificationsRead` | `AdminController.MarkAllNotificationsRead` | POST — mark all notifications read |
| `/Admin/GetNotificationCount` | `AdminController.GetNotificationCount` | JSON — unread notification count (polled by nav bell) |
| `/Project` | `ProjectController.Index` | Project list (admin only) |
| `/Project/Create` | `ProjectController.Create` | Create project (super admin only) |
| `/Project/Edit/{id}` | `ProjectController.Edit` | Edit project (super admin only) |
| `/Project/Delete/{id}` | `ProjectController.Delete` | Delete project (POST, super admin only) |
| `/Project/View/{id}` | `ProjectController.View` | Redirects to Nomination page for project |
| `/Nomination/Index?projectId=` | `NominationController.Index` | Nomination grid — search persons, add/delete rows |
| `/Nomination/SearchPerson` | `NominationController.SearchPerson` | JSON — search persons by ID/name |
| `/Nomination/CheckPersonInProject` | `NominationController.CheckPersonInProject` | JSON — check if person already nominated |
| `/Nomination/AddRow` | `NominationController.AddRow` | POST — add or update nomination row |
| `/Nomination/DeleteRow` | `NominationController.DeleteRow` | POST — soft-delete nomination row |
| `/File/Download` | `FileController.Download` | Serve uploaded files (by relative path) |

## Registration Flow (4 Steps + Submit)
1. **Step 1**: Family Head info (personal, socio-economic, health, injury, BathroomStatus, documents).
2. **Step 2**: Family Members (dynamic add/remove). Validation: if MaritalStatus=متزوج, at least one member must have RelationshipToHead=زوجة. Each member has own health/injury/maternity/docs.
3. **Step 3**: Housing & Special Cases + **Bathroom section** (HasBathroom, BathroomType, BathroomStatus) + **Refugee Desires** (الرغبات — ranked dropdowns populated from `Desires` DB table, mutual exclusivity per rank).
4. **Step 4**: Review & Confirm + **StatusNotes** textarea + **password creation**.

**Navigation**: Steps are sequential — user cannot skip ahead by clicking tabs. `tryGoToStep(step)` validates all prior steps before allowing forward navigation. Going back is always allowed. `nextStep()` delegates to `tryGoToStep()`.

**Submit**: Wraps everything in a DB transaction — creates FamilyHead `Person`, then `FamilyRegistration`, then each Member `Person` + `FamilyMember`. On success, creates notifications for all sector mandoobs.

## Registration ViewModel (`RegistrationViewModel`)
- `Head` (PersonViewModel) + `Members` (List of MemberViewModel) + housing/special-case fields + `StatusNotes` (string) + **Refugee Needs** (7 `int` fields mapped to `NeedPriority` enum).
- `MemberViewModel` is a **standalone class** (does NOT inherit `PersonViewModel`). It has its own fields without Sector, PhoneNumber, or Wallet — these are head-only.
- `CurrentStep` (1-4) tracks wizard progress.
- `UploadedFiles` (List<string>) tracks client-side uploaded file paths.
- `Password` string — set in Step 4, hashed on server.

## Refugee Desires (الرغبات)
- **Featured** instead of the old Refugee Needs: ranked `<select>` dropdowns (الرغبة رقم 1, 2, ...) populated from `Desires` DB table.
- Stored as `FamilyDesire` join records with `Order` (rank position) and `DesireId`.
- **Mutual exclusivity**: Once a desire is selected for rank N, it is hidden/disabled in all later ranks via `updateDesireOptions()`.
- **Model binding**: The form uses a single hidden input `desireIdsInput` with comma-separated IDs. Before submission, `submitForm()` / `submitEditForm()` replaces it with indexed hidden inputs (`DesireIds[0]`, `DesireIds[1]`, ...) for proper `List<int>` binding on the server.

## File Upload Pattern
- **Registration form**: Uses AJAX POST to `/Registration/UploadFile` with `IFormFile`.
- Files saved to `wwwroot/uploads/registrations/TEMP/{personId}_{fileType}_{timestamp}.{ext}`.
- After final submission, files are NOT moved/renamed (TEMP folder persists).
- **Download**: `/File/Download?path=...` serves files with correct MIME types.

## Admin System
- **Password Hashing**: SHA256 (single round, hex-encoded, no salt).
- **Default super admin**: nationalId=`admin`, password=`admin123`.
- **Role-based access**: `Admin` (super) sees all sectors/admins; `Mandoob` sees only their assigned sector.
- **Session-based auth**: No ASP.NET Identity — uses raw `ISession` with `AdminId`, `AdminName`, `AdminRole` keys.
- **Login screen** at `/Admin/Login`, logout clears session.
- **Mandoob assignment**: Super admin can assign mandoobs to sectors via `EditSector` page. Each sector can have multiple mandoobs.

## Notification System
- **Model**: `Notification` — AdminId, Message, Link, IsRead, CreatedAt.
- **Triggers**:
  - New registration (`RegistrationController.Submit`) → notifies all mandoobs in that sector
  - Refugee edits data (`RecordController.Update`) → notifies all mandoobs in that sector
- **Display**: 🔔 bell icon in nav bar with red badge (polled every 30s via `/Admin/GetNotificationCount`)
- **Management**: `/Admin/Notifications` page lists last 50, supports per-item and bulk mark-as-read

## Project / Nomination System
- **Projects**: Created by super admins, have start/end dates, required count, status (Draft/Active/Closed). Soft-deleted.
- **Nominations**: Link a registered Person (refugee) to a Project. Added by mandoobs/delegates per sector.
- **Nomination grid** (`/Nomination/Index?projectId=X`): Search persons by ID/name → add row with sector assignment → table of current nominations with delete.
- **Duplicate check**: Client-side via `/Nomination/CheckPersonInProject` + server-side in `NominationService`.
- **Read-only after end date**: Add-form hidden when project end date has passed.

## Key Conventions
- All UI text is in Arabic; RTL layout (`dir="rtl"`) with Cairo font.
- Dark theme (`#121212` background, gold/dark grey accents).
- Database auto-seeds 4 default sectors (A, B, C, D), 1 super admin, 2 health statuses, 8 chronic diseases, 4 disability types on first run.
- `Program.cs` uses raw SQL `IF NOT EXISTS` to create tables alongside `EnsureCreated()` (workaround for schema updates).
- Record ID: 8 chars from charset `23456789ABCDEFGHJKLMNPQRSTUVWXYZ` (no ambiguous chars).
- `Person.IdNumber` has a unique index — no duplicate ID numbers allowed.
- `FamilyRegistration.RecordId` has a unique index.
- `Admin.NationalId` has a unique index.
- `Sector.Name` has a unique index.
- No migrations — uses `EnsureCreated()` + manual SQL for new tables.
- No try/catch in `Program.cs` startup seeding.
- No logging framework beyond the default ASP.NET Core `ILogger`.
- Soft delete pattern with `IsDeleted` flag + global query filters on Project and Nomination.
- `RowVersion` (SQL Server `rowversion`) for concurrency on Project and Nomination.
- `AuditService` logs JSON-serialized old/new values for Create/Update/Delete operations.

## Limitations / Gotchas
- **No proper file isolation**: All pre-submission uploads go to `TEMP` folder, not per-record folders.
- **Password hashing**: SHA256 without salt is weak — not production-grade.
- **No migrations**: `EnsureCreated()` won't update existing DB schema; uses raw SQL as workaround.
- **Session-based auth**: Lost on server restart; no token/refresh mechanism.
- **ID Validation**: Palestinian ID enforced as 9-digit string with **check digit** (Luhn-like algorithm with weights 1,2,1,2,1,2,1,2). Validation via `validatePalestinianId()` in JS + `[RegularExpression(@"^\d{9}$")]` on ViewModel. Client-side validates on blur + on step navigation + on submit. Invalid IDs show red border + inline error message.
- **Mother ID Validation**: Same Palestinian ID validation as regular ID number (9 digits + check digit), but optional (empty allowed). Validated via `validateMotherIdField()` in both Index.cshtml and Edit.cshtml.
- **Duplicate ID check**: Server-side in all 3 controllers (Submit, Update, AdminUpdateRegistration) — checks both within-family duplicates and against DB. Client-side in validateStep2 / submitEditForm prevents duplicates within the form.
- **Health validation**: If health status is "مريض" (sick), at least one chronic disease or disability must be selected (client-side in validateStep1/validateStep2, server-side in RegistrationController.Submit, RecordController.Update, and AdminController.AdminUpdateRegistration).
- **Phone required**: Phone number is required in the registration form, validated both client-side and server-side with `[Required]`.
- **No input sanitization**: User text inputs go directly to DB.
- **No pagination**: Admin lists (admins, sectors) load all rows at once.
- **No cascade deletes**: `FamilyHeadId` uses `Restrict` delete behavior — can't delete a Person that's a family head.
- **Sequential tabs**: User cannot skip registration steps by clicking tab headers — each step must be completed before advancing.
- **Nominations use N+1 on initial load**: The nomination grid loads persons via service call; no batching yet.
- **NullReferenceException in Update**: Prevented by recovering the `RegistrationViewModel` from the session and loading attachments if model binding fails during POST `/Record/Update`.
