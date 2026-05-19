using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CampRegistrationApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
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
                ModelState.AddModelError("", "رقم الهوية أو كلمة المرور غير صحيحة");
                return View();
            }

            HttpContext.Session.SetInt32("AdminId", admin.Id);
            HttpContext.Session.SetString("AdminName", admin.Name);
            HttpContext.Session.SetString("AdminRole", admin.Role.ToString());

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
                        RegistrationCount = _context.Persons.Count(p => p.Sector == s.Name)
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
                        RegistrationCount = _context.Persons.Count(p => p.Sector == s.Name)
                    })
                    .ToListAsync();
                totalRegistrations = await _context.Persons
                    .CountAsync(p => p.Sector == sectorName);
            }

            var model = new DashboardViewModel
            {
                Sectors = sectors,
                TotalRegistrations = totalRegistrations,
                TotalAdmins = isAdmin ? await _context.Admins.CountAsync() : 0,
                TotalSectors = isAdmin ? await _context.Sectors.CountAsync() : 0
            };

            ViewBag.IsMandoob = !isAdmin;
            return View(model);
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
        public async Task<IActionResult> Refugees(string? sector = null, string? search = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login");

            var adminId = GetCurrentAdminId();
            var adminRole = HttpContext.Session.GetString("AdminRole");

            var query = _context.FamilyRegistrations.AsQueryable();

            if (adminRole == "Mandoob")
            {
                var admin = await _context.Admins.Include(a => a.Sector).FirstOrDefaultAsync(a => a.Id == adminId);
                if (admin?.Sector != null)
                    query = query.Where(f => f.FamilyHead.Sector == admin.Sector.Name);
                else if (admin == null)
                    return NotFound();
            }

            if (!string.IsNullOrEmpty(sector))
                query = query.Where(f => f.FamilyHead.Sector == sector);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(f =>
                    f.FamilyHead.IdNumber.Contains(search) ||
                    f.FamilyHead.FirstName.Contains(search) ||
                    f.FamilyHead.LastName.Contains(search) ||
                    f.Members.Any(m => m.Person.IdNumber.Contains(search) || m.Person.FirstName.Contains(search) || m.Person.LastName.Contains(search))
                );
            }

            var list = await query
                .OrderByDescending(f => f.RegistrationTimestamp)
                .Select(f => new RefugeeViewModel
                {
                    Id = f.Id,
                    RecordId = f.RecordId,
                    HeadName = f.FamilyHead.FirstName + " " + f.FamilyHead.SecondName + " " + f.FamilyHead.ThirdName + " " + f.FamilyHead.LastName,
                    IdNumber = f.FamilyHead.IdNumber,
                    Phone = f.FamilyHead.PhoneNumber,
                    Sector = f.FamilyHead.Sector,
                    Gender = f.FamilyHead.Gender,
                    MaritalStatus = f.FamilyHead.MaritalStatus,
                    HealthStatus = f.FamilyHead.HealthStatus,
                    MemberCount = f.Members.Count,
                    RegistrationDate = f.RegistrationTimestamp,
                    ApprovalStatus = f.ApprovalStatus.ToString()
                })
                .ToListAsync();

            ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync();
            ViewBag.CurrentSector = sector;
            ViewBag.CurrentSearch = search;

            var vm = new RefugeeListPageViewModel
            {
                Refugees = list,
                TotalCount = list.Count,
                SectorFilter = sector,
                SearchQuery = search
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportRefugeesToExcel(string? sector = null, string? search = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login");

            var adminId = GetCurrentAdminId();
            var adminRole = HttpContext.Session.GetString("AdminRole");

            var query = _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .Include(f => f.Members).ThenInclude(m => m.Person)
                .AsQueryable();

            if (adminRole == "Mandoob")
            {
                var admin = await _context.Admins.Include(a => a.Sector).FirstOrDefaultAsync(a => a.Id == adminId);
                if (admin?.Sector != null)
                    query = query.Where(f => f.FamilyHead.Sector == admin.Sector.Name);
            }

            if (!string.IsNullOrEmpty(sector))
                query = query.Where(f => f.FamilyHead.Sector == sector);

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
                ["Bathroom"] = "يوجد حمام",
                ["BathroomType"] = "نوع الحمام",
                ["ChildHeaded"] = "يعيل طفل",
                ["FemaleHeaded"] = "تعيل امرأة",
                ["OutsideSupport"] = "دعم خارج العائلة",
                ["Password"] = "كلمة المرور"
            };

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
                ws.Cell(row, 4).Value = head.PhoneNumber;
                ws.Cell(row, 5).Value = head.Sector;
                ws.Cell(row, 6).Value = head.Gender == "male" ? "ذكر" : head.Gender == "female" ? "أنثى" : head.Gender;
                ws.Cell(row, 7).Value = head.MaritalStatus;
                ws.Cell(row, 8).Value = head.HealthStatus;
                ws.Cell(row, 9).Value = head.ChronicDiseases ?? "";
                ws.Cell(row, 10).Value = head.DisabilityTypes ?? "";
                ws.Cell(row, 11).Value = reg.Members.Count;
                ws.Cell(row, 12).Value = reg.RegistrationTimestamp.ToString("yyyy-MM-dd");
                ws.Cell(row, 13).Value = reg.ApprovalStatus switch
                {
                    RegistrationApprovalStatus.Approved => "مقبول",
                    RegistrationApprovalStatus.Rejected => "مرفوض",
                    _ => "قيد المراجعة"
                };
                ws.Cell(row, 14).Value = head.OriginalGovernorate;
                ws.Cell(row, 15).Value = head.DateOfBirth.ToString("yyyy-MM-dd");
                ws.Cell(row, 16).Value = reg.LivesInTent ? "نعم" : "لا";
                ws.Cell(row, 17).Value = reg.TentType ?? "";
                ws.Cell(row, 18).Value = reg.HasBathroom ? "نعم" : "لا";
                ws.Cell(row, 19).Value = reg.BathroomType ?? "";
                ws.Cell(row, 20).Value = reg.IsChildHeaded ? "نعم" : "لا";
                ws.Cell(row, 21).Value = reg.IsFemaleHeaded ? "نعم" : "لا";
                ws.Cell(row, 22).Value = reg.SupportsOutsidePerson ? "نعم" : "لا";
                ws.Cell(row, 23).Value = reg.PasswordHash ?? "";

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
                .Include(f => f.FamilyHead)
                .Include(f => f.ApprovedBy)
                .Include(f => f.Members).ThenInclude(m => m.Person)
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
                    query = query.Where(f => f.FamilyHead.Sector == admin.Sector.Name);
            }

            if (!string.IsNullOrEmpty(sector))
                query = query.Where(f => f.FamilyHead.Sector == sector);

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
                    Sector = f.FamilyHead.Sector,
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

            var registration = await _context.FamilyRegistrations.FindAsync(id);
            if (registration == null) return NotFound();

            registration.ApprovalStatus = RegistrationApprovalStatus.Approved;
            registration.ApprovedById = GetCurrentAdminId();
            registration.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم الموافقة على التسجيل بنجاح";
            return RedirectToAction("Registrations");
        }

        [HttpPost]
        public async Task<IActionResult> RejectRegistration(int id)
        {
            if (!IsAuthenticated()) return Unauthorized();

            var registration = await _context.FamilyRegistrations.FindAsync(id);
            if (registration == null) return NotFound();

            registration.ApprovalStatus = RegistrationApprovalStatus.Rejected;
            registration.ApprovedById = GetCurrentAdminId();
            registration.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم رفض التسجيل";
            return RedirectToAction("Registrations");
        }
    }
}
