using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models.ViewModels;

public class PublicSubmitViewModel
{
    [Required(ErrorMessage = "الموضوع مطلوب")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "الرسالة مطلوبة")]
    public string Message { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SenderName { get; set; }

    [MaxLength(20)]
    public string? SenderPhone { get; set; }
}

public class ComplaintListViewModel
{
    public int Id { get; set; }
    public string TicketId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? SenderName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ComplaintDetailsViewModel
{
    public int Id { get; set; }
    public string TicketId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SenderName { get; set; }
    public string? SenderPhone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminResponse { get; set; }
    public string? ResolvedByName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ComplaintResponseViewModel
{
    public int Id { get; set; }
    public string? AdminResponse { get; set; }
}
