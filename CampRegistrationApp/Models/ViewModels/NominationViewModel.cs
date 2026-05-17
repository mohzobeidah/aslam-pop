using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models.ViewModels;

public class NominationRowViewModel
{
    public int? Id { get; set; }
    public int RowNumber { get; set; }
    public int? PersonId { get; set; }
    public string? PersonName { get; set; }
    public string? IdNumber { get; set; }
    public string? Phone { get; set; }
    public string? Sector { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Description { get; set; }
    public string? Notes { get; set; }
}

public class NominationPageViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public ProjectStatus ProjectStatus { get; set; }
    public int RequiredCount { get; set; }
    public int ExistingCount { get; set; }
    public List<NominationRowViewModel> Rows { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool IsPastEndDate { get; set; }
}
