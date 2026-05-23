using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using CampRegistrationApp.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CampRegistrationApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;
        private readonly INotificationService _notificationService;

        public AdminController(ApplicationDbContext context, IAuditService audit, INotificationService notificationService)
        {
            _context = context;
            _audit = audit;
            _notificationService = notificationService;
        }

        private static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
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

        // ──────────────────────────────────────
        //  Login / Logout
        // ──────────────────────────────────────

        [HttpGet]
        public IActionResult Login()
        {
            if (IsAuthenticated()) return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string nationalId, string password)
        {
            var admin = await _context.Admins
                .Include(a => a.Sector)
                .FirstOrDefaultAsync(a => a.NationalId == nationalId);

            if (admin == null || admin.PasswordHash != HashPassword(password))
            {
                await _audit.LogAsync(0, "LoginFailed", "Admins", null,
                    new { nationalId, reason = "رقم الهوية أو كلمة المرور غير صحيحة" },
                    null);
                ModelState.AddModelError("", "رقم الهوية أو كلمة المرور غير صحيحة");
                return View();
            }

            HttpContext.Session.SetInt32("AdminId", admin.Id);
            HttpContext.Session.SetString("AdminName", admin.Name);
            HttpContext.Session.SetString("AdminRole", admin.Role.ToString());

            await _audit.LogAsync(admin.Id, "Login", "Admins", null,
                null,
                new { admin.Name, admin.NationalId, role = admin.Role.ToString(), sector = admin.Sector?.Name });

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ──────────────────────────────────────
        //  Dashboard
        // ──────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login");

            var adminId = GetCurrentAdminId();
            var admin = await _context.Admins
                .Include(a => a.Sector)
                .FirstOrDefaultAsync(a => a.Id == adminId);

            if (admin == null) return RedirectToAction("Login");

            ViewBag.AdminName = admin.Name;
            ViewBag.AdminRole = admin.Role.ToString();

            var isAdmin = IsSuperAdmin();

            List<SectorDashboard> sectors;
            int totalRegistrations;

            if (isAdmin)
            {
                sectors = await _context.Sectors
                    .Select(s => new SectorDashboard
                    {
                        SectorName = s.Name,
                        Camp = s.Camp,
                        Area = s.Area,
                        ManufacturedTents = s.ManufacturedTentsCount,
                        HandmadeTents = s.HandmadeTentsCount,
                        Bathrooms = s.BathroomsCount,
                        RegistrationCount = _context.FamilyRegistrations.Count(f => f.SectorId == s.Id),
                        ApprovedFamilyCount = _context.FamilyRegistrations
                            .Count(f => f.SectorId == s.Id && f.ApprovalStatus == RegistrationApprovalStatus.Approved)
                    })
                    .ToListAsync();
                totalRegistrations = await _context.FamilyRegistrations.CountAsync();
            }
            else
            {
                var sectorName = admin.Sector?.Name;
                sectors = await _context.Sectors
                    .Where(s => s.Name == sectorName)
                    .Select(s => new SectorDashboard
                    {
                        SectorName = s.Name,
                        Camp = s.Camp,
                        Area = s.Area,
                        ManufacturedTents = s.ManufacturedTentsCount,
                        HandmadeTents = s.HandmadeTentsCount,
                        Bathrooms = s.BathroomsCount,
                        RegistrationCount = _context.FamilyRegistrations.Count(f => f.SectorId == s.Id),
                        ApprovedFamilyCount = _context.FamilyRegistrations
                            .Count(f => f.SectorId == s.Id && f.ApprovalStatus == RegistrationApprovalStatus.Approved)
                    })
                    .ToListAsync();
                totalRegistrations = await _context.FamilyRegistrations
                    .CountAsync(f => f.Sector.Name == sectorName);
            }

            var model = new DashboardViewModel
            {
                Sectors = sectors,
                TotalRegistrations = totalRegistrations,
                TotalAdmins = isAdmin ? await _context.Admins.CountAsync() : 0,
                TotalSectors = isAdmin ? await _context.Sectors.CountAsync() : 0,
                TotalApprovedRefugees = sectors.Sum(s => s.ApprovedFamilyCount)
            };

            ViewBag.IsMandoob = !isAdmin;
            return View(model);
        }

        // ──────────────────────────────────────
        //  Audit Logs (Super Admin only)
        // ──────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> AuditLogs(int page = 1, string? actionFilter = null, string? tableFilter = null)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            const int pageSize = 50;

            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(actionFilter))
                query = query.Where(l => l.Action.Contains(actionFilter));

            if (!string.IsNullOrEmpty(tableFilter))
                query = query.Where(l => l.TableName.Contains(tableFilter));

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new AuditLogListViewModel
            {
                Logs = logs,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                ActionFilter = actionFilter,
                TableFilter = tableFilter
            };

            return View(vm);
        }

        // ──────────────────────────────────────
        //  Notification endpoints
        // ──────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            if (!IsAuthenticated()) return Json(0);

            var adminId = GetCurrentAdminId();
            var count = await _context.Notifications.CountAsync(n => n.AdminId == adminId && !n.IsRead);
            return Json(count);
        }

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login");

            var adminId = GetCurrentAdminId();
            var list = await _context.Notifications
                .Where(n => n.AdminId == adminId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            if (!IsAuthenticated()) return Unauthorized();

            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null && notification.AdminId == GetCurrentAdminId())
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Notifications");
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            if (!IsAuthenticated()) return Unauthorized();

            var adminId = GetCurrentAdminId();
            var unread = await _context.Notifications
                .Where(n => n.AdminId == adminId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread) n.IsRead = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("Notifications");
        }

        // ──────────────────────────────────────
        //  Admin CRUD (Super Admin only)
        // ──────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            var admins = await _context.Admins
                .Include(a => a.Sector)
                .OrderBy(a => a.Role)
                .ThenBy(a => a.Name)
                .ToListAsync();

            return View(admins);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");
            ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Admin admin, string password)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("password", "كلمة المرور مطلوبة");
            }

            if (await _context.Admins.AnyAsync(a => a.NationalId == admin.NationalId))
            {
                ModelState.AddModelError("NationalId", "رقم الهوية موجود مسبقاً");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
                return View(admin);
            }

            admin.PasswordHash = HashPassword(password);
            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة المسؤول بنجاح";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            var admin = await _context.Admins.FindAsync(id);
            if (admin == null) return NotFound();

            ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
            return View(admin);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Admin admin, string? newPassword)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            var existing = await _context.Admins.FindAsync(admin.Id);
            if (existing == null) return NotFound();

            if (await _context.Admins.AnyAsync(a => a.NationalId == admin.NationalId && a.Id != admin.Id))
            {
                ModelState.AddModelError("NationalId", "رقم الهوية موجود مسبقاً");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
                return View(admin);
            }

            existing.Name = admin.Name;
            existing.NationalId = admin.NationalId;
            existing.Mobile = admin.Mobile;
            existing.Role = admin.Role;
            existing.SectorId = admin.SectorId;

            if (!string.IsNullOrEmpty(newPassword))
            {
                existing.PasswordHash = HashPassword(newPassword);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تعديل بيانات المسؤول بنجاح";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            var admin = await _context.Admins.FindAsync(id);
            if (admin != null)
            {
                _context.Admins.Remove(admin);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف المسؤول بنجاح";
            }

            return RedirectToAction("Index");
        }

        // ──────────────────────────────────────
        //  Sector CRUD (Super Admin only)
        // ──────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Sectors()
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            var sectors = await _context.Sectors
                .Include(s => s.Admins)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(sectors);
        }

        [HttpGet]
        public IActionResult CreateSector()
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSector(Sector sector)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            if (await _context.Sectors.AnyAsync(s => s.Name == sector.Name))
            {
                ModelState.AddModelError("Name", "اسم القاطع موجود مسبقاً");
            }

            if (!ModelState.IsValid) return View(sector);

            _context.Sectors.Add(sector);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم إضافة القاطع بنجاح";
            return RedirectToAction("Sectors");
        }

        [HttpGet]
        public async Task<IActionResult> EditSector(int id)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            var sector = await _context.Sectors
                .Include(s => s.Admins)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sector == null) return NotFound();

            ViewBag.AvailableMandoobs = await _context.Admins
                .Where(a => a.Role == AdminRole.Mandoob && a.SectorId == null)
                .OrderBy(a => a.Name)
                .ToListAsync();

            return View(sector);
        }

        [HttpPost]
        public async Task<IActionResult> EditSector(Sector sector)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            var existing = await _context.Sectors.FindAsync(sector.Id);
            if (existing == null) return NotFound();

            if (await _context.Sectors.AnyAsync(s => s.Name == sector.Name && s.Id != sector.Id))
            {
                ModelState.AddModelError("Name", "اسم القاطع موجود مسبقاً");
            }

            if (!ModelState.IsValid) return View(sector);

            existing.Name = sector.Name;
            existing.Camp = sector.Camp;
            existing.Coordinate = sector.Coordinate;
            existing.Area = sector.Area;
            existing.ManufacturedTentsCount = sector.ManufacturedTentsCount;
            existing.HandmadeTentsCount = sector.HandmadeTentsCount;
            existing.BathroomsCount = sector.BathroomsCount;

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تعديل القاطع بنجاح";
            return RedirectToAction("Sectors");
        }

        [HttpPost]
        public async Task<IActionResult> AssignMandoob(int sectorId, int adminId)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            var admin = await _context.Admins.FindAsync(adminId);
            if (admin != null && admin.Role == AdminRole.Mandoob)
            {
                admin.SectorId = sectorId;
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تعيين المندوب للقاطع";
            }

            return RedirectToAction("EditSector", new { id = sectorId });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveMandoob(int sectorId, int adminId)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            var admin = await _context.Admins.FindAsync(adminId);
            if (admin != null)
            {
                admin.SectorId = null;
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم إزالة المندوب من القاطع";
            }

            return RedirectToAction("EditSector", new { id = sectorId });
        }

        [HttpGet]
        public async Task<IActionResult> SearchPersonForMandoob(string query)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return Unauthorized();

            var persons = await _context.Persons
                .AsNoTracking()
                .Where(p => p.IdNumber.Contains(query)
                    || (p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.LastName).Contains(query))
                .Select(p => new
                {
                    p.Id,
                    name = p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.LastName,
                    p.IdNumber
                })
                .Take(20)
                .ToListAsync();

            return Json(persons);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPersonAsMandoob(int sectorId, int personId)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            var person = await _context.Persons.FindAsync(personId);
            if (person == null) return NotFound();

            if (await _context.Admins.AnyAsync(a => a.NationalId == person.IdNumber))
            {
                TempData["Error"] = "هذا الشخص مسجل كمسؤول مسبقاً";
                return RedirectToAction("EditSector", new { id = sectorId });
            }

            // Get phone from family registration if available
            var reg = await _context.FamilyRegistrations
                .FirstOrDefaultAsync(f => f.FamilyHeadId == personId);
            var admin = new Admin
            {
                Name = person.FirstName + " " + person.SecondName + " " + person.ThirdName + " " + person.LastName,
                NationalId = person.IdNumber,
                Mobile = reg?.PhoneNumber ?? person.IdNumber,
                Role = AdminRole.Mandoob,
                SectorId = sectorId,
                PasswordHash = HashPassword(person.IdNumber),
                IsActive = true
            };

            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم تعيين {admin.Name} كمندوب للقاطع";
            return RedirectToAction("EditSector", new { id = sectorId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSector(int id)
        {
            if (!IsAuthenticated() || !IsSuperAdmin()) return RedirectToAction("Dashboard");

            var sector = await _context.Sectors.FindAsync(id);
            if (sector != null)
            {
                _context.Sectors.Remove(sector);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف القاطع بنجاح";
            }

            return RedirectToAction("Sectors");
        }

        [HttpGet]
        public async Task<IActionResult> Refugees(string? sector = null, string? search = null, string? status = "Approved")
        {
            if (!IsAuthenticated()) return RedirectToAction("Login");

            var adminId = GetCurrentAdminId();
            var adminRole = HttpContext.Session.GetString("AdminRole");

            var query = _context.FamilyRegistrations.AsQueryable();

            if (adminRole == "Mandoob")
            {
                var admin = await _context.Admins.Include(a => a.Sector).FirstOrDefaultAsync(a => a.Id == adminId);
                if (admin?.Sector != null)
                    query = query.Where(f => f.SectorId == admin.Sector.Id);
                else if (admin == null)
                    return NotFound();
            }

            if (!string.IsNullOrEmpty(sector))
                query = query.Where(f => f.Sector.Name == sector);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RegistrationApprovalStatus>(status, out var statusFilter))
                query = query.Where(f => f.ApprovalStatus == statusFilter);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(f =>
                    f.FamilyHead.IdNumber.Contains(search) ||
                    f.FamilyHead.FirstName.Contains(search) ||
                    f.FamilyHead.LastName.Contains(search) ||
                    f.Members.Any(m => m.Person.IdNumber.Contains(search) || m.Person.FirstName.Contains(search) || m.Person.LastName.Contains(search))
                );
            }

            var baseQuery = _context.FamilyRegistrations.AsQueryable();
            if (adminRole == "Mandoob")
            {
                var admin = await _context.Admins.Include(a => a.Sector).FirstOrDefaultAsync(a => a.Id == adminId);
                if (admin?.Sector != null)
                    baseQuery = baseQuery.Where(f => f.SectorId == admin.Sector.Id);
            }

            var approvedCount = await baseQuery.CountAsync(f => f.ApprovalStatus == RegistrationApprovalStatus.Approved);
            var pendingCount = await baseQuery.CountAsync(f => f.ApprovalStatus == RegistrationApprovalStatus.Pending);
            var rejectedCount = await baseQuery.CountAsync(f => f.ApprovalStatus == RegistrationApprovalStatus.Rejected);

            var list = await query
                .OrderByDescending(f => f.RegistrationTimestamp)
                .Select(f => new RefugeeViewModel
                {
                    Id = f.Id,
                    RecordId = f.RecordId,
                    HeadName = f.FamilyHead.FirstName + " " + f.FamilyHead.SecondName + " " + f.FamilyHead.ThirdName + " " + f.FamilyHead.LastName,
                    IdNumber = f.FamilyHead.IdNumber,
                    Phone = f.PhoneNumber,
                    Sector = f.Sector.Name,
                    Gender = f.FamilyHead.Gender,
                    MaritalStatus = f.FamilyHead.MaritalStatus,
                    HealthStatus = f.FamilyHead.HealthStatus,
                    MemberCount = f.Members.Count,
                    RegistrationDate = f.RegistrationTimestamp,
                    ApprovalStatus = f.ApprovalStatus.ToString()
                })
                .ToListAsync();

            var sectorApproved = await baseQuery
                .Where(f => f.ApprovalStatus == RegistrationApprovalStatus.Approved)
                .GroupBy(f => f.Sector.Name)
                .Select(g => new { Sector = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync();
            ViewBag.CurrentSector = sector;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;

            var vm = new RefugeeListPageViewModel
            {
                Refugees = list,
                TotalCount = list.Count,
                ApprovedCount = approvedCount,
                PendingCount = pendingCount,
                RejectedCount = rejectedCount,
                SectorFilter = sector,
                SearchQuery = search,
                StatusFilter = status,
                SectorApprovedCounts = sectorApproved.ToDictionary(x => x.Sector, x => x.Count)
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportRefugeesToExcel(string? sector = null, string? search = null, string? status = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login");

            var adminId = GetCurrentAdminId();
            var adminRole = HttpContext.Session.GetString("AdminRole");

            var query = _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .Include(f => f.Members).ThenInclude(m => m.Person)
                .Include(f => f.FamilyDesires).ThenInclude(fd => fd.Desire)
                .Include(f => f.Sector)
                .AsQueryable();

            if (adminRole == "Mandoob")
            {
                var admin = await _context.Admins.Include(a => a.Sector).FirstOrDefaultAsync(a => a.Id == adminId);
                if (admin?.Sector != null)
                    query = query.Where(f => f.SectorId == admin.Sector.Id);
            }

            if (!string.IsNullOrEmpty(sector))
                query = query.Where(f => f.Sector.Name == sector);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RegistrationApprovalStatus>(status, out var exportStatusFilter))
                query = query.Where(f => f.ApprovalStatus == exportStatusFilter);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(f =>
                    f.FamilyHead.IdNumber.Contains(search) ||
                    f.FamilyHead.FirstName.Contains(search) ||
                    f.FamilyHead.LastName.Contains(search));
            }

            var registrations = await query
                .OrderByDescending(f => f.RegistrationTimestamp)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("النازحين");

            var arabicHeaders = new Dictionary<string, string>
            {
                ["RecordId"] = "معرف التسجيل",
                ["HeadName"] = "اسم رب الأسرة",
                ["IdNumber"] = "رقم الهوية",
                ["Phone"] = "رقم الجوال",
                ["Wallet"] = "المحفظة",
                ["Sector"] = "القاطع",
                ["Gender"] = "الجنس",
                ["MaritalStatus"] = "الحالة الاجتماعية",
                ["HealthStatus"] = "الحالة الصحية",
                ["ChronicDiseases"] = "الأمراض المزمنة",
                ["DisabilityTypes"] = "الإعاقات",
                ["MemberCount"] = "عدد الأفراد",
                ["RegistrationDate"] = "تاريخ التسجيل",
                ["ApprovalStatus"] = "الحالة",
                ["Gov"] = "المحافظة",
                ["Dob"] = "تاريخ الميلاد",
                ["TentLiving"] = "يسكن خيمة",
                ["TentType"] = "نوع الخيمة",
                ["OtherTentType"] = "نوع الخيمة (أخرى)",
                ["Bathroom"] = "يوجد حمام",
                ["BathroomType"] = "نوع الحمام",
                ["BathroomStatus"] = "حالة الحمام",
                ["ChildHeaded"] = "يعيل طفل",
                ["ChildHeadedDetails"] = "تفاصيل طفل يعيل",
                ["FemaleHeaded"] = "تعيل امرأة",
                ["FemaleHeadedDetails"] = "تفاصيل امرأة تعيل",
                ["OutsideSupport"] = "دعم خارج العائلة",
                ["OutsidePersonName"] = "اسم الخارجي",
                ["OutsidePersonRelation"] = "صلة قرابة الخارجي",
                ["IsPrisoner"] = "أسير",
                ["EmploymentStatus"] = "الوظيفة",
                ["EducationLevel"] = "المستوى التعليمي",
                ["HasInjury"] = "إصابة",
                ["InjuryDate"] = "تاريخ الإصابة",
                ["InjuryDetails"] = "تفاصيل الإصابة",
                ["IsPregnant"] = "حامل",
                ["PregnancyMonth"] = "شهر الحمل",
                ["IsNursing"] = "مرضع",
                ["NursingInfantName"] = "اسم الطفل",
                ["NursingInfantDOB"] = "تاريخ ميلاد الطفل",
                ["NursingInfantID"] = "رقم هوية الطفل",
                ["NeedsDiapers"] = "حفائظ",
                ["DiaperDetails"] = "تفاصيل الحفائظ",
                ["HasMultipleFamiliesInTent"] = "أسر بنفس الخيمة",
                ["AdditionalFamiliesCount"] = "عدد الأسر الإضافية",
                ["StatusNotes"] = "ملاحظات",
                ["Password"] = "كلمة المرور"
            };

            // Add dynamic desire columns
            var desireLabels = new[] { "الأولى", "الثانية", "الثالثة", "الرابعة", "الخامسة", "السادسة", "السابعة", "الثامنة", "التاسعة", "العاشرة" };
            var allDesires = await _context.Desires.OrderBy(d => d.Id).ToListAsync();
            var desireHeaderColumns = new List<(string key, string header)>();
            for (int i = 0; i < allDesires.Count; i++)
            {
                var key = $"Desire{i + 1}";
                var label = i < desireLabels.Length ? desireLabels[i] : (i + 1).ToString();
                arabicHeaders[key] = $"الرغبة {label} ({allDesires[i].Name})";
                desireHeaderColumns.Add((key, $"الرغبة {label} ({allDesires[i].Name})"));
            }

            int col = 1;
            foreach (var header in arabicHeaders.Values)
            {
                ws.Cell(1, col).Value = header;
                ws.Cell(1, col).Style.Font.Bold = true;
                ws.Cell(1, col).Style.Fill.BackgroundColor = XLColor.Gold;
                col++;
            }

            int row = 2;
            foreach (var reg in registrations)
            {
                var head = reg.FamilyHead;
                ws.Cell(row, 1).Value = reg.RecordId;
                ws.Cell(row, 2).Value = head.FullName;
                ws.Cell(row, 3).Value = head.IdNumber;
                ws.Cell(row, 4).Value = reg.PhoneNumber;
                ws.Cell(row, 5).Value = reg.Wallet ?? "";
                ws.Cell(row, 6).Value = reg.Sector?.Name ?? "";
                ws.Cell(row, 7).Value = head.Gender == "male" ? "ذكر" : head.Gender == "female" ? "أنثى" : head.Gender;
                ws.Cell(row, 8).Value = head.MaritalStatus;
                ws.Cell(row, 9).Value = head.HealthStatus;
                ws.Cell(row, 10).Value = head.ChronicDiseases ?? "";
                ws.Cell(row, 11).Value = head.DisabilityTypes ?? "";
                ws.Cell(row, 12).Value = reg.Members.Count;
                ws.Cell(row, 13).Value = reg.RegistrationTimestamp.ToString("yyyy-MM-dd");
                ws.Cell(row, 14).Value = reg.ApprovalStatus switch
                {
                    RegistrationApprovalStatus.Approved => "مقبول",
                    RegistrationApprovalStatus.Rejected => "مرفوض",
                    _ => "قيد المراجعة"
                };
                ws.Cell(row, 15).Value = head.OriginalGovernorate;
                ws.Cell(row, 16).Value = head.DateOfBirth.ToString("yyyy-MM-dd");
                ws.Cell(row, 17).Value = reg.LivesInTent ? "نعم" : "لا";
                ws.Cell(row, 18).Value = reg.TentType ?? "";
                ws.Cell(row, 19).Value = reg.OtherTentType ?? "";
                ws.Cell(row, 20).Value = reg.HasBathroom ? "نعم" : "لا";
                ws.Cell(row, 21).Value = reg.BathroomType ?? "";
                ws.Cell(row, 22).Value = head.BathroomStatus ?? "";
                ws.Cell(row, 23).Value = reg.IsChildHeaded ? "نعم" : "لا";
                ws.Cell(row, 24).Value = reg.ChildHeadedDetails ?? "";
                ws.Cell(row, 25).Value = reg.IsFemaleHeaded ? "نعم" : "لا";
                ws.Cell(row, 26).Value = reg.FemaleHeadedDetails ?? "";
                ws.Cell(row, 27).Value = reg.SupportsOutsidePerson ? "نعم" : "لا";
                ws.Cell(row, 28).Value = reg.OutsidePersonName ?? "";
                ws.Cell(row, 29).Value = reg.OutsidePersonRelation ?? "";
                ws.Cell(row, 30).Value = head.IsPrisoner ? "نعم" : "لا";
                ws.Cell(row, 31).Value = head.EmploymentStatus ?? "";
                ws.Cell(row, 32).Value = head.EducationLevel ?? "";
                ws.Cell(row, 33).Value = head.HasInjury ? "نعم" : "لا";
                ws.Cell(row, 34).Value = head.InjuryDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row, 35).Value = head.InjuryDetails ?? "";
                ws.Cell(row, 36).Value = head.IsPregnant == true ? "نعم" : "لا";
                ws.Cell(row, 37).Value = head.PregnancyMonth?.ToString() ?? "";
                ws.Cell(row, 38).Value = head.IsNursing == true ? "نعم" : "لا";
                ws.Cell(row, 39).Value = head.NursingInfantName ?? "";
                ws.Cell(row, 40).Value = head.NursingInfantDOB?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row, 41).Value = head.NursingInfantID ?? "";
                ws.Cell(row, 42).Value = reg.NeedsDiapers ? "نعم" : "لا";
                ws.Cell(row, 43).Value = reg.DiaperDetails ?? "";
                ws.Cell(row, 44).Value = reg.HasMultipleFamiliesInTent ? "نعم" : "لا";
                ws.Cell(row, 45).Value = reg.AdditionalFamiliesCount?.ToString() ?? "";
                var regDesires = reg.FamilyDesires.OrderBy(fd => fd.Order).ToList();
                var desireCol = 46;
                foreach (var desire in allDesires)
                {
                    var match = regDesires.FirstOrDefault(fd => fd.DesireId == desire.Id);
                    ws.Cell(row, desireCol).Value = match != null ? match.Order.ToString() : "-";
                    desireCol++;
                }
                ws.Cell(row, desireCol).Value = reg.StatusNotes ?? "";
                ws.Cell(row, desireCol + 1).Value = reg.PasswordHash ?? "";

                foreach (int c in Enumerable.Range(1, arabicHeaders.Count))
                {
                    ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"النازحون_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> RefugeeDetails(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login");

            var reg = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead).ThenInclude(h => h.Attachments)
                .Include(f => f.ApprovedBy)
                .Include(f => f.Members).ThenInclude(m => m.Person)
                .Include(f => f.FamilyDesires).ThenInclude(fd => fd.Desire)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (reg == null) return NotFound();

            return View(reg);
        }

        [HttpGet]
        public async Task<IActionResult> Registrations(string status = "Pending", string? sector = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login");

            var query = _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .Include(f => f.ApprovedBy)
                .AsQueryable();

            var adminId = GetCurrentAdminId();
            var adminRole = HttpContext.Session.GetString("AdminRole");

            if (adminRole == "Mandoob")
            {
                var admin = await _context.Admins.Include(a => a.Sector).FirstAsync(a => a.Id == adminId);
                if (admin.Sector != null)
                    query = query.Where(f => f.SectorId == admin.Sector.Id);
            }

            if (!string.IsNullOrEmpty(sector))
                query = query.Where(f => f.Sector.Name == sector);

            if (Enum.TryParse<RegistrationApprovalStatus>(status, true, out var statusEnum))
                query = query.Where(f => f.ApprovalStatus == statusEnum);

            var list = await query
                .OrderByDescending(f => f.RegistrationTimestamp)
                .Select(f => new RegistrationApprovalViewModel
                {
                    Id = f.Id,
                    RecordId = f.RecordId,
                    HeadName = f.FamilyHead.FirstName + " " + f.FamilyHead.LastName,
                    IdNumber = f.FamilyHead.IdNumber,
                    Sector = f.Sector.Name,
                    RegistrationDate = f.RegistrationTimestamp,
                    ApprovalStatus = f.ApprovalStatus,
                    ApprovedByName = f.ApprovedBy != null ? f.ApprovedBy.Name : null,
                    ApprovedAt = f.ApprovedAt,
                    MemberCount = f.Members.Count
                })
                .ToListAsync();

            ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync();
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSector = sector;
            ViewBag.IsSuperAdmin = IsSuperAdmin();

            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveRegistration(int id)
        {
            if (!IsAuthenticated()) return Unauthorized();

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (registration == null) return NotFound();

            var oldStatus = registration.ApprovalStatus;
            registration.ApprovalStatus = RegistrationApprovalStatus.Approved;
            registration.ApprovedById = GetCurrentAdminId();
            registration.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(GetCurrentAdminId(), "Approve", "FamilyRegistrations",
                registration.RecordId,
                new { status = oldStatus.ToString() },
                new { status = RegistrationApprovalStatus.Approved.ToString(), headName = registration.FamilyHead.FullName });

            TempData["Success"] = "تم الموافقة على التسجيل بنجاح";
            return RedirectToAction("Registrations");
        }

        [HttpPost]
        public async Task<IActionResult> RejectRegistration(int id)
        {
            if (!IsAuthenticated()) return Unauthorized();

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (registration == null) return NotFound();

            var oldStatus = registration.ApprovalStatus;
            registration.ApprovalStatus = RegistrationApprovalStatus.Rejected;
            registration.ApprovedById = GetCurrentAdminId();
            registration.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(GetCurrentAdminId(), "Reject", "FamilyRegistrations",
                registration.RecordId,
                new { status = oldStatus.ToString() },
                new { status = RegistrationApprovalStatus.Rejected.ToString(), headName = registration.FamilyHead.FullName });

            TempData["Success"] = "تم رفض التسجيل";
            return RedirectToAction("Registrations");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRefugee(int id)
        {
            if (!IsAuthenticated()) return Unauthorized();

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .Include(f => f.Sector)
                .FirstOrDefaultAsync(f => f.Id == id && f.ApprovalStatus == RegistrationApprovalStatus.Approved);
            if (registration == null) return NotFound();

            registration.IsDeleted = true;
            registration.DeletedById = GetCurrentAdminId();
            registration.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(GetCurrentAdminId(), "RemoveOutOfCamp", "FamilyRegistrations",
                registration.RecordId,
                new { headName = registration.FamilyHead.FullName, sector = registration.Sector?.Name, status = registration.ApprovalStatus.ToString() },
                new { isDeleted = true, deletedAt = registration.DeletedAt },
                source: "Web");

            TempData["Success"] = $"تم إزالة {registration.FamilyHead.FullName} — انتقل خارج المخيم";
            return RedirectToAction("Refugees");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id)
        {
            if (!IsAuthenticated()) return Unauthorized();

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (registration == null) return NotFound();

            var newPassword = Random.Shared.Next(1000, 10000).ToString();
            registration.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(GetCurrentAdminId(), "ResetPassword", "FamilyRegistrations",
                registration.RecordId,
                new { action = "تم إعادة تعيين كلمة المرور", headName = registration.FamilyHead.FullName },
                null,
                source: "Web");

            return Json(new { success = true, newPassword });
        }

        [HttpGet]
        public async Task<IActionResult> AdminEditRegistration(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login");

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead).ThenInclude(h => h.Attachments)
                .Include(f => f.Members)
                    .ThenInclude(m => m.Person)
                .Include(f => f.FamilyDesires)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (registration == null) return NotFound();

            if (registration.ApprovalStatus != RegistrationApprovalStatus.Pending)
            {
                TempData["Error"] = "لا يمكن تعديل هذا التسجيل بعد الموافقة عليه";
                return RedirectToAction("RefugeeDetails", new { id });
            }

            var model = MapToViewModel(registration);

            ViewBag.FormAction = "AdminUpdateRegistration";
            ViewBag.FormController = "Admin";
            ViewBag.HeadAttachments = registration.FamilyHead.Attachments.ToList();
            await PopulateLookupViewBags();

            return View("~/Views/Record/Edit.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminUpdateRegistration(RegistrationViewModel model)
        {
            if (!IsAuthenticated()) return Unauthorized();

            await PopulateLookupViewBags();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "يرجى تصحيح الأخطاء في البيانات");
                ViewBag.FormAction = "AdminUpdateRegistration";
                ViewBag.FormController = "Admin";
                return View("~/Views/Record/Edit.cshtml", model);
            }

            // Server-side validations
            if (model.Head.MaritalStatus == "متزوج" && !model.Members.Any(m => m.RelationshipToHead == "زوجة"))
            {
                ModelState.AddModelError("", "بما أن الحالة الاجتماعية متزوج، يجب إضافة فرد بصفة زوجة");
                ViewBag.FormAction = "AdminUpdateRegistration";
                ViewBag.FormController = "Admin";
                return View("~/Views/Record/Edit.cshtml", model);
            }

            if (model.Head.HealthStatus == "مريض" && string.IsNullOrEmpty(model.Head.ChronicDiseases) && string.IsNullOrEmpty(model.Head.DisabilityTypes))
            {
                ModelState.AddModelError("", "بما أن الحالة الصحية مريض، يجب اختيار مرض مزمن أو نوع إعاقة على الأقل لرب الأسرة");
                ViewBag.FormAction = "AdminUpdateRegistration";
                ViewBag.FormController = "Admin";
                return View("~/Views/Record/Edit.cshtml", model);
            }

            for (int i = 0; i < model.Members.Count; i++)
            {
                var m = model.Members[i];
                if (m.HealthStatus == "مريض" && string.IsNullOrEmpty(m.ChronicDiseases) && string.IsNullOrEmpty(m.DisabilityTypes))
                {
                    ModelState.AddModelError("", $"الفرد رقم {i + 1}: بما أن الحالة الصحية مريض، يجب اختيار مرض مزمن أو نوع إعاقة على الأقل");
                    ViewBag.FormAction = "AdminUpdateRegistration";
                    ViewBag.FormController = "Admin";
                    return View("~/Views/Record/Edit.cshtml", model);
                }
            }

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead).ThenInclude(h => h.Attachments)
                .Include(f => f.Members)
                    .ThenInclude(m => m.Person)
                .Include(f => f.FamilyDesires)
                .FirstOrDefaultAsync(f => f.Id == model.Id);

            if (registration == null) return NotFound();

            if (registration.ApprovalStatus != RegistrationApprovalStatus.Pending)
            {
                TempData["Error"] = "لا يمكن تعديل هذا التسجيل بعد الموافقة عليه";
                return RedirectToAction("RefugeeDetails", new { id = model.Id });
            }

            ViewBag.HeadAttachments = registration.FamilyHead.Attachments.ToList();

            // Check for duplicate IDs
            var currentHeadId = registration.FamilyHead.IdNumber;
            var currentMemberIds = registration.Members.Select(m => m.Person.IdNumber).ToHashSet();
            var allIds = new List<string> { model.Head.IdNumber };
            allIds.AddRange(model.Members.Select(m => m.IdNumber));

            var duplicateInForm = allIds.GroupBy(id => id).Any(g => g.Count() > 1);
            if (duplicateInForm)
            {
                ModelState.AddModelError("", "يوجد تكرار في أرقام الهوية داخل نفس الطلب. يجب أن يكون لكل شخص رقم هوية فريد.");
                ViewBag.FormAction = "AdminUpdateRegistration";
                ViewBag.FormController = "Admin";
                return View("~/Views/Record/Edit.cshtml", model);
            }

            var existingIds = await _context.Persons
                .Where(p => allIds.Contains(p.IdNumber) && p.IdNumber != currentHeadId && !currentMemberIds.Contains(p.IdNumber))
                .Select(p => p.IdNumber)
                .ToListAsync();

            if (existingIds.Any())
            {
                if (existingIds.Contains(model.Head.IdNumber) && model.Head.IdNumber != currentHeadId)
                {
                    ModelState.AddModelError("", "رقم الهوية هذا مسجل مسبقاً لرب أسرة آخر.");
                }
                else
                {
                    var duplicateMembers = model.Members
                        .Where(m => existingIds.Contains(m.IdNumber))
                        .Select(m => $"{m.FirstName} {m.LastName} (رقم: {m.IdNumber})");
                    ModelState.AddModelError("", $"أرقام الهوية التالية مسجلة مسبقاً لأفراد آخرين: {string.Join("، ", duplicateMembers)}");
                }
                ViewBag.FormAction = "AdminUpdateRegistration";
                ViewBag.FormController = "Admin";
                return View("~/Views/Record/Edit.cshtml", model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update Family Head
                var head = registration.FamilyHead;
                head.FirstName = model.Head.FirstName;
                head.SecondName = model.Head.SecondName;
                head.ThirdName = model.Head.ThirdName;
                head.LastName = model.Head.LastName;
                head.IdNumber = model.Head.IdNumber;
                head.DateOfBirth = model.Head.DateOfBirth;
                head.Gender = model.Head.Gender;
                head.OriginalGovernorate = model.Head.OriginalGovernorate;
                head.MaritalStatus = model.Head.MaritalStatus;
                head.EmploymentStatus = model.Head.EmploymentStatus;
                head.EducationLevel = model.Head.EducationLevel;
                head.HealthStatus = model.Head.HealthStatus;
                head.ChronicDiseases = model.Head.ChronicDiseases;
                head.DisabilityTypes = model.Head.DisabilityTypes;
                head.HasInjury = model.Head.HasInjury;
                head.InjuryDate = model.Head.InjuryDate;
                head.InjuryDetails = model.Head.InjuryDetails;
                head.IsHouseDestroyed = model.Head.IsHouseDestroyed;
                head.IsPrisoner = model.Head.IsPrisoner;
                head.BathroomStatus = model.Head.BathroomStatus;
                head.IsPregnant = model.Head.IsPregnant;
                head.PregnancyMonth = model.Head.PregnancyMonth;
                head.IsNursing = model.Head.IsNursing;
                head.NursingInfantName = model.Head.NursingInfantName;
                head.NursingInfantDOB = model.Head.NursingInfantDOB;
                head.NursingInfantID = model.Head.NursingInfantID;
                head.MotherIdNumber = model.Head.MotherIdNumber;

                // Update Registration-level fields
                registration.SectorId = model.SectorId ?? 0;
                registration.PhoneNumber = model.PhoneNumber;
                registration.Wallet = model.Wallet;
                registration.WalletType = model.WalletType;
                registration.IsChildHeaded = model.IsChildHeaded;
                registration.ChildHeadedDetails = model.ChildHeadedDetails;
                registration.IsFemaleHeaded = model.IsFemaleHeaded;
                registration.FemaleHeadedDetails = model.FemaleHeadedDetails;
                registration.SupportsOutsidePerson = model.SupportsOutsidePerson;
                registration.OutsidePersonName = model.OutsidePersonName;
                registration.OutsidePersonRelation = model.OutsidePersonRelation;
                registration.LivesInTent = model.LivesInTent;
                registration.TentType = model.TentType;
                registration.OtherTentType = model.OtherTentType;
                registration.HasBathroom = model.HasBathroom;
                registration.BathroomType = model.BathroomType;
                registration.NeedsDiapers = model.NeedsDiapers;
                registration.DiaperDetails = model.DiaperDetails;
                registration.HasMultipleFamiliesInTent = model.HasMultipleFamiliesInTent;
                registration.AdditionalFamiliesCount = model.AdditionalFamiliesCount;
                registration.StatusNotes = model.StatusNotes;
                registration.IsHusbandAbroad = model.IsHusbandAbroad;

                // Update family desires
                _context.FamilyDesires.RemoveRange(registration.FamilyDesires);
                if (model.DesireIds != null)
                {
                    for (int i = 0; i < model.DesireIds.Count; i++)
                    {
                        if (model.DesireIds[i] > 0)
                        {
                            _context.FamilyDesires.Add(new FamilyDesire
                            {
                                FamilyRegistrationId = registration.Id,
                                DesireId = model.DesireIds[i],
                                Order = i + 1
                            });
                        }
                    }
                }

                // Get old member person IDs BEFORE removing
                var oldPersonIds = registration.Members.Select(m => m.PersonId).ToList();

                // Remove existing members
                _context.FamilyMembers.RemoveRange(registration.Members);

                // Remove old member persons (not the head)
                var oldPersons = await _context.Persons
                    .Where(p => oldPersonIds.Contains(p.Id) && p.Id != registration.FamilyHeadId)
                    .ToListAsync();
                _context.Persons.RemoveRange(oldPersons);
                await _context.SaveChangesAsync();

                // Add new members
                foreach (var mViewModel in model.Members)
                {
                    var memberPerson = new Person
                    {
                        FirstName = mViewModel.FirstName,
                        SecondName = mViewModel.SecondName,
                        ThirdName = mViewModel.ThirdName,
                        LastName = mViewModel.LastName,
                        IdNumber = mViewModel.IdNumber,
                        DateOfBirth = mViewModel.DateOfBirth,
                        Gender = mViewModel.Gender,
                        OriginalGovernorate = mViewModel.OriginalGovernorate,
                        MaritalStatus = mViewModel.MaritalStatus,
                        EmploymentStatus = mViewModel.EmploymentStatus,
                        EducationLevel = mViewModel.EducationLevel,
                        HealthStatus = mViewModel.HealthStatus,
                        ChronicDiseases = mViewModel.ChronicDiseases,
                        DisabilityTypes = mViewModel.DisabilityTypes,
                        HasInjury = mViewModel.HasInjury,
                        InjuryDate = mViewModel.InjuryDate,
                        InjuryDetails = mViewModel.InjuryDetails,
                        IsPrisoner = mViewModel.IsPrisoner,
                        IsPregnant = mViewModel.IsPregnant,
                        PregnancyMonth = mViewModel.PregnancyMonth,
                        IsNursing = mViewModel.IsNursing,
                        NursingInfantName = mViewModel.NursingInfantName,
                        NursingInfantDOB = mViewModel.NursingInfantDOB,
                        NursingInfantID = mViewModel.NursingInfantID,
                        MotherIdNumber = mViewModel.MotherIdNumber,
                        BathroomStatus = mViewModel.BathroomStatus
                    };
                    _context.Persons.Add(memberPerson);
                    await _context.SaveChangesAsync();

                    _context.FamilyMembers.Add(new FamilyMember
                    {
                        RegistrationId = registration.Id,
                        PersonId = memberPerson.Id,
                        RelationshipToHead = mViewModel.RelationshipToHead
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var sectorName = await _context.Sectors
                    .Where(s => s.Id == model.SectorId)
                    .Select(s => s.Name)
                    .FirstAsync();

                await _audit.LogAsync(GetCurrentAdminId(), "AdminEdit", "FamilyRegistrations",
                    registration.RecordId,
                    new { action = "تم تعديل بيانات العائلة بواسطة المشرف" },
                    new { headName = head.FullName, sector = sectorName },
                    source: "Web");

                await _notificationService.NotifyMandoobsAsync(
                    sectorName,
                    $"تعديل بيانات بواسطة المشرف: {head.FullName} - رقم القيد: {registration.RecordId}",
                    $"/Admin/RefugeeDetails/{registration.Id}");

                TempData["Success"] = "تم تعديل البيانات بنجاح";
                return RedirectToAction("RefugeeDetails", new { id = registration.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "حدث خطأ أثناء حفظ التعديلات: " + ex.Message;
                ViewBag.FormAction = "AdminUpdateRegistration";
                ViewBag.FormController = "Admin";
                return View("~/Views/Record/Edit.cshtml", model);
            }
        }

        private async Task PopulateLookupViewBags()
        {
            ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
            ViewBag.HealthStatuses = await _context.HealthStatuses.OrderBy(h => h.Name).ToListAsync();
            ViewBag.ChronicDiseases = await _context.ChronicDiseases.OrderBy(c => c.Name).ToListAsync();
            ViewBag.DisabilityTypes = await _context.DisabilityTypes.OrderBy(d => d.Name).ToListAsync();
            ViewBag.Desires = await _context.Desires.OrderBy(d => d.Id).ToListAsync();
        }

        private RegistrationViewModel MapToViewModel(FamilyRegistration registration)
        {
            return new RegistrationViewModel
            {
                Id = registration.Id,
                RecordId = registration.RecordId,
                CurrentStep = 1,
                Head = new PersonViewModel
                {
                    FirstName = registration.FamilyHead.FirstName,
                    SecondName = registration.FamilyHead.SecondName,
                    ThirdName = registration.FamilyHead.ThirdName,
                    LastName = registration.FamilyHead.LastName,
                    IdNumber = registration.FamilyHead.IdNumber,
                    DateOfBirth = registration.FamilyHead.DateOfBirth,
                    Gender = registration.FamilyHead.Gender,
                    OriginalGovernorate = registration.FamilyHead.OriginalGovernorate,
                    MaritalStatus = registration.FamilyHead.MaritalStatus,
                    EmploymentStatus = registration.FamilyHead.EmploymentStatus,
                    EducationLevel = registration.FamilyHead.EducationLevel,
                    HealthStatus = registration.FamilyHead.HealthStatus,
                    ChronicDiseases = registration.FamilyHead.ChronicDiseases,
                    DisabilityTypes = registration.FamilyHead.DisabilityTypes,
                    HasInjury = registration.FamilyHead.HasInjury,
                    InjuryDate = registration.FamilyHead.InjuryDate,
                    InjuryDetails = registration.FamilyHead.InjuryDetails,
                    IsPrisoner = registration.FamilyHead.IsPrisoner,
                    BathroomStatus = registration.FamilyHead.BathroomStatus,
                    IsHouseDestroyed = registration.FamilyHead.IsHouseDestroyed,
                    IsPregnant = registration.FamilyHead.IsPregnant,
                    PregnancyMonth = registration.FamilyHead.PregnancyMonth,
                    IsNursing = registration.FamilyHead.IsNursing,
                    NursingInfantName = registration.FamilyHead.NursingInfantName,
                    NursingInfantDOB = registration.FamilyHead.NursingInfantDOB,
                    NursingInfantID = registration.FamilyHead.NursingInfantID,
                    MotherIdNumber = registration.FamilyHead.MotherIdNumber
                },
                Members = registration.Members.Select(m => new MemberViewModel
                {
                    FirstName = m.Person.FirstName,
                    SecondName = m.Person.SecondName,
                    ThirdName = m.Person.ThirdName,
                    LastName = m.Person.LastName,
                    IdNumber = m.Person.IdNumber,
                    DateOfBirth = m.Person.DateOfBirth,
                    Gender = m.Person.Gender,
                    OriginalGovernorate = m.Person.OriginalGovernorate,
                    MaritalStatus = m.Person.MaritalStatus,
                    EmploymentStatus = m.Person.EmploymentStatus,
                    EducationLevel = m.Person.EducationLevel,
                    HealthStatus = m.Person.HealthStatus,
                    ChronicDiseases = m.Person.ChronicDiseases,
                    DisabilityTypes = m.Person.DisabilityTypes,
                    HasInjury = m.Person.HasInjury,
                    InjuryDate = m.Person.InjuryDate,
                    InjuryDetails = m.Person.InjuryDetails,
                    IsPrisoner = m.Person.IsPrisoner,
                    IsPregnant = m.Person.IsPregnant,
                    PregnancyMonth = m.Person.PregnancyMonth,
                    IsNursing = m.Person.IsNursing,
                    NursingInfantName = m.Person.NursingInfantName,
                    NursingInfantDOB = m.Person.NursingInfantDOB,
                    NursingInfantID = m.Person.NursingInfantID,
                    MotherIdNumber = m.Person.MotherIdNumber,
                    BathroomStatus = m.Person.BathroomStatus,
                    RelationshipToHead = m.RelationshipToHead
                }).ToList(),
                IsChildHeaded = registration.IsChildHeaded,
                ChildHeadedDetails = registration.ChildHeadedDetails,
                IsFemaleHeaded = registration.IsFemaleHeaded,
                FemaleHeadedDetails = registration.FemaleHeadedDetails,
                SupportsOutsidePerson = registration.SupportsOutsidePerson,
                OutsidePersonName = registration.OutsidePersonName,
                OutsidePersonRelation = registration.OutsidePersonRelation,
                LivesInTent = registration.LivesInTent,
                TentType = registration.TentType,
                OtherTentType = registration.OtherTentType,
                HasBathroom = registration.HasBathroom,
                BathroomType = registration.BathroomType,
                NeedsDiapers = registration.NeedsDiapers,
                DiaperDetails = registration.DiaperDetails,
                HasMultipleFamiliesInTent = registration.HasMultipleFamiliesInTent,
                AdditionalFamiliesCount = registration.AdditionalFamiliesCount,
                StatusNotes = registration.StatusNotes,
                SectorId = registration.SectorId,
                PhoneNumber = registration.PhoneNumber,
                Wallet = registration.Wallet,
                WalletType = registration.WalletType,
                IsHusbandAbroad = registration.IsHusbandAbroad,
                DesireIds = registration.FamilyDesires
                    .OrderBy(fd => fd.Order)
                    .Select(fd => fd.DesireId)
                    .ToList()
            };
        }
    }
}
