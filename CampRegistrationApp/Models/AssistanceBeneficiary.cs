using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models;

public enum BeneficiaryStatus
{
    Active,
    Draft,
    Rejected,
    Cancelled
}

public class AssistanceBeneficiary
{
    public int Id { get; set; }

    public int AssistanceId { get; set; }
    public virtual Assistance Assistance { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string NationalId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Phone { get; set; } = string.Empty;

    public int SectorId { get; set; }
    public virtual Sector Sector { get; set; } = null!;

    [MaxLength(200)]
    public string BenefitType { get; set; } = string.Empty;

    // Legacy columns kept nullable for DB compatibility
    public string? FileNumber { get; set; }
    public string? FamilyName { get; set; }
    public string? City { get; set; }
    public int? FamilyCount { get; set; }

    public BeneficiaryStatus Status { get; set; } = BeneficiaryStatus.Draft;

    public string? Notes { get; set; }

    public int CreatedById { get; set; }
    public virtual Admin CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = JerusalemTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public int? ImportId { get; set; }
    public virtual AssistanceImport? Import { get; set; }

    public bool IsDeleted { get; set; }
}
