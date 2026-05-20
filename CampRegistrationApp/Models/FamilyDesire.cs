using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models
{
    public class FamilyDesire
    {
        public int Id { get; set; }

        [Required]
        public int FamilyRegistrationId { get; set; }
        public virtual FamilyRegistration FamilyRegistration { get; set; } = null!;

        [Required]
        public int DesireId { get; set; }
        public virtual Desire Desire { get; set; } = null!;

        public int Order { get; set; }
    }
}
