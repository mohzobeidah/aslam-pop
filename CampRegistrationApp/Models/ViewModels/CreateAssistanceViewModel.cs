using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models.ViewModels;

public class CreateAssistanceViewModel
{
    [Required(ErrorMessage = "اسم المساعدة مطلوب")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string AssistanceType { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Source { get; set; } = string.Empty;

    public DateTime AssistanceDate { get; set; } = DateTime.Today;

    public string? Description { get; set; }

    [Required(ErrorMessage = "القاطع مطلوب")]
    public int? SectorId { get; set; }
}
