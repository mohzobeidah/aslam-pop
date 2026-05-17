using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models
{
    public class Person
    {
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string SecondName { get; set; } = string.Empty;
        [Required]
        public string ThirdName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {SecondName} {ThirdName} {LastName}";

        [Required]
        public string IdNumber { get; set; } = string.Empty;
        [Required]
        public string Sector { get; set; } = string.Empty; // A, B, C, D
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string OriginalGovernorate { get; set; } = string.Empty;
        public string MaritalStatus { get; set; } = string.Empty;
        public string EmploymentStatus { get; set; } = string.Empty;
        public string EducationLevel { get; set; } = string.Empty;
        [Required]
        public string HealthStatus { get; set; } = string.Empty; // Healthy / Sick
        public string? ChronicDiseases { get; set; }
        public string? DisabilityTypes { get; set; }
        public bool HasInjury { get; set; }
        public DateTime? InjuryDate { get; set; }
        public string? InjuryDetails { get; set; }
        public bool IsHouseDestroyed { get; set; }
        public bool? IsPregnant { get; set; }
        public int? PregnancyMonth { get; set; }
        public bool? IsNursing { get; set; }
        public string? NursingInfantName { get; set; }
        public DateTime? NursingInfantDOB { get; set; }
        public string? NursingInfantID { get; set; }
        public bool IsPrisoner { get; set; }
        public string? Nationality { get; set; }

        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
