using System.Text;
using CampRegistrationApp.Models.ViewModels;

namespace CampRegistrationApp.Services;

/// <summary>Builds a human-readable description of the report request for audit logs.</summary>
public static class ReportQueryDescriptor
{
    public static object BuildAuditPayload(
        ReportFilter filter,
        List<string> selectedColumns,
        List<ReportDisplayColumn>? displayColumns,
        List<ColumnGroup> columnGroups,
        int? adminSectorId,
        string? adminSectorName,
        string? filterSectorName,
        int rowCount,
        string action)
    {
        var queryText = BuildQueryText(filter, selectedColumns, displayColumns, columnGroups, adminSectorId, adminSectorName, filterSectorName, rowCount, action);
        var columnLabels = ResolveColumnLabels(selectedColumns, displayColumns, columnGroups);

        return new
        {
            QueryDescription = queryText,
            Action = action,
            ReportType = filter.ReportType,
            ReportTypeLabel = GetReportTypeLabel(filter.ReportType),
            RowCount = rowCount,
            Filters = BuildFiltersObject(filter, adminSectorId, adminSectorName, filterSectorName),
            SelectedColumns = selectedColumns,
            SelectedColumnLabels = columnLabels,
            DisplayedColumns = displayColumns?.Select(c => new { c.Key, c.Label }).ToList(),
            DisplayedColumnCount = displayColumns?.Count ?? columnLabels.Count
        };
    }

    public static string BuildQueryText(
        ReportFilter filter,
        List<string> selectedColumns,
        List<ReportDisplayColumn>? displayColumns,
        List<ColumnGroup> columnGroups,
        int? adminSectorId,
        string? adminSectorName,
        string? filterSectorName,
        int rowCount,
        string action)
    {
        var sb = new StringBuilder();
        var isPersonReport = filter.ReportType is "Disabled" or "ChronicSick" or "Pregnant" or "Nursing";

        sb.AppendLine($"الإجراء: {(action == "ExportExcel" ? "تصدير Excel" : "معاينة التقرير")}");
        sb.AppendLine($"نوع التقرير: {GetReportTypeLabel(filter.ReportType)}");
        sb.AppendLine($"مصدر البيانات: {(isPersonReport ? "صف لكل شخص يطابق شرط التقرير" : "صف لكل عائلة (تسجيل واحد)")}");

        if (isPersonReport)
            sb.AppendLine($"شرط الشخص: {GetPersonCriteriaLabel(filter.ReportType)}");

        sb.AppendLine("── الفلاتر ──");

        if (adminSectorId.HasValue)
            sb.AppendLine($"• قيد المندوب (القاطع): {adminSectorName ?? adminSectorId.ToString()}");

        if (filter.SectorId.HasValue)
            sb.AppendLine($"• القاطع: {filterSectorName ?? filter.SectorId.ToString()}");
        else if (!adminSectorId.HasValue)
            sb.AppendLine("• القاطع: الكل");

        sb.AppendLine($"• الحالة: {GetStatusLabel(filter.Status)}");
        sb.AppendLine($"• الجنس: {GetGenderLabel(filter.Gender)}");
        sb.AppendLine($"• الحالة الصحية: {(string.IsNullOrEmpty(filter.HealthStatus) ? "الكل" : filter.HealthStatus)}");

        if (filter.AgeFrom.HasValue || filter.AgeTo.HasValue)
        {
            var from = filter.AgeFrom?.ToString() ?? "—";
            var to = filter.AgeTo?.ToString() ?? "—";
            var ageTarget = isPersonReport ? "الشخص المعني" : "رب الأسرة";
            sb.AppendLine($"• العمر ({ageTarget}): من {from} إلى {to}");
        }
        else
            sb.AppendLine("• العمر: الكل");

        sb.AppendLine($"• بحث: {(string.IsNullOrWhiteSpace(filter.Search) ? "—" : filter.Search)}");

        if (!isPersonReport)
            sb.AppendLine($"• عرض أفراد العائلة في البيانات: {(filter.IncludeMembers ? "نعم" : "لا")}");

        sb.AppendLine("── الأعمدة المختارة ──");
        var labels = ResolveColumnLabels(selectedColumns, displayColumns, columnGroups);
        if (labels.Count == 0)
            sb.AppendLine("(لا يوجد أعمدة)");
        else
            sb.AppendLine(string.Join(" | ", labels));

        if (displayColumns != null && displayColumns.Count > labels.Count)
            sb.AppendLine($"── أعمدة العرض الفعلية ({displayColumns.Count}) ──");

        sb.AppendLine("── الاستعلام (منطق التطبيق) ──");
        sb.Append(BuildEfQueryOutline(filter, selectedColumns, adminSectorId, isPersonReport));

        sb.AppendLine($"── النتيجة: {rowCount} صف ──");
        return sb.ToString().TrimEnd();
    }

    private static string BuildEfQueryOutline(ReportFilter filter, List<string> selectedColumns, int? adminSectorId, bool isPersonReport)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FROM FamilyRegistrations f");
        sb.AppendLine("  JOIN Persons head ON f.FamilyHeadId = head.Id");
        sb.AppendLine("  LEFT JOIN Sectors s ON f.SectorId = s.Id");
        if (!isPersonReport && (filter.IncludeMembers || selectedColumns.Contains("Wives") || selectedColumns.Contains("Children") || selectedColumns.Contains("OtherMembers")))
            sb.AppendLine("  INCLUDE Members → Person");

