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
    public async Task<IActionResult> AddRow(int projectId, int personId, int sectorId, string? description, string? notes)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var delegateId = GetCurrentAdminId();

        try
        {
            await _nominationService.AddOrUpdateRowAsync(projectId, personId, sectorId, delegateId, description, notes);
            await _audit.LogAsync(delegateId, "AddNomination", "Nominations", $"project:{projectId},person:{personId}", null, new { projectId, personId, sectorId });
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
        return Json(persons.Select(p => new
        {
            p.Id,
            name = p.FullName,
            p.IdNumber,
            phone = p.PhoneNumber
        }));
    }

    [HttpGet]
    public async Task<IActionResult> CheckPersonInProject(int projectId, int personId)
    {
        if (!IsAuthenticated()) return Unauthorized();

        var exists = await _nominationService.PersonIsNominatedInProjectAsync(projectId, personId);
        return Json(new { exists });
    }
}
