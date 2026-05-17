using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models;

public enum AssistanceStatus
{
    Draft,
    Approved,
    Cancelled
}

public class Assistance
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string AssistanceType { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Source { get; set; } = string.Empty;

    public DateTime AssistanceDate { get; set; }

    public string? Description { get; set; }

    public int SectorId { get; set; }
    public virtual Sector Sector { get; set; } = null!;

    public AssistanceStatus Status { get; set; } = AssistanceStatus.Draft;

    public int CreatedById { get; set; }
    public virtual Admin CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? ApprovedById { get; set; }
    public virtual Admin? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? AttachmentsPath { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<AssistanceBeneficiary> Beneficiaries { get; set; } = new List<AssistanceBeneficiary>();
}