        var wheres = new List<string>();
        if (adminSectorId.HasValue)
            wheres.Add($"f.SectorId = {adminSectorId.Value}  /* قيد المندوب */");
        if (filter.SectorId.HasValue)
            wheres.Add($"f.SectorId = {filter.SectorId.Value}");
        if (!string.IsNullOrEmpty(filter.Status))
            wheres.Add($"f.ApprovalStatus = '{filter.Status}'");
        if (!string.IsNullOrWhiteSpace(filter.Search))
            wheres.Add($"head.IdNumber / head.FullName / f.RecordId CONTAINS '{filter.Search}'");

        if (isPersonReport)
        {
            sb.AppendLine("  EXPAND: head + all Members as persons");
            wheres.Add(GetPersonCriteriaSql(filter.ReportType));
            if (!string.IsNullOrEmpty(filter.Gender))
                wheres.Add($"person.Gender = '{filter.Gender}'");
            if (!string.IsNullOrEmpty(filter.HealthStatus))
                wheres.Add($"person.HealthStatus = '{filter.HealthStatus}'");
            if (filter.AgeFrom.HasValue)
                wheres.Add($"person.Age >= {filter.AgeFrom}");
            if (filter.AgeTo.HasValue)
                wheres.Add($"person.Age <= {filter.AgeTo}");
        }
        else
        {
            if (!string.IsNullOrEmpty(filter.Gender))
                wheres.Add($"head.Gender = '{filter.Gender}'");
            if (!string.IsNullOrEmpty(filter.HealthStatus))
                wheres.Add($"head.HealthStatus = '{filter.HealthStatus}'");
        }

        if (wheres.Count > 0)
        {
            sb.AppendLine("WHERE");
            sb.AppendLine("  " + string.Join("\n  AND ", wheres));
        }

        sb.AppendLine("ORDER BY f.RegistrationTimestamp DESC");
        return sb.ToString();
    }

    private static Dictionary<string, object?> BuildFiltersObject(
        ReportFilter filter,
        int? adminSectorId,
        string? adminSectorName,
        string? filterSectorName)
    {
        return new Dictionary<string, object?>
        {
            ["ReportType"] = GetReportTypeLabel(filter.ReportType),
            ["AdminSectorRestriction"] = adminSectorId.HasValue ? (adminSectorName ?? adminSectorId.ToString()) : null,
            ["Sector"] = filter.SectorId.HasValue ? (filterSectorName ?? filter.SectorId.ToString()) : "الكل",
            ["Status"] = GetStatusLabel(filter.Status),
            ["Gender"] = GetGenderLabel(filter.Gender),
            ["HealthStatus"] = string.IsNullOrEmpty(filter.HealthStatus) ? "الكل" : filter.HealthStatus,
            ["AgeFrom"] = filter.AgeFrom,
            ["AgeTo"] = filter.AgeTo,
            ["Search"] = filter.Search,
            ["IncludeMembers"] = filter.IncludeMembers
        };
    }

    private static List<string> ResolveColumnLabels(List<string> selectedColumns, List<ReportDisplayColumn>? displayColumns, List<ColumnGroup> columnGroups)
    {
        if (displayColumns != null && displayColumns.Count > 0)
            return displayColumns.Select(c => c.Label).ToList();

        var allCols = columnGroups.SelectMany(g => g.Columns).ToDictionary(c => c.Key, c => c.Label);
        return selectedColumns.Select(k => allCols.GetValueOrDefault(k, k)).ToList();
    }

    public static string GetReportTypeLabel(string reportType) => reportType switch
    {
        "Normal" => "تقرير عادي (عائلة)",
        "Disabled" => "ذوي إعاقة (صف لكل شخص)",
        "ChronicSick" => "أمراض مزمنة (صف لكل شخص)",
        "Pregnant" => "حوامل (صف لكل شخص)",
        "Nursing" => "مرضعات (صف لكل شخص)",
        _ => reportType
    };

    private static string GetPersonCriteriaLabel(string reportType) => reportType switch
    {
        "Disabled" => "وجود نوع إعاقة",
        "ChronicSick" => "وجود مرض مزمن",
        "Pregnant" => "حامل",
        "Nursing" => "مرضع",
        _ => "—"
    };

    private static string GetPersonCriteriaSql(string reportType) => reportType switch
    {
        "Disabled" => "person.DisabilityTypes IS NOT NULL AND person.DisabilityTypes <> ''",
        "ChronicSick" => "person.ChronicDiseases IS NOT NULL AND person.ChronicDiseases <> ''",
        "Pregnant" => "person.IsPregnant = true",
        "Nursing" => "person.IsNursing = true",
        _ => "1=1"
    };

    private static string GetStatusLabel(string? status) => status switch
    {
        "Approved" => "مقبول",
        "Pending" => "قيد المراجعة",
        "Rejected" => "مرفوض",
        _ => "الكل"
    };

    private static string GetGenderLabel(string? gender) => gender switch
    {
        "male" => "ذكر",
        "female" => "أنثى",
        "ذكر" => "ذكر",
        "أنثى" => "أنثى",
        _ => "الكل"
    };
}
