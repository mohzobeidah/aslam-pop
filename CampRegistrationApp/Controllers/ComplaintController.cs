using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using CampRegistrationApp.Services;

namespace CampRegistrationApp.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IComplaintIdGenerator _idGenerator;
        private readonly IAuditService _audit;

        public ComplaintController(ApplicationDbContext context, IComplaintIdGenerator idGenerator, IAuditService audit)
        {
            _context = context;
            _idGenerator = idGenerator;
            _audit = audit;
        }

        private bool IsAuthenticated()
        {
            return HttpContext.Session.GetInt32("AdminId").HasValue;
        }

        private int GetCurrentAdminId()
        {
            return HttpContext.Session.GetInt32("AdminId") ?? 0;
        }

        // ========== Public Area ==========

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PublicSubmitViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PublicSubmitViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var ticketId = await _idGenerator.GenerateUniqueIdAsync();

            var complaint = new Complaint
            {
                TicketId = ticketId,
                Subject = model.Subject,
                Message = model.Message,
                SenderName = model.SenderName,
                SenderPhone = model.SenderPhone,
                Status = ComplaintStatus.Pending
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(0, "CreateComplaint", "Complaints",
                complaint.Id.ToString(), null, new { complaint.TicketId, complaint.Subject, complaint.SenderName });

            TempData["TicketId"] = ticketId;
            return RedirectToAction("Confirmation");
        }

        [HttpGet]
        public IActionResult Confirmation()
        {
            var ticketId = TempData["TicketId"] as string;
            if (string.IsNullOrEmpty(ticketId))
                return RedirectToAction("Create");

            ViewBag.TicketId = ticketId;
            return View();
        }

        // ========== Admin Area ==========

        [HttpGet]
        public async Task<IActionResult> Index(string? status = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

            var query = _context.Complaints.AsNoTracking();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ComplaintStatus>(status, true, out var statusFilter))
                query = query.Where(c => c.Status == statusFilter);

            var list = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ComplaintListViewModel
                {
                    Id = c.Id,
                    TicketId = c.TicketId,
                    Subject = c.Subject,
                    SenderName = c.SenderName,
                    Status = c.Status.ToString(),
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

            var complaint = await _context.Complaints
                .AsNoTracking()
                .Include(c => c.ResolvedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null) return NotFound();

            var vm = new ComplaintDetailsViewModel
            {
                Id = complaint.Id,
                TicketId = complaint.TicketId,
                Subject = complaint.Subject,
                Message = complaint.Message,
                SenderName = complaint.SenderName,
                SenderPhone = complaint.SenderPhone,
                Status = complaint.Status.ToString(),
                AdminResponse = complaint.AdminResponse,
                ResolvedByName = complaint.ResolvedBy?.Name,
                ResolvedAt = complaint.ResolvedAt,
                CreatedAt = complaint.CreatedAt
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(int id, string? adminResponse, string? newStatus)
        {
            if (!IsAuthenticated()) return Unauthorized();

            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == id);
            if (complaint == null) return NotFound();

            var oldStatus = complaint.Status;
            var oldResponse = complaint.AdminResponse;

            if (!string.IsNullOrEmpty(adminResponse))
                complaint.AdminResponse = adminResponse;

            if (!string.IsNullOrEmpty(newStatus) && Enum.TryParse<ComplaintStatus>(newStatus, true, out var status))
            {
                complaint.Status = status;
                if (status == ComplaintStatus.Resolved)
                {
                    complaint.ResolvedById = GetCurrentAdminId();
                    complaint.ResolvedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            await _audit.LogAsync(GetCurrentAdminId(), "RespondComplaint", "Complaints",
                id.ToString(),
                new { status = oldStatus.ToString(), responseLength = oldResponse?.Length ?? 0 },
                new { status = complaint.Status.ToString(), responseLength = (complaint.AdminResponse?.Length ?? 0) });

            TempData["Success"] = "تم حفظ الرد بنجاح";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return Unauthorized();

            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == id);
            if (complaint == null) return NotFound();

            var old = new { complaint.TicketId, complaint.SenderName, complaint.SenderPhone, complaint.Status };
            complaint.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(GetCurrentAdminId(), "DeleteComplaint", "Complaints",
                id.ToString(), old, new { isDeleted = true });

            TempData["Success"] = "تم حذف الشكوى";
            return RedirectToAction("Index");
        }
    }
}
