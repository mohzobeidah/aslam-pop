using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models.ViewModels;

public class EditAssistanceViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم المساعدة مطلوب")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string AssistanceType { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Source { get; set; } = string.Empty;

    public DateTime AssistanceDate { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = "القاطع مطلوب")]
    public int? SectorId { get; set; }
}
