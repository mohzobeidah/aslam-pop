using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models;

public enum ComplaintStatus
{
    Pending,
    InProgress,
    Resolved
}

public class Complaint
{
    public int Id { get; set; }

    [Required]
    [MaxLength(8)]
    public string TicketId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SenderName { get; set; }

    [MaxLength(20)]
    public string? SenderPhone { get; set; }

    public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;

    public string? AdminResponse { get; set; }

    public int? ResolvedById { get; set; }
    public virtual Admin? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public int? FamilyRegistrationId { get; set; }
    public virtual FamilyRegistration? FamilyRegistration { get; set; }

    public DateTime CreatedAt { get; set; } = JerusalemTime.Now;

    public bool IsDeleted { get; set; }
}
