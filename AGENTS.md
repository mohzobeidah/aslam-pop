# AGENTS.md — Camp Registration (Google Apps Script + ASP.NET Core)

## Overview
Camp family registration system in two versions:
- **Google Apps Script** (root `Code.gs` + `Index.html`): Serves HTML, writes to Google Sheets, uploads PDFs to Drive.
- **ASP.NET Core MVC** (`CampRegistrationApp/`): .NET 10, SQL Server (LocalDB), Entity Framework Core, full admin/nomination/project system.

## How to run / deploy
### GAS
- Deploy via Google Apps Script editor (Extensions > Apps Script > Deploy > New deployment). No local dev server.

### ASP.NET Core
- Build: `dotnet build`
- Run: `dotnet run --project CampRegistrationApp/CampRegistrationApp.csproj`
- Test: `dotnet test`
- Selenium E2E: `dotnet test CampRegistrationApp.SeleniumTests/CampRegistrationApp.SeleniumTests.csproj`
- Ports: HTTP `localhost:5392`, HTTPS `localhost:7126`
- Production: CI/CD via `.github/workflows/publish.yml`, Azure deploy via `.deployment`

### Playwright E2E Tests (`CampRegistrationApp.PlaywrightTests/`)
- **Setup**: `cd CampRegistrationApp.PlaywrightTests && npm install && npx playwright install chromium`
- **Run**: `BASE_URL=http://localhost:5392 npx playwright test` (app must be running on that URL)
- **Run headed (debug)**: `BASE_URL=http://localhost:5392 npx playwright test --headed`
- **HTML report**: After run, open `playwright-report/index.html`
- **Tests**: `tests/registration.spec.ts` (4-step wizard) + `tests/record-edit.spec.ts` (login, edit, admin edit)
- **Helpers**: Valid Palestinian ID generation, selector constants
- Requires the ASP.NET Core app to be running (the Selenium tests use an in-process server; Playwright tests connect to a running instance)

## ASP.NET Core Architecture
### Data Model
- **Person**: Shared entity for family head and members (4-part name, ID, sector, DOB, gender, phone, Wallet (المحفظة), BathroomStatus (جيد/متوسط/سيء), health, maternity, prisoner flag, etc.).
- **FamilyRegistration**: Links to head (Person), members (FamilyMembers), housing/special-case fields (HasBathroom, BathroomType), approval workflow, and **Refugee Needs** (NeedPriority enum for 7 aid items: Tents, Blankets, Mattresses, KitchenTools, Tarpaulins, Clothes, HygieneKit).
- **FamilyMember**: Join table `FamilyRegistration → Person` with `RelationshipToHead`.
- **Attachment**: File metadata (`MedicalReport` or `IDImage`), stores relative paths.
- **Admin**: Login with `AdminRole` (`Admin`=super, `Mandoob`=sector-limited), session-based auth, SHA256 password hashing.
- **Sector**: Camp sector with name, camp, coordinates, area, tent/bathroom counts.
- **Project/Nomination**: Campaign system for aid distribution with approval workflow, soft delete, rowversion concurrency.
- **AuditLog**: Immutable audit trail with JSON old/new values and automatic source tracking (Web/Mobile).
- **Notification**: Per-admin notification system with bell icon polling.

### Registration Flow (4 Steps + Submit)
1. Step 1: Family Head info + health + documents + injury (always visible) + BathroomStatus
2. Step 2: Family Members (dynamic add/remove). Validation: if MaritalStatus=متزوج, at least one member must have RelationshipToHead=زوجة.
3. Step 3: Housing & Special Cases + **Bathroom section** (HasBathroom, BathroomType, BathroomStatus) + **Refugee Desires** (الرغبات — ranked dropdowns)
4. Step 4: Review & Confirm + **StatusNotes** textarea for additional notes + password creation

### Admin Edit (only before acceptance)
- **AdminEditRegistration** GET: loads Pending registration, reuses `Record/Edit.cshtml` view
- **AdminUpdateRegistration** POST: saves changes (head, members, desires) with audit log + mandoob notification
- Only accessible when `ApprovalStatus == Pending` — blocked for Approved/Rejected
- Edit button shown in `RefugeeDetails` page and `Registrations` list for Pending items

### Refugee Desires
- Ranked dropdowns (الرغبة رقم 1, 2, ...) populated from `Desires` DB table
- Each select corresponds to a rank position; selections are mutually exclusive per rank
- Stored as `FamilyDesire` join records with Order (rank position) and DesireId
- **Model binding**: Before submit, comma-separated hidden input is converted to indexed inputs (`DesireIds[0]`, `DesireIds[1]`, ...) for proper `List<int>` binding

## GAS Architecture
- `doGet()` serves `Index.html`, `google.script.run` for server calls.
- `processForm(data)` appends row to sheet `البيانات`.
- `uploadFileToDrive(base64Data, fileName)` saves to Drive folder `التقارير الطبية`.
- Record ID: first 8 chars of `Utilities.getUuid()`.

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

    ### NullReferenceException in Record Update
    - Fixed crash in `Edit.cshtml` during POST `/Record/Update` when model binding fails.
    - Added logic to recover `RegistrationViewModel` from session and ensure `ViewBag.HeadAttachments` are populated when returning to the view.

    ### Production Error Page
    - Implemented a professional, themed error view at `/Home/Error` for non-development environments.
    - Displays a user-friendly Arabic message and the Request ID for support tracking.

    ## Key Conventions (both versions)
- All UI text in Arabic, RTL layout, Cairo font, dark theme (`#121212` + `#d4af37` gold).
- 8-char Record ID from charset `23456789ABCDEFGHJKLMNPQRSTUVWXYZ`.
- No input sanitization; no migrations (`EnsureCreated()` + raw SQL).
