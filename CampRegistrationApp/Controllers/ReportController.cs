using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models.ViewModels;
using CampRegistrationApp.Services;

namespace CampRegistrationApp.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportService _reportService;
        private readonly IAuditService _audit;

        public ReportController(ApplicationDbContext context, IReportService reportService, IAuditService audit)
        {
            _context = context;
            _reportService = reportService;
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

        private int? GetAdminSectorId()
        {
            var role = HttpContext.Session.GetString("AdminRole");
            if (role == "Mandoob")
                return HttpContext.Session.GetInt32("AdminSectorId");
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

            var groups = _reportService.GetColumnGroups();
            var vm = new ReportViewModel
            {
                ColumnGroups = groups,
                SelectedColumns = groups
                    .SelectMany(g => g.Columns)
                    .Where(c => c.IsDefault)
                    .Select(c => c.Key)
                    .ToList(),
                Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync(),
                HeaderLabels = BuildHeaderLabels(groups)
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Preview(ReportViewModel model)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

            model.ColumnGroups = _reportService.GetColumnGroups();
            model.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
            model.HeaderLabels = BuildHeaderLabels(model.ColumnGroups);

            var rows = await _reportService.GetReportDataAsync(model.Filter, model.SelectedColumns, GetAdminSectorId());
            model.Rows = rows;
            model.TotalCount = rows.Count;

            foreach (var row in rows)
                foreach (var key in row.Values.Keys)
                    if (!model.HeaderLabels.ContainsKey(key))
                        model.HeaderLabels[key] = GenerateDynamicLabel(key);

            var sqlQuery = BuildSqlQuery(model.Filter, model.SelectedColumns, GetAdminSectorId());
            await _audit.LogAsync(GetCurrentAdminId(), "PreviewReport", "Reports", null, null, new
            {
                Filter = model.Filter,
                SelectedColumns = model.SelectedColumns,
                SqlQuery = sqlQuery,
                RowCount = rows.Count
            });

            return View("Index", model);
        }

        [HttpPost]
        public async Task<IActionResult> ExportExcel(ReportViewModel model)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

            var rows = await _reportService.GetReportDataAsync(model.Filter, model.SelectedColumns, GetAdminSectorId());
            var excelBytes = await _reportService.GenerateExcelAsync(rows, model.SelectedColumns);

            var sqlQuery = BuildSqlQuery(model.Filter, model.SelectedColumns, GetAdminSectorId());
            await _audit.LogAsync(GetCurrentAdminId(), "ExportExcel", "Reports", null, null, new
            {
                Filter = model.Filter,
                SelectedColumns = model.SelectedColumns,
                SqlQuery = sqlQuery,
                RowCount = rows.Count
            });

            var reportTypeLabel = model.Filter.ReportType switch
            {
                "Disabled" => "ذوي_إعاقة",
                "ChronicSick" => "أمراض_مزمنة",
                "Pregnant" => "حوامل",
                "Nursing" => "مرضعات",
                _ => "تقرير"
            };

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{reportTypeLabel}_{JerusalemTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        private static Dictionary<string, string> BuildHeaderLabels(List<ColumnGroup> groups)
        {
            var labels = new Dictionary<string, string>();
            foreach (var col in groups.SelectMany(g => g.Columns))
                labels[col.Key] = col.Label;

            return labels;
        }

        private static string GenerateDynamicLabel(string key)
        {
            if (key.StartsWith("Wife") && key.Contains("_"))
            {
                var parts = key.Split('_');
                var num = parts[0]["Wife".Length..];
                var field = parts[1] switch
                {
                    "Name" => "الاسم",
                    "IdNumber" => "رقم الهوية",
                    "DOB" => "تاريخ الميلاد",
                    "Age" => "العمر",
                    "HealthStatus" => "الحالة الصحية",
                    "ChronicDiseases" => "أمراض مزمنة",
                    "DisabilityTypes" => "إعاقات",
                    "IsPregnant" => "حامل",
                    "IsNursing" => "مرضع",
                    _ => parts[1]
                };
                return $"الزوجة {num} - {field}";
            }
            if (key.StartsWith("Child") && key.Contains("_"))
            {
                var parts = key.Split('_');
                var num = parts[0]["Child".Length..];
                var field = parts[1] switch
                {
                    "Name" => "الاسم",
                    "IdNumber" => "رقم الهوية",
                    "DOB" => "تاريخ الميلاد",
                    "Age" => "العمر",
                    "Gender" => "الجنس",
                    "HealthStatus" => "الحالة الصحية",
                    "ChronicDiseases" => "أمراض مزمنة",
                    "DisabilityTypes" => "إعاقات",
                    _ => parts[1]
                };
                return $"الابن {num} - {field}";
            }
            if (key == "OtherMembers") return "أفراد آخرون";
            return key;
        }

        private static string BuildSqlQuery(ReportFilter filter, List<string> selectedColumns, int? adminSectorId)
        {
            var reportTypeLabel = filter.ReportType switch
            {
                "Normal" => "عادي",
                "Disabled" => "ذوي إعاقة",
                "ChronicSick" => "أمراض مزمنة",
                "Pregnant" => "حوامل",
                "Nursing" => "مرضعات",
                _ => filter.ReportType
            };

            var query = $"تقرير {reportTypeLabel}\n";
            query += $"SELECT الأعمدة: {string.Join(", ", selectedColumns)}\n";
            query += $"FROM FamilyRegistrations f\n";
            query += $"JOIN Persons head ON f.FamilyHeadId = head.Id\n";

            var wheres = new List<string>();
            if (adminSectorId.HasValue)
                wheres.Add($"f.SectorId = {adminSectorId.Value}");
            if (filter.SectorId.HasValue)
                wheres.Add($"f.SectorId = {filter.SectorId.Value}");
            if (!string.IsNullOrEmpty(filter.Status))
                wheres.Add($"f.ApprovalStatus = '{filter.Status}'");
            if (!string.IsNullOrEmpty(filter.Gender))
                wheres.Add($"head.Gender = '{filter.Gender}'");
            if (!string.IsNullOrEmpty(filter.HealthStatus))
                wheres.Add($"head.HealthStatus = '{filter.HealthStatus}'");
            if (!string.IsNullOrEmpty(filter.Search))
                wheres.Add($"(head.IdNumber LIKE '%{filter.Search}%' OR head.FullName LIKE '%{filter.Search}%')");
            if (filter.AgeFrom.HasValue)
                wheres.Add($"العمر >= {filter.AgeFrom.Value}");
            if (filter.AgeTo.HasValue)
                wheres.Add($"العمر <= {filter.AgeTo.Value}");

            if (wheres.Count > 0)
                query += "WHERE " + string.Join("\n  AND ", wheres);

            query += $"\nORDER BY f.RegistrationTimestamp DESC";
            return query;
        }
    }
}
