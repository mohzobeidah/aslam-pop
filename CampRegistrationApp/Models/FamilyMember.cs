using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models
{
    public class FamilyMember
    {
        public int Id { get; set; }
        public int RegistrationId { get; set; }
        public virtual FamilyRegistration Registration { get; set; } = null!;

        public int PersonId { get; set; }
        public virtual Person Person { get; set; } = null!;

        [Required]
        public string RelationshipToHead { get; set; } = string.Empty;
    }
}
