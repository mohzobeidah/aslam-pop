# Mandoob Financial Recorder — Implementation Plan
## الحالة: ⏳ لم ينفذ بعد (خطة تنفيذ)

> **ملاحظة:** هذا المستند هو خطة تنفيذ لم يتم البدء بها بعد.
> لا توجد نماذج أو خدمات أو وحدات تحكم مالية في الكود حاليًا.
> راجع هذا المستند عند البدء في تنفيذ الميزة المالية.

## Context

The Camp Registration app currently has no way for mandoobs (field reps) to record money in/out, so collections, expenses, and advances are tracked on paper / WhatsApp / Excel. This causes missing records, wrong balances, and unresolvable disputes. The PRD (provided by the user) calls for a first-class feature so mandoobs can record income/expense transactions, see a live wallet balance, upload receipts, and have managers generate reports.

This plan adds the feature as a new module inside the existing ASP.NET Core 10 MVC app at `C:\Users\Mohammed\Desktop\aslam pop\CampRegistrationApp\`, reusing every established convention (session-based auth, `IAuditService`, `INotificationService`, soft-delete + query filter, idempotent raw-SQL schema in `Program.cs`, Tailwind/CDN dark-RTL theme).

**User-confirmed scope decisions:**
- Single `FinancialTransaction` table with a `Type` enum (Income / Expense).
- Categories are a `FinancialCategory` lookup table, seeded from the PRD defaults, editable by super admin.
- **No approval gate** — transactions are final on save (audit-logged). Manager visibility is via dashboard + reports, not a Pending workflow. *(Trade-off: simpler & faster; dispute prevention is partially sacrificed — flagged as a v2 enhancement below.)*
- **Daily Closing flow is included** in v1 (open/close day, capture expected vs actual cash, manager can verify).

---

## High-Level Architecture

| Concern | Approach | Reused from |
|---|---|---|
| Auth | Same `HttpContext.Session` (`AdminId` / `AdminRole`) — any logged-in mandoob can record; super admin gets the reports & CRUD | `AdminController` / `AssistanceController` session helpers |
| DB schema | New tables + FKs added via idempotent `IF NOT EXISTS` raw SQL at the end of the `Program.cs` startup block | `Program.cs` lines 488+ (`Complaints` table pattern) |
| Service layer | New `IFinancialService` + `FinancialService` keeps controller thin | `IAssistanceService` / `AssistanceService` pattern |
| Audit | Every state change calls `_audit.LogAsync(...)` | `AuditService` |
| Notifications | On daily-close verification, fire `NotifyAdminAsync` to super admin(s) | `INotificationService` |
| File uploads | Reuse the existing `IFormFile` → `FileCompressionService` (SkiaSharp) pipeline, save to `wwwroot/uploads/financials/{year}/{month}/{mandoobId}_{fileType}_{ts}.{ext}` | `RegistrationController.UploadFile` (lines 76–99) |
| Soft delete | `bool IsDeleted` + `HasQueryFilter(!IsDeleted)` on the new tables | `FamilyRegistration` / `Project` pattern |
| UI | New `Views/Financial/` Razor pages, dark RTL Tailwind, no new libraries | `_Layout.cshtml` design tokens |

---

## New / Modified Files

### Models (new)
- `Models/FinancialCategory.cs` — `int Id`, `[Required] [MaxLength(100)] Name`, `TransactionType Type` (Income/Expense), `bool IsActive = true`, `ICollection<FinancialTransaction> Transactions`.
- `Models/FinancialTransaction.cs` — co-located enum `FinancialTransactionType { Income, Expense }`. Fields: `int Id`, `int MandoobId` + `virtual Admin Mandoob = null!` (Restrict), `FinancialTransactionType Type`, `[Range(0.01, double.MaxValue)] decimal Amount`, `int CategoryId` + `virtual FinancialCategory Category = null!` (Restrict), `string? PaymentMethod` (Cash/Transfer/Other), `string? Description` (max 500), `string? ReceiptPath` (relative path under `wwwroot/uploads/`), `DateTime CreatedAt = DateTime.UtcNow`, `bool IsDeleted`, `DateTime? DeletedAt`, `int? DeletedById`.
- `Models/DailyClosing.cs` — `int Id`, `int MandoobId` + `virtual Admin Mandoob`, `DateTime Date` (unique per mandoob), `decimal OpeningBalance` (computed at open), `decimal ExpectedClosingBalance` (computed when closed), `decimal ActualClosingBalance?` (user enters), `decimal Difference` (computed), `string? Notes`, `ClosingStatus Status` (Open/Closed/Verified), `DateTime OpenedAt = DateTime.UtcNow`, `DateTime? ClosedAt`, `int? VerifiedById?` + `virtual Admin? VerifiedBy`, `DateTime? VerifiedAt?`. Co-located enum.

### Models (modified)
- `Models/Admin.cs` — add `virtual ICollection<FinancialTransaction> FinancialTransactions = new List<...>()` and `virtual ICollection<DailyClosing> DailyClosings = new List<...>()` (no FK changes).

### Data
- `Data/ApplicationDbContext.cs` — add 3 `DbSet<>`s (`FinancialCategories`, `FinancialTransactions`, `DailyClosings`). Configure in `OnModelCreating`:
  - `FinancialCategory`: unique index on `Name`; query filter `!IsDeleted`.
  - `FinancialTransaction`: `Mandoob`/`Category` FKs Restrict; index on `(MandoobId, CreatedAt)`; index on `(Type, CreatedAt)` for reports; query filter `!IsDeleted`.
  - `DailyClosing`: unique composite index `(MandoobId, Date)`; `VerifiedBy` SetNull; `Mandoob` Restrict; query filter `!IsDeleted`; index on `Date`.

### Services
- `Services/IFinancialService.cs` / `Services/FinancialService.cs` — the single business-logic layer:
  - `Task<TransactionListResult> ListAsync(int? mandoobId, FinancialTransactionType? type, int? categoryId, DateTime? from, DateTime? to, int page, int pageSize, int currentAdminId, bool isSuperAdmin)` — applies role filter (mandoob can only see their own rows).
  - `Task<FinancialTransaction> CreateAsync(int mandoobId, FinancialTransactionType type, decimal amount, int categoryId, string? paymentMethod, string? description, string? receiptPath, int actorAdminId)` — validates mandoob exists, category matches type, amount > 0; persists; calls `_audit.LogAsync`.
  - `Task<FinancialTransaction?> GetAsync(int id, int actorAdminId, bool isSuperAdmin)` — role-aware.
  - `Task<bool> SoftDeleteAsync(int id, int actorAdminId, bool isSuperAdmin)` — admin-only; writes audit with old values.
  - `Task<WalletSummary> GetWalletAsync(int mandoobId)` — returns `TodayIncome`, `TodayExpense`, `WeekIncome`, `WeekExpense`, `MonthIncome`, `MonthExpense`, `CurrentBalance` (all-time approved = current, but per user choice, "approved" = non-deleted; we filter `!IsDeleted`).
  - `Task<DailyClosing> OpenDayAsync(int mandoobId, string? notes, int actorAdminId)` — refuses if an `Open` row already exists for today; sets `OpeningBalance` = `CurrentBalance` at open time.
  - `Task<DailyClosing> CloseDayAsync(int dailyClosingId, decimal actualClosingBalance, string? notes, int actorAdminId)` — refuses if no `Open` row; computes `ExpectedClosingBalance` and `Difference`; sets status `Closed`; fires notification to super admin(s) for verification.
  - `Task<DailyClosing> VerifyAsync(int dailyClosingId, int verifierAdminId)` — super admin only; sets status `Verified`, `VerifiedById`, `VerifiedAt`.
  - `Task<DailyClosing?> GetOpenClosingAsync(int mandoobId)` — used by the dashboard.
  - `Task<List<DailyClosing>> ListClosingsAsync(int? mandoobId, DateTime? from, DateTime? to, int actorAdminId, bool isSuperAdmin)` — role-filtered.

### ViewModels
- `Models/ViewModels/FinancialDashboardViewModel.cs` — `WalletSummary Wallet`, `List<FinancialTransaction> RecentTransactions` (last 10), `DailyClosing? TodayClosing`, `int MandoobId`, `bool IsSuperAdmin`.
- `Models/ViewModels/TransactionFormViewModel.cs` — `int Id` (0 = create), `FinancialTransactionType Type`, `[Required] [Range(0.01, ...)] decimal Amount`, `[Required] int CategoryId`, `string? PaymentMethod`, `string? Description`, `IFormFile? Receipt`. Includes `List<FinancialCategory> CategoriesByType` populated server-side.
- `Models/ViewModels/TransactionListViewModel.cs` — paged list + filter state (`from`, `to`, `type`, `categoryId`, `mandoobId`).
- `Models/ViewModels/CloseDayViewModel.cs` — `int DailyClosingId`, `decimal ExpectedClosingBalance`, `[Required] decimal ActualClosingBalance`, `string? Notes`.
- `Models/ViewModels/CategoryFormViewModel.cs` — `int Id`, `[Required] string Name`, `FinancialTransactionType Type`, `bool IsActive`.

### Controller
- `Controllers/FinancialController.cs` (new) — thin controller delegating to `IFinancialService`. Mirrors the `AssistanceController` session helpers exactly:
  - `Dashboard()` — mandoob sees own dashboard; super admin sees picker + aggregated view.
  - `Create(TransactionFormViewModel, IFormFile? receipt)` GET/POST — form + AJAX file upload (reuses the same `IFormFile` pipeline; calls into service).
  - `UploadReceipt(IFormFile file)` — AJAX endpoint like `RegistrationController.UploadFile`, but writes to `wwwroot/uploads/financials/{yyyy}/{mm}/...` and returns `{ success, path }`.
  - `List(...)` GET with query-string filters + pagination.
  - `Delete/{id}` POST (super admin only).
  - `CloseDay()` GET shows the form (expected vs actual), POST saves the close; calls service.
  - `Verify/{id}` POST (super admin only).
  - `Categories()` GET (super admin), `CreateCategory` GET/POST, `EditCategory/{id}` GET/POST, `DeleteCategory/{id}` POST.

### Views (new Razor files in `Views/Financial/`)
- `Dashboard.cshtml` — wallet cards (Current / Today / Week / Month) + recent transactions table + "Add Income" / "Add Expense" buttons + today's open/close status banner.
- `Create.cshtml` — form (Amount, Category select filtered by type, PaymentMethod, Description, Receipt file input, submit). Mobile-first large amount input per PRD.
- `List.cshtml` — filterable table (date range, type, category, status) + export to CSV button (server-side action).
- `CloseDay.cshtml` — expected vs actual input + notes.
- `Categories.cshtml` / `CreateCategory.cshtml` / `EditCategory.cshtml` — CRUD pages.
- All views use the existing dark RTL Tailwind classes (`bg-camp-accent-grey`, `text-camp-gold`, `border-gray-700`, `rounded-xl`) and Cairo font — copy the visual language from `Views/Assistance/Index.cshtml` and `Views/Admin/Notifications.cshtml`.

### Layout / Navigation
- `Views/Shared/_Layout.cshtml` — add a `المالية` (Financial) nav link, gated on `isLoggedIn`, in both the desktop nav block (around line 71, next to `المساعدات`) and the mobile nav block (around line 105). One link → `Financial/Dashboard`.
- `Views/Admin/Dashboard.cshtml` — add a feature tile linking to `Financial/Dashboard` / `Financial/Categories` / `Financial/List` (super admin only).

### Program.cs (modified)
- Register `IFinancialService → FinancialService` in the scoped-services block (lines 24–33).
- Append a new idempotent SQL section at the end of the startup block (after the `Complaints` section, ~line 488) that:
  1. `IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FinancialCategories' AND xtype='U') CREATE TABLE FinancialCategories (...)`.
  2. Same for `FinancialTransactions`, `DailyClosings`.
  3. Adds the composite unique index `(MandoobId, Date)` on `DailyClosings` and the `(MandoobId, CreatedAt)` index on `FinancialTransactions` (wrapped in `IF NOT EXISTS` checks on `sys.indexes`).
  4. Adds a `NOT NULL` seed of the 11 default categories from the PRD (Delivery Income / Customer Payment / Refund / Bonus / Transfer Received / Fuel / Food / Car Maintenance / Parking / Mobile-Data / Miscellaneous), only if the table is empty.
  5. Adds the `CategoryId` FK to `FinancialTransactions` and `VerifiedById` FK to `DailyClosings` via `ALTER TABLE ... WITH CHECK ADD CONSTRAINT ... IF NOT EXISTS` pattern (note: SQL Server doesn't support `IF NOT EXISTS` on `ADD CONSTRAINT` directly — wrap in `IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_...')`).

### `Migrator` project (optional, recommended)
- Open `CampRegistrationApp.Migrator/Program.cs` (currently excluded from the main `.csproj`). Add a corresponding migration entry so anyone running the Migrator against an existing DB gets the same tables. If the Migrator isn't maintained, skip — the raw-SQL `IF NOT EXISTS` in `Program.cs` is sufficient for this codebase.

---

## Feature Flows (end-to-end)

### Mandoob records income
1. Mandoob logs in (existing flow).
2. Clicks المالية → Dashboard.
3. Clicks "Add Income" → `Financial/Create?type=Income` → form rendered with only Income categories.
4. Enters amount, picks category, optionally uploads a receipt (AJAX POST to `Financial/UploadReceipt` saves file, returns relative path).
5. Submits → `FinancialController.Create` POST → `FinancialService.CreateAsync` validates, writes the row, fires `_audit.LogAsync(actor, "CreateTransaction", "FinancialTransactions", id, null, new { Amount, Type, CategoryId })`.
6. Returns to Dashboard — wallet card updates.

### Daily closing
1. End of day, mandoob clicks "Close Day" on Dashboard.
2. `Financial/CloseDay` GET — service returns the `Open` row (if none, the page says "No open day for today" and offers to `OpenDay`).
3. Form shows `ExpectedClosingBalance` (server-computed) and inputs `ActualClosingBalance` + notes.
4. POST → service sets status `Closed`, persists Difference, fires `INotificationService.NotifyAdminAsync` to every super admin (`Role == Admin`) with a verification link.
5. Super admin sees the notification (existing bell), clicks → `Financial/Verify/{id}` → status `Verified`, audit logged.

### Manager reporting
1. Super admin → المالية → Categories / List.
2. `List` shows all mandoobs' transactions with filters; "Export CSV" button hits `Financial/Export` (POST) which streams a CSV.

---

## Verification

1. **Build & run** — `dotnet build` then `dotnet run --project CampRegistrationApp/CampRegistrationApp.csproj`. Confirm no compile errors.
2. **DB schema** — on first run, the new `IF NOT EXISTS` SQL creates the 3 tables. Re-run to confirm idempotence (no errors on second start).
3. **Login as default super admin** (`admin` / `admin123`) — confirm the المالية nav link appears in desktop + mobile menus.
4. **Categories** — `/Financial/Categories` shows 11 seeded rows; create/edit/delete one and confirm it appears in the Income/Expense dropdowns.
5. **Create transaction** — login as a mandoob, create 1 income (with receipt upload — confirm file lands in `wwwroot/uploads/financials/2026/06/...`) and 1 expense. Confirm:
   - Dashboard wallet shows the right totals (current, today, week, month).
   - `AuditLog` has a `CreateTransaction` row with the amount in `NewValues`.
   - File is served via `/File/Download?path=uploads/financials/...`.
6. **List & filter** — `/Financial/List?type=Expense&from=...&to=...` returns the right rows, and the mandoob only sees their own rows (cross-mandoob isolation).
7. **Daily close** — open a day, add a couple of transactions, close the day with an actual balance that matches; reopen, close with a mismatch — confirm the `Difference` is computed and the super admin receives a bell notification.
8. **Verify** — super admin verifies the close; status flips to `Verified`; audit row written.
9. **Soft delete** — super admin deletes a transaction via the row's delete button; row disappears from lists, but the underlying row is still in the DB with `IsDeleted=1`. Audit captures the pre-delete state.
10. **Mobile-friendly** — load `/Financial/Dashboard` in a narrow viewport; the wallet cards stack and the "Add Income"/"Add Expense" buttons are large and tappable (per the PRD's "mobile-first form" guidance).
11. **Regression** — re-test the existing registration → approval → notification path to confirm the new nav and Program.cs SQL additions didn't break it.
