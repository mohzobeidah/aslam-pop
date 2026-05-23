using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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

    [Range(1, int.MaxValue, ErrorMessage = "يرجى اختيار القاطع")]
    public int SectorId { get; set; }
    [BindNever]
    public virtual Sector Sector { get; set; } = null!;

    public AssistanceStatus Status { get; set; } = AssistanceStatus.Draft;

    public int CreatedById { get; set; }
    [BindNever]
    public virtual Admin CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? ApprovedById { get; set; }
    public virtual Admin? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? AttachmentsPath { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<AssistanceBeneficiary> Beneficiaries { get; set; } = new List<AssistanceBeneficiary>();
}
