using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models;

public enum ProjectStatus
{
    [Display(Name = "مسودة")]
    Draft,
    [Display(Name = "نشط")]
    Active,
    [Display(Name = "مغلق")]
    Closed
}

public class Project
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int RequiredCount { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
    public string? Description { get; set; }
    public string? Notes { get; set; }

    public int CreatedById { get; set; }
    public virtual Admin CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = JerusalemTime.Now;

    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
