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
- Ports: HTTP `localhost:5392`, HTTPS `localhost:7126`
- Production: CI/CD via `.github/workflows/publish.yml`, Azure deploy via `.deployment`

## ASP.NET Core Architecture
### Data Model
- **Person**: Shared entity for family head and members (4-part name, ID, sector, DOB, gender, phone, Wallet (المحفظة), BathroomStatus (جيد/متوسط/سيء), health, maternity, prisoner flag, etc.).
- **FamilyRegistration**: Links to head (Person), members (FamilyMembers), housing/special-case fields, approval workflow, and **Refugee Needs** (NeedPriority enum for 7 aid items: Tents, Blankets, Mattresses, KitchenTools, Tarpaulins, Clothes, HygieneKit).
- **FamilyMember**: Join table `FamilyRegistration → Person` with `RelationshipToHead`.
- **Attachment**: File metadata (`MedicalReport` or `IDImage`), stores relative paths.
- **Admin**: Login with `AdminRole` (`Admin`=super, `Mandoob`=sector-limited), session-based auth, SHA256 password hashing.
- **Sector**: Camp sector with name, camp, coordinates, area, tent/bathroom counts.
- **Project/Nomination**: Campaign system for aid distribution with approval workflow, soft delete, rowversion concurrency.
- **AuditLog**: Immutable audit trail with JSON old/new values.
- **Notification**: Per-admin notification system with bell icon polling.

### Registration Flow (4 Steps + Submit)
1. Step 1: Family Head info + health + documents + injury (always visible) + BathroomStatus
2. Step 2: Family Members (dynamic add/remove). Validation: if MaritalStatus=متزوج, at least one member must have RelationshipToHead=زوجة.
3. Step 3: Housing & Special Cases + **Refugee Needs** (7 aid items with None/Low/Medium/High/Critical priority selectors)
4. Step 4: Review & Confirm + **StatusNotes** textarea for additional notes

### Refugee Needs (`NeedPriority` enum)
- `None=0`, `Low=1`, `Medium=2`, `High=3`, `Critical=4`
- Fields: `NeedTents`, `NeedBlankets`, `NeedMattresses`, `NeedKitchenTools`, `NeedTarpaulins`, `NeedClothes`, `NeedHygieneKit`
- Stored on `FamilyRegistration`, mapped via `int` in `RegistrationViewModel`

## GAS Architecture
- `doGet()` serves `Index.html`, `google.script.run` for server calls.
- `processForm(data)` appends row to sheet `البيانات`.
- `uploadFileToDrive(base64Data, fileName)` saves to Drive folder `التقارير الطبية`.
- Record ID: first 8 chars of `Utilities.getUuid()`.

## Key Conventions (both versions)
- All UI text in Arabic, RTL layout, Cairo font, dark theme (`#121212` + `#d4af37` gold).
- 8-char Record ID from charset `23456789ABCDEFGHJKLMNPQRSTUVWXYZ`.
- No input sanitization; no migrations (`EnsureCreated()` + raw SQL).
