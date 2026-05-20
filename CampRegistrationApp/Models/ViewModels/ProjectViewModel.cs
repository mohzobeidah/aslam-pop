using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models.ViewModels;

public class ProjectViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم المشروع مطلوب")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "تاريخ بداية الترشيح مطلوب")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "تاريخ نهاية الترشيح مطلوب")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

    [Required(ErrorMessage = "العدد المطلوب مطلوب")]
    [Range(1, 1000, ErrorMessage = "العدد المطلوب يجب أن يكون بين 1 و 1000")]
    public int RequiredCount { get; set; } = 10;

    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
    public string? Description { get; set; }
    public string? Notes { get; set; }

    public List<SectorQuotaViewModel> SectorQuotas { get; set; } = new();
}

public class SectorQuotaViewModel
{
    public int SectorId { get; set; }
    public string SectorName { get; set; } = string.Empty;
    public int MaxCount { get; set; }
}

public class ProjectListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int RequiredCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int NominationCount { get; set; }
}
