namespace CampRegistrationApp.Models.ViewModels
{
    public class ReportViewModel
    {
        public List<ColumnGroup> ColumnGroups { get; set; } = new();
        public List<string> SelectedColumns { get; set; } = new();
        public ReportFilter Filter { get; set; } = new();
        public List<ReportRow> Rows { get; set; } = new();
        public int TotalCount { get; set; }
        public List<Sector> Sectors { get; set; } = new();
        public Dictionary<string, string> HeaderLabels { get; set; } = new();
    }

    public class ColumnGroup
    {
        public string GroupName { get; set; } = "";
        public List<ColumnDef> Columns { get; set; } = new();
    }

    public class ColumnDef
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public bool IsDefault { get; set; }
    }

    public class ReportFilter
    {
        public string ReportType { get; set; } = "Normal"; // Normal, Disabled, ChronicSick, Pregnant, Nursing
        public int? SectorId { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }
        public string? Gender { get; set; }
        public string? HealthStatus { get; set; }
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }
        public bool IncludeMembers { get; set; }
    }

    public class ReportRow
    {
        public Dictionary<string, object?> Values { get; set; } = new();
    }
}
