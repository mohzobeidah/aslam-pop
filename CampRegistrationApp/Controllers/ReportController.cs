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

            model.SelectedColumns ??= new List<string>();
            model.ColumnGroups = _reportService.GetColumnGroups();
            model.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
            model.HeaderLabels = BuildHeaderLabels(model.ColumnGroups);

            var rows = await _reportService.GetReportDataAsync(model.Filter, model.SelectedColumns, GetAdminSectorId());
            model.Rows = rows;
            model.TotalCount = rows.Count;
            model.DisplayColumns = _reportService.ResolveDisplayColumns(rows, model.SelectedColumns);
            model.HeaderLabels = model.DisplayColumns.ToDictionary(c => c.Key, c => c.Label, StringComparer.Ordinal);

            var auditPayload = await BuildReportAuditPayloadAsync(model.Filter, model.SelectedColumns, model.DisplayColumns, model.ColumnGroups, rows.Count, "PreviewReport");
            await _audit.LogAsync(GetCurrentAdminId(), "PreviewReport", "Reports", auditPayload.Summary, null, auditPayload.Payload);

            return View("Index", model);
        }

        [HttpPost]
        public async Task<IActionResult> ExportExcel(ReportViewModel model)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Admin");

            model.SelectedColumns ??= new List<string>();
            var rows = await _reportService.GetReportDataAsync(model.Filter, model.SelectedColumns, GetAdminSectorId());
            var excelBytes = await _reportService.GenerateExcelAsync(rows, model.SelectedColumns);

            model.ColumnGroups = _reportService.GetColumnGroups();
            var displayColumns = _reportService.ResolveDisplayColumns(rows, model.SelectedColumns);
            var auditPayload = await BuildReportAuditPayloadAsync(model.Filter, model.SelectedColumns, displayColumns, model.ColumnGroups, rows.Count, "ExportExcel");
            await _audit.LogAsync(GetCurrentAdminId(), "ExportExcel", "Reports", auditPayload.Summary, null, auditPayload.Payload);

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

        private async Task<(string Summary, object Payload)> BuildReportAuditPayloadAsync(
            ReportFilter filter,
            List<string> selectedColumns,
            List<ReportDisplayColumn> displayColumns,
            List<ColumnGroup> columnGroups,
            int rowCount,
            string action)
        {
            var adminSectorId = GetAdminSectorId();
            string? adminSectorName = null;
            string? filterSectorName = null;

            if (adminSectorId.HasValue)
                adminSectorName = await _context.Sectors.Where(s => s.Id == adminSectorId.Value).Select(s => s.Name).FirstOrDefaultAsync();
            if (filter.SectorId.HasValue)
                filterSectorName = await _context.Sectors.Where(s => s.Id == filter.SectorId.Value).Select(s => s.Name).FirstOrDefaultAsync();

            var payload = ReportQueryDescriptor.BuildAuditPayload(
                filter, selectedColumns, displayColumns, columnGroups,
                adminSectorId, adminSectorName, filterSectorName, rowCount, action);

            var summary = $"{ReportQueryDescriptor.GetReportTypeLabel(filter.ReportType)} | {rowCount} صف";
            if (!string.IsNullOrEmpty(filterSectorName))
                summary += $" | قاطع: {filterSectorName}";
            else if (!string.IsNullOrEmpty(adminSectorName))
                summary += $" | قاطع: {adminSectorName}";

            return (summary, payload);
        }
    }
}
