using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using CampRegistrationApp.Services;

namespace CampRegistrationApp.Controllers;

public class NominationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INominationService _nominationService;
    private readonly IAuditService _audit;

    public NominationController(ApplicationDbContext context, INominationService nominationService, IAuditService audit)
    {
        _context = context;
        _nominationService = nominationService;
        _audit = audit;
    }

    private bool IsAuthenticated()
    {
        return HttpContext.Session.GetInt32("AdminId").HasValue;
    }

    private bool IsSuperAdmin()
    {
        return HttpContext.Session.GetString("AdminRole") == "Admin";
    }

    private int GetCurrentAdminId()
    {
        return HttpContext.Session.GetInt32("AdminId") ?? 0;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int projectId)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var isAdmin = IsSuperAdmin();
        var delegateId = GetCurrentAdminId();
        ViewBag.Sectors = await _context.Sectors.AsNoTracking().ToListAsync();
        ViewBag.IsSuperAdmin = isAdmin;

        if (!isAdmin)
        {
            var admin = await _context.Admins
                .Include(a => a.Sector)
                .FirstOrDefaultAsync(a => a.Id == delegateId);
            ViewBag.DelegateSectorId = admin?.SectorId;
        }

        var vm = await _nominationService.GetNominationPageAsync(projectId, delegateId, isAdmin);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRow(int projectId, int personId, string? description, string? notes)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var delegateId = GetCurrentAdminId();

        try
        {
                var sector = await _context.FamilyRegistrations
                    .Where(fr => fr.FamilyHeadId == personId)
                    .Select(fr => fr.Sector.Name)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(sector))
                {
                    sector = await _context.FamilyMembers
                        .Where(fm => fm.PersonId == personId)
                        .Select(fm => fm.Registration.Sector.Name)
                        .FirstOrDefaultAsync();
                }

            if (sector == null)
                throw new InvalidOperationException("لم يتم العثور على قطاع للشخص");

            var sectorEntity = await _context.Sectors
                .FirstOrDefaultAsync(s => s.Name == sector);

            if (sectorEntity == null)
                throw new InvalidOperationException("لم يتم العثور على القطاع في النظام");

            await _nominationService.AddOrUpdateRowAsync(projectId, personId, sectorEntity.Id, delegateId, description, notes);
            await _audit.LogAsync(delegateId, "AddNomination", "Nominations", $"project:{projectId},person:{personId}", null, new { projectId, personId, sectorId = sectorEntity.Id });
            TempData["Success"] = "تمت إضافة الترشيح بنجاح";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "حدث خطأ: " + ex.Message;
        }

        return RedirectToAction("Index", new { projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRow(int id, int projectId)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        await _nominationService.DeleteRowAsync(id);
        await _audit.LogAsync(GetCurrentAdminId(), "DeleteNomination", "Nominations", id.ToString(), null, null);

        TempData["Success"] = "تم حذف الترشيح";
        return RedirectToAction("Index", new { projectId });
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
                .FirstOrDefaultAsync(a => a.Id == GetCurrentAdminId());
            sectorName = admin?.Sector?.Name;
        }

        var persons = await _nominationService.SearchPersonsAsync(query, sectorName);

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

    [HttpGet]
    public async Task<IActionResult> CheckPersonInProject(int projectId, int personId)
    {
        if (!IsAuthenticated()) return Unauthorized();

        var exists = await _nominationService.PersonIsNominatedInProjectAsync(projectId, personId);
        return Json(new { exists });
    }

    [HttpGet]
    public async Task<IActionResult> GetFamilyHeads(int projectId)
    {
        if (!IsAuthenticated()) return Unauthorized();

        string? sectorName = null;
        if (!IsSuperAdmin())
        {
            var admin = await _context.Admins
                .Include(a => a.Sector)
                .FirstOrDefaultAsync(a => a.Id == GetCurrentAdminId());
            sectorName = admin?.Sector?.Name;
        }

        var heads = await _nominationService.GetFamilyHeadsAsync(sectorName, projectId);
        return Json(heads);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMultipleRows(int projectId, List<int> personIds, string? notes)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var delegateId = GetCurrentAdminId();

        try
        {
            await _nominationService.AddMultipleRowsAsync(projectId, personIds, delegateId, notes);
            await _audit.LogAsync(delegateId, "AddMultipleNominations", "Nominations",
                $"project:{projectId},count:{personIds.Count}", null,
                new { projectId, personIds, count = personIds.Count });
            TempData["Success"] = $"تمت إضافة {personIds.Count} ترشيح بنجاح";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "حدث خطأ: " + ex.Message;
        }

        return RedirectToAction("Index", new { projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(int projectId, IFormFile excelFile)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["Error"] = "يرجى اختيار ملف Excel";
            return RedirectToAction("Index", new { projectId });
        }

        var delegateId = GetCurrentAdminId();

        try
        {
            using var stream = new MemoryStream();
            await excelFile.CopyToAsync(stream);
            stream.Position = 0;

            var result = await _nominationService.ImportFromExcelAsync(stream, projectId, delegateId);

            await _audit.LogAsync(delegateId, "ImportNominationsExcel", "Nominations",
                $"project:{projectId}", null,
                new
                {
                    projectId,
                    result.SuccessCount,
                    result.SkippedCount,
                    result.NotFoundCount
                });

            var msg = $"تم استيراد {result.SuccessCount} ترشيح";
            if (result.SkippedCount > 0) msg += $"، تم تخطي {result.SkippedCount}";
            if (result.NotFoundCount > 0) msg += $"، {result.NotFoundCount} غير موجودين";
            TempData["Success"] = msg;

            if (result.Errors.Count > 0)
                TempData["ImportErrors"] = string.Join(" | ", result.Errors.Take(20));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"فشل استيراد Excel: {ex.Message}";
        }

        return RedirectToAction("Index", new { projectId });
    }

    [HttpGet]
    public IActionResult DownloadImportTemplate()
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("الترشيحات");
        ws.Cell(1, 1).Value = "NationalId";
        ws.Cell(1, 2).Value = "Notes";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 2).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.Gold;
        ws.Cell(1, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.Gold;
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "قالب_ترشيحات.xlsx");
    }
}
