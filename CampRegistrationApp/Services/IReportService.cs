using CampRegistrationApp.Models.ViewModels;

namespace CampRegistrationApp.Services
{
    public interface IReportService
    {
        List<ColumnGroup> GetColumnGroups();
        Task<List<ReportRow>> GetReportDataAsync(ReportFilter filter, List<string> selectedColumns, int? adminSectorId = null);
        Task<byte[]> GenerateExcelAsync(List<ReportRow> rows, List<string> selectedColumns);
        List<ReportDisplayColumn> ResolveDisplayColumns(List<ReportRow> rows, List<string> selectedColumns);
    }
}
