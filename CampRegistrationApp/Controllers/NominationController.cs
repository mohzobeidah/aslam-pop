using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using CampRegistrationApp.Services;
using System.Data;

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

            var projectName = await _context.Projects.Where(p => p.Id == projectId).Select(p => p.Name).FirstOrDefaultAsync();
            var personName = await _context.Persons.Where(p => p.Id == personId).Select(p => p.FullName).FirstOrDefaultAsync();
            var adminName = await _context.Admins.Where(a => a.Id == delegateId).Select(a => a.Name).FirstOrDefaultAsync();
            await _audit.LogAsync(delegateId, "AddNomination", "Nominations",
                $"المشروع:{projectName},الشخص:{personName}",
                null,
                new { المشروع = projectName, الشخص = personName, القطاع = sectorEntity.Name, المسؤول = adminName });
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

        var nomination = await _context.Nominations
            .Include(n => n.Person)
            .Include(n => n.Project)
            .FirstOrDefaultAsync(n => n.Id == id);
        var personName = nomination?.Person?.FullName;
        var projectName = nomination?.Project?.Name;
        var adminName = await _context.Admins.Where(a => a.Id == GetCurrentAdminId()).Select(a => a.Name).FirstOrDefaultAsync();

        await _nominationService.DeleteRowAsync(id);
        await _audit.LogAsync(GetCurrentAdminId(), "DeleteNomination", "Nominations",
            $"المشروع:{projectName},الشخص:{personName}",
            nomination != null ? new { المشروع = projectName, الشخص = personName, تاريخ_الحذف = DateTime.UtcNow, المسؤول = adminName } : null,
            null);

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

            var projectName = await _context.Projects.Where(p => p.Id == projectId).Select(p => p.Name).FirstOrDefaultAsync();
            var personNames = await _context.Persons.Where(p => personIds.Contains(p.Id)).Select(p => p.FullName).ToListAsync();
            var adminName = await _context.Admins.Where(a => a.Id == delegateId).Select(a => a.Name).FirstOrDefaultAsync();
            await _audit.LogAsync(delegateId, "AddMultipleNominations", "Nominations",
                $"المشروع:{projectName},عدد:{personIds.Count}",
                null,
                new { المشروع = projectName, الأشخاص = string.Join("، ", personNames), العدد = personIds.Count, المسؤول = adminName });
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

            var projectName = await _context.Projects.Where(p => p.Id == projectId).Select(p => p.Name).FirstOrDefaultAsync();
            var importAdmin = await _context.Admins.Where(a => a.Id == delegateId).Select(a => a.Name).FirstOrDefaultAsync();
            await _audit.LogAsync(delegateId, "ImportNominationsExcel", "Nominations",
                $"المشروع:{projectName}", null,
                new
                {
                    المشروع = projectName,
                    تم_الاستيراد = result.SuccessCount,
                    تم_التخطي = result.SkippedCount,
                    غير_موجودين = result.NotFoundCount,
                    المسؤول = importAdmin
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

    [HttpGet]
    public async Task<IActionResult> ExportExcel(int projectId)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var isAdmin = IsSuperAdmin();
        var delegateId = GetCurrentAdminId();

        int? adminSectorId = null;
        if (!isAdmin)
        {
            var admin = await _context.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == delegateId);
            adminSectorId = admin?.SectorId;
        }

        var nominationsQuery = _context.Nominations
            .AsNoTracking()
            .Include(n => n.Person)
            .Where(n => n.ProjectId == projectId && !n.IsDeleted);

        if (adminSectorId.HasValue)
            nominationsQuery = nominationsQuery.Where(n => n.SectorId == adminSectorId.Value);

        var nominations = await nominationsQuery.ToListAsync();
        var personIds = nominations.Select(n => n.PersonId).ToList();

        var registrations = await _context.FamilyRegistrations
            .AsNoTracking()
            .Include(fr => fr.FamilyHead)
            .Include(fr => fr.Members).ThenInclude(m => m.Person)
            .Where(fr => personIds.Contains(fr.FamilyHeadId) && !fr.IsDeleted)
            .ToListAsync();

        var regMap = registrations.ToDictionary(fr => fr.FamilyHeadId);

        int maxWives = 0;
        foreach (var reg in registrations)
        {
            var wifeCount = reg.Members.Count(m => m.RelationshipToHead == "زوجة");
            if (wifeCount > maxWives) maxWives = wifeCount;
        }

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("الترشيحات");
        ws.RightToLeft = true;

        var headers = new List<string>
        {
            "م", "رقم التسجيل", "الهوية", "الاسم", "الجوال",
            "عدد الأفراد", "نوع المسكن", "نوع الضرر", "الحالة الاجتماعية"
        };

        for (int i = 1; i <= maxWives; i++)
        {
            headers.Add($"الزوجة {i} - الاسم");
            headers.Add($"الزوجة {i} - الهوية");
        }

        for (int c = 0; c < headers.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.Gold;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        int row = 2;
        for (int i = 0; i < nominations.Count; i++)
        {
            var nom = nominations[i];
            regMap.TryGetValue(nom.PersonId, out var reg);

            var recordId = reg?.RecordId ?? "";
            var idNumber = nom.Person.IdNumber;
            var name = nom.Person.FullName;
            var phone = reg?.PhoneNumber ?? "";
            var memberCount = reg?.Members.Count ?? 0;

            string housingType;
            if (reg?.LivesInTent == true)
                housingType = reg.TentType ?? "خيمة";
            else
                housingType = "منزل";

            var damageType = nom.Person.IsHouseDestroyed ? "مدمر كلي" : "سليم";
            var maritalStatus = nom.Person.MaritalStatus;

            var wives = reg?.Members.Where(m => m.RelationshipToHead == "زوجة").ToList() ?? new();

            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = recordId;
            ws.Cell(row, 3).Value = idNumber;
            ws.Cell(row, 4).Value = name;
            ws.Cell(row, 5).Value = phone;
            ws.Cell(row, 6).Value = memberCount;
            ws.Cell(row, 7).Value = housingType;
            ws.Cell(row, 8).Value = damageType;
            ws.Cell(row, 9).Value = maritalStatus;

            int col = 10;
            for (int w = 0; w < maxWives; w++)
            {
                if (w < wives.Count)
                {
                    ws.Cell(row, col).Value = wives[w].Person.FullName;
                    ws.Cell(row, col + 1).Value = wives[w].Person.IdNumber;
                }
                else
                {
                    ws.Cell(row, col).Value = "";
                    ws.Cell(row, col + 1).Value = "";
                }
                col += 2;
            }

            for (int c = 1; c <= headers.Count; c++)
            {
                ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ترشيحات_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }
}
