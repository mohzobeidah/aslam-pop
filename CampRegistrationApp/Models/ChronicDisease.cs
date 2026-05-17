using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models;

public class ChronicDisease
{
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
