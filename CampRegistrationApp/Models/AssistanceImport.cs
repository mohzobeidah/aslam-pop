using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampRegistrationApp.Models;

public class AssistanceImport
{
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    public int ImportedById { get; set; }
    public virtual Admin ImportedBy { get; set; } = null!;

    public int SectorId { get; set; }
    public virtual Sector Sector { get; set; } = null!;

    public DateTime ImportedAt { get; set; } = JerusalemTime.Now;

    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailedRows { get; set; }
    public int DuplicateRows { get; set; }

    public string? ErrorFilePath { get; set; }

    [NotMapped]
    public List<string> ImportErrors { get; set; } = new();

    [NotMapped]
    public List<string> ImportWarnings { get; set; } = new();

    public virtual ICollection<AssistanceBeneficiary> Beneficiaries { get; set; } = new List<AssistanceBeneficiary>();
}
