using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using CampRegistrationApp.Services;

namespace CampRegistrationApp.Controllers;

public class ProjectController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _audit;

    public ProjectController(ApplicationDbContext context, IAuditService audit)
    {
        _context = context;
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
    public async Task<IActionResult> Index()
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var projects = await _context.Projects
            .AsNoTracking()
            .Include(p => p.CreatedBy)
            .Where(p => !p.IsDeleted)
            .Select(p => new ProjectListViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                RequiredCount = p.RequiredCount,
                Status = p.Status.ToString(),
                CreatedByName = p.CreatedBy.Name,
                CreatedAt = p.CreatedAt,
                NominationCount = _context.Nominations.Count(n => n.ProjectId == p.Id && !n.IsDeleted)
            })
            .ToListAsync();

        return View(projects);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");
        if (!IsSuperAdmin())
        {
            TempData["Error"] = "ليس لديك صلاحية";
            return RedirectToAction("Index");
        }

        return View(new ProjectViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectViewModel model)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");
        if (!IsSuperAdmin())
        {
            TempData["Error"] = "ليس لديك صلاحية";
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid)
            return View(model);

        var project = new Project
        {
            Name = model.Name,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            RequiredCount = model.RequiredCount,
            Status = model.Status,
            Description = model.Description,
            Notes = model.Notes,
            CreatedById = GetCurrentAdminId()
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(GetCurrentAdminId(), "Create", "Projects", project.Id.ToString(), null, project);

        TempData["Success"] = "تم إنشاء المشروع بنجاح";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");
        if (!IsSuperAdmin())
        {
            TempData["Error"] = "ليس لديك صلاحية";
            return RedirectToAction("Index");
        }

        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (project == null)
            return NotFound();

        var model = new ProjectViewModel
        {
            Id = project.Id,
            Name = project.Name,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            RequiredCount = project.RequiredCount,
            Status = project.Status,
            Description = project.Description,
            Notes = project.Notes
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectViewModel model)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");
        if (!IsSuperAdmin())
        {
            TempData["Error"] = "ليس لديك صلاحية";
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid)
            return View(model);

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == model.Id && !p.IsDeleted);

        if (project == null)
            return NotFound();

        var old = new { project.Name, project.StartDate, project.EndDate, project.RequiredCount, project.Status, project.Description, project.Notes };

        project.Name = model.Name;
        project.StartDate = model.StartDate;
        project.EndDate = model.EndDate;
        project.RequiredCount = model.RequiredCount;
        project.Status = model.Status;
        project.Description = model.Description;
        project.Notes = model.Notes;

        await _context.SaveChangesAsync();

        await _audit.LogAsync(GetCurrentAdminId(), "Update", "Projects", project.Id.ToString(), old, project);

        TempData["Success"] = "تم تحديث المشروع بنجاح";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");
        if (!IsSuperAdmin())
        {
            TempData["Error"] = "ليس لديك صلاحية";
            return RedirectToAction("Index");
        }

        var project = await _context.Projects.FindAsync(id);
        if (project != null)
        {
            project.IsDeleted = true;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(GetCurrentAdminId(), "Delete", "Projects", project.Id.ToString(), project, null);
        }

        TempData["Success"] = "تم حذف المشروع";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> View(int id)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

        var service = HttpContext.RequestServices.GetRequiredService<INominationService>();
        var isAdmin = IsSuperAdmin();
        var delegateId = GetCurrentAdminId();

        var vm = await service.GetNominationPageAsync(id, delegateId, isAdmin);
        ViewBag.Sectors = await _context.Sectors.AsNoTracking().ToListAsync();
        return View("~/Views/Nomination/Index.cshtml", vm);
    }
}
