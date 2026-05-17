using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models
{
    public class Attachment
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public virtual Person Person { get; set; } = null!;

        [Required]
        public string FileType { get; set; } = string.Empty; // MedicalReport, IDImage
        [Required]
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
