namespace CampRegistrationApp.Models.ViewModels;

public class AuditLogListViewModel
{
    public List<AuditLog> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? ActionFilter { get; set; }
    public string? TableFilter { get; set; }
    public string? JsonFilter { get; set; }
}
