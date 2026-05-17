using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models;

public enum NominationStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Cancelled
}

public class Nomination
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public virtual Project Project { get; set; } = null!;

    public int PersonId { get; set; }
    public virtual Person Person { get; set; } = null!;

    public int SectorId { get; set; }
    public virtual Sector Sector { get; set; } = null!;

    public int DelegateId { get; set; }
    public virtual Admin Delegate { get; set; } = null!;

    public NominationStatus Status { get; set; } = NominationStatus.Draft;
    public string? Description { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int? ApprovedById { get; set; }
    public virtual Admin? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
