using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models
{
    public class Sector
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Camp { get; set; }
        public string? Coordinate { get; set; }
        public string? Area { get; set; }
        public int ManufacturedTentsCount { get; set; }
        public int HandmadeTentsCount { get; set; }
        public int BathroomsCount { get; set; }

        public virtual ICollection<Admin> Admins { get; set; } = new List<Admin>();
    }
}
