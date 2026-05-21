using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models
{
    public enum AdminRole
    {
        Admin,
        Mandoob,
        Viewer
    }

    public class Admin
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        public string Mobile { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public AdminRole Role { get; set; } = AdminRole.Mandoob;

        public int? SectorId { get; set; }
        public virtual Sector? Sector { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
