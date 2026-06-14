using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using CampRegistrationApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampRegistrationApp.Controllers;

public class AssistanceController : Controller
{
    private readonly IAssistanceService _assistanceService;
    private readonly IImportService _importService;
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _audit;

    public AssistanceController(
        IAssistanceService assistanceService,
        IImportService importService,
        ApplicationDbContext context,
        IAuditService audit)
    {
        _assistanceService = assistanceService;
        _importService = importService;
        _context = context;
        _audit = audit;
    }

    private bool IsAuthenticated() => HttpContext.Session.GetInt32("AdminId").HasValue;
    private bool IsSuperAdmin() => HttpContext.Session.GetString("AdminRole") == "Admin";
    private bool IsViewer() => HttpContext.Session.GetString("AdminRole") == "Viewer";
    private int GetUserId() => HttpContext.Session.GetInt32("AdminId") ?? 0;

    private async Task<int> GetUserSectorId()
    {
        var admin = await _context.Admins.AsNoTracking().Include(a => a.Sector)
            .FirstOrDefaultAsync(a => a.Id == GetUserId());
        return admin?.SectorId ?? 0;
    }

    // ──────────────────────────────────────
    //  Assistance List
    // ──────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(string? sector = null, string? search = null, string? status = null)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var list = await _assistanceService.GetAllAsync(sector, search, status, IsSuperAdmin(), GetUserId());
        ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync();
        ViewBag.CurrentSector = sector;
        ViewBag.CurrentSearch = search;
        ViewBag.CurrentStatus = status;
        ViewBag.IsSuperAdmin = IsSuperAdmin();
        ViewBag.IsViewer = IsViewer();
        return View(list);
    }

    // ──────────────────────────────────────
    //  Details
    // ──────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var assistance = await _assistanceService.GetByIdAsync(id);
        if (assistance == null) return NotFound();

        ViewBag.IsSuperAdmin = IsSuperAdmin();
        ViewBag.IsViewer = IsViewer();
        ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
        return View(assistance);
    }

    // ──────────────────────────────────────
    //  Create
    // ──────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsAuthenticated() || IsViewer()) return RedirectToAction("Index");

        ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
        return View(new CreateAssistanceViewModel { AssistanceDate = DateTime.Today });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAssistanceViewModel model)
    {
        if (!IsAuthenticated() || IsViewer()) return RedirectToAction("Index");

        if (!IsSuperAdmin())
        {
            model.SectorId = await GetUserSectorId();
            ModelState.Remove(nameof(model.SectorId));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
            return View(model);
        }

        var assistance = new Assistance
        {
            Name = model.Name,
            AssistanceType = model.AssistanceType,
            Source = model.Source,
            AssistanceDate = model.AssistanceDate,
            Description = model.Description,
            SectorId = model.SectorId ?? 0
        };

        await _assistanceService.CreateAsync(assistance, GetUserId());
        TempData["Success"] = "تم إنشاء المساعدة بنجاح";
        return RedirectToAction("Index");
    }

    // ──────────────────────────────────────
    //  Edit
    // ──────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAuthenticated() || IsViewer()) return RedirectToAction("Index");

        var assistance = await _assistanceService.GetByIdAsync(id);
        if (assistance == null) return NotFound();

        ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
        return View(new EditAssistanceViewModel
        {
            Id = assistance.Id,
            Name = assistance.Name,
            AssistanceType = assistance.AssistanceType,
            Source = assistance.Source,
            AssistanceDate = assistance.AssistanceDate,
            Description = assistance.Description,
            SectorId = assistance.SectorId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditAssistanceViewModel model)
    {
        if (!IsAuthenticated() || IsViewer()) return RedirectToAction("Index");

        if (!ModelState.IsValid)
        {
            ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
            return View(model);
        }

        var assistance = new Assistance
        {
            Id = model.Id,
            Name = model.Name,
            AssistanceType = model.AssistanceType,
            Source = model.Source,
            AssistanceDate = model.AssistanceDate,
            Description = model.Description,
            SectorId = model.SectorId ?? 0
        };

        await _assistanceService.UpdateAsync(assistance, GetUserId());
        TempData["Success"] = "تم تعديل المساعدة بنجاح";
        return RedirectToAction("Index");
    }

    // ──────────────────────────────────────
    //  Delete / Approve / Cancel
    // ──────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAuthenticated() || IsViewer()) return Unauthorized();

        await _assistanceService.DeleteAsync(id, GetUserId());
        TempData["Success"] = "تم حذف المساعدة";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        if (!IsAuthenticated() || !IsSuperAdmin()) return Unauthorized();

        await _assistanceService.ApproveAsync(id, GetUserId());
        TempData["Success"] = "تم اعتماد المساعدة";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!IsAuthenticated() || !IsSuperAdmin()) return Unauthorized();

        await _assistanceService.CancelAsync(id, GetUserId());
        TempData["Success"] = "تم إلغاء المساعدة";
        return RedirectToAction("Index");
    }

    // ──────────────────────────────────────
    //  Beneficiaries
    // ──────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AddBeneficiary(AssistanceBeneficiary model)
    {
        if (!IsAuthenticated() || IsViewer()) return Unauthorized();

        if (!IsSuperAdmin())
        {
            model.SectorId = await GetUserSectorId();
            ModelState.Remove(nameof(model.SectorId));
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "يرجى ملء جميع الحقول المطلوبة";
            return RedirectToAction("Details", new { id = model.AssistanceId });
        }

        try
        {
            await _assistanceService.AddBeneficiaryAsync(model, GetUserId());
            TempData["Success"] = "تم إضافة المستفيد بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Details", new { id = model.AssistanceId });
    }

    [HttpPost]
    public async Task<IActionResult> EditBeneficiary(AssistanceBeneficiary model)
    {
        if (!IsAuthenticated() || IsViewer()) return Unauthorized();

        try
        {
            await _assistanceService.UpdateBeneficiaryAsync(model, GetUserId());
            TempData["Success"] = "تم تعديل المستفيد";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Details", new { id = model.AssistanceId });
    }

    [HttpGet]
    public async Task<IActionResult> SearchPerson(string query)
    {
        if (!IsAuthenticated()) return Unauthorized();

        string? sectorName = null;
        if (!IsSuperAdmin())
        {
            var admin = await _context.Admins
                .Include(a => a.Sector)
                .FirstOrDefaultAsync(a => a.Id == GetUserId());
            sectorName = admin?.Sector?.Name;
        }

        var persons = await _assistanceService.SearchPersonsAsync(query, sectorName);

        var personIds = persons.Select(p => p.Id).ToList();
        var phones = await _context.FamilyRegistrations
            .Where(fr => personIds.Contains(fr.FamilyHeadId))
            .ToDictionaryAsync(fr => fr.FamilyHeadId, fr => fr.PhoneNumber);

        return Json(persons.Select(p => new
        {
            p.Id,
            name = p.FullName,
            p.IdNumber,
            phone = phones.GetValueOrDefault(p.Id, "")
        }));
    }

    [HttpPost]
    public async Task<IActionResult> AddBeneficiaryFromPerson(int personId, int assistanceId)
    {
        if (!IsAuthenticated() || IsViewer()) return Unauthorized();

        try
        {
            await _assistanceService.AddBeneficiaryFromPersonAsync(personId, assistanceId, GetUserId());
            TempData["Success"] = "تم إضافة المستفيد بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Details", new { id = assistanceId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBeneficiary(int id, int assistanceId)
    {
        if (!IsAuthenticated() || IsViewer()) return Unauthorized();

        await _assistanceService.DeleteBeneficiaryAsync(id, GetUserId());
        TempData["Success"] = "تم حذف المستفيد";
        return RedirectToAction("Details", new { id = assistanceId });
    }

    // ──────────────────────────────────────
    //  Import
    // ──────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Import()
    {
        if (!IsAuthenticated() || IsViewer()) return RedirectToAction("Index");

        var sectorId = IsSuperAdmin() ? 0 : await GetUserSectorId();
        var assistances = IsSuperAdmin()
            ? await _context.Assistances.Where(a => !a.IsDeleted).ToListAsync()
            : await _context.Assistances.Where(a => a.SectorId == sectorId && !a.IsDeleted).ToListAsync();

        ViewBag.Assistances = assistances;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Import(int assistanceId, IFormFile excelFile)
    {
        if (!IsAuthenticated() || IsViewer()) return RedirectToAction("Index");

        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["Error"] = "يرجى اختيار ملف Excel";
            return RedirectToAction("Import");
        }

        var sectorId = IsSuperAdmin()
            ? (await _context.Assistances.FindAsync(assistanceId))?.SectorId ?? 0
            : await GetUserSectorId();

        using var stream = new MemoryStream();
        await excelFile.CopyToAsync(stream);
        stream.Position = 0;

        try
        {
            var result = await _importService.ImportFromExcelAsync(
                stream, excelFile.FileName, assistanceId, GetUserId(), sectorId);

            TempData["Success"] = $"تم الاستيراد: {result.SuccessRows} بنجاح، {result.DuplicateRows} مكرر، {result.FailedRows} فاشل";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"فشل الاستيراد: {ex.Message}";
        }

        return RedirectToAction("Import");
    }

    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        var bytes = _importService.GenerateTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "قالب_المستفيدين.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ImportHistory()
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var query = _context.AssistanceImports
            .Include(i => i.ImportedBy)
            .Include(i => i.Sector)
            .AsQueryable();

        if (!IsSuperAdmin())
        {
            var sectorId = await GetUserSectorId();
            query = query.Where(i => i.SectorId == sectorId);
        }

        var list = await query.OrderByDescending(i => i.ImportedAt).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadErrorReport(int id)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var import = await _context.AssistanceImports.FindAsync(id);
        if (import == null || string.IsNullOrEmpty(import.ErrorFilePath) || !System.IO.File.Exists(import.ErrorFilePath))
        {
            TempData["Error"] = "ملف الأخطاء غير موجود";
            return RedirectToAction("ImportHistory");
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(import.ErrorFilePath);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"errors_{import.Id}.xlsx");
    }

    // ──────────────────────────────────────
    //  Export
    // ──────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ExportBeneficiaries(int id)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var beneficiaries = await _assistanceService.GetBeneficiariesAsync(id);
        var assistance = await _context.Assistances.FindAsync(id);

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("المستفيدين");

        var headers = new[] { "الاسم", "رقم الهوية", "الجوال", "القاطع", "نوع الاستفادة", "تاريخ الإضافة" };
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
            ws.Cell(1, c + 1).Style.Font.Bold = true;
            ws.Cell(1, c + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.Gold;
        }

        int row = 2;
        foreach (var b in beneficiaries)
        {
            ws.Cell(row, 1).Value = b.FullName;
            ws.Cell(row, 2).Value = b.NationalId;
            ws.Cell(row, 3).Value = b.Phone;
            ws.Cell(row, 4).Value = b.Sector?.Name ?? "";
            ws.Cell(row, 5).Value = b.BenefitType;
            ws.Cell(row, 6).Value = b.CreatedAt.ToString("yyyy-MM-dd");
            row++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();

        var fileName = assistance != null
            ? $"مستفيدو_{assistance.Name}_{JerusalemTime.Now:yyyyMMdd}.xlsx"
            : $"مستفيدون_{JerusalemTime.Now:yyyyMMdd}.xlsx";

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // ──────────────────────────────────────
    //  Import Excel on Details page
    // ──────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(int id, IFormFile excelFile)
    {
        if (!IsAuthenticated() || IsViewer()) return RedirectToAction("Details", new { id });

        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["Error"] = "يرجى اختيار ملف Excel";
            return RedirectToAction("Details", new { id });
        }

        var sectorId = IsSuperAdmin()
            ? (await _context.Assistances.FindAsync(id))?.SectorId ?? 0
            : await GetUserSectorId();

        using var stream = new MemoryStream();
        await excelFile.CopyToAsync(stream);
        stream.Position = 0;

        try
        {
            var result = await _importService.ImportFromExcelAsync(
                stream, excelFile.FileName, id, GetUserId(), sectorId);

            var msg = $"تم استيراد {result.SuccessRows} مستفيد";
            if (result.DuplicateRows > 0) msg += $"، {result.DuplicateRows} مكرر";
            if (result.FailedRows > 0) msg += $"، {result.FailedRows} فشل";
            TempData["Success"] = msg;

            if (result.ImportErrors.Count > 0)
                TempData["ImportErrors"] = string.Join(" | ", result.ImportErrors.Take(20));
            if (result.ImportWarnings.Count > 0)
                TempData["ImportWarnings"] = string.Join(" | ", result.ImportWarnings.Take(20));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"فشل استيراد Excel: {ex.Message}";
        }

        return RedirectToAction("Details", new { id });
    }

    // ──────────────────────────────────────
    //  Delete All Beneficiaries
    // ──────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAllBeneficiaries(int id)
    {
        if (!IsAuthenticated() || IsViewer()) return RedirectToAction("Details", new { id });
        if (!IsSuperAdmin())
        {
            TempData["Error"] = "ليس لديك صلاحية";
            return RedirectToAction("Details", new { id });
        }

        var beneficiaries = await _context.AssistanceBeneficiaries
            .Where(b => b.AssistanceId == id && !b.IsDeleted)
            .ToListAsync();

        var count = beneficiaries.Count;
        var assistanceName = await _context.Assistances.Where(a => a.Id == id).Select(a => a.Name).FirstOrDefaultAsync();
        var adminName = await _context.Admins.Where(a => a.Id == GetUserId()).Select(a => a.Name).FirstOrDefaultAsync();

        foreach (var b in beneficiaries)
        {
            b.IsDeleted = true;
        }

        await _context.SaveChangesAsync();

        await _audit.LogAsync(GetUserId(), "DeleteAllBeneficiaries", "AssistanceBeneficiaries",
            $"المساعدة:{assistanceName},عدد:{count}",
            new { المساعدة = assistanceName, العدد = count, تاريخ_الحذف = DateTime.UtcNow, المسؤول = adminName },
            null);

        TempData["Success"] = $"تم حذف {count} مستفيد بنجاح";
        return RedirectToAction("Details", new { id });
    }

    // ──────────────────────────────────────
    //  Bulk Add Beneficiaries
    // ──────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetFamilyHeads(int assistanceId)
    {
        if (!IsAuthenticated()) return Unauthorized();

        string? sectorName = null;
        if (!IsSuperAdmin())
        {
            var admin = await _context.Admins
                .Include(a => a.Sector)
                .FirstOrDefaultAsync(a => a.Id == GetUserId());
            sectorName = admin?.Sector?.Name;
        }

        var heads = await _assistanceService.GetFamilyHeadsAsync(sectorName, assistanceId);
        return Json(heads);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMultipleBeneficiaries(int assistanceId, List<int> personIds)
    {
        if (!IsAuthenticated() || IsViewer()) return Unauthorized();

        try
        {
            await _assistanceService.AddMultipleBeneficiariesAsync(assistanceId, personIds, GetUserId());
            TempData["Success"] = $"تمت إضافة {personIds.Count} مستفيد بنجاح";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "حدث خطأ: " + ex.Message;
        }

        return RedirectToAction("Details", new { id = assistanceId });
    }

    [HttpGet]
    public async Task<IActionResult> CheckPersonInAssistance(int assistanceId, int personId)
    {
        if (!IsAuthenticated()) return Unauthorized();

        var exists = await _assistanceService.PersonExistsInAssistanceAsync(assistanceId, personId);
        return Json(new { exists });
    }
}
