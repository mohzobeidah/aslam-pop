using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models
{
    public class Desire
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
