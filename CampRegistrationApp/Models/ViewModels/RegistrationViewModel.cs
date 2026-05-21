using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models.ViewModels
{
    public class RegistrationViewModel
    {
        public int Id { get; set; }
        public string? RecordId { get; set; }
        public int CurrentStep { get; set; } = 1;

        // Step 1: Family Head
        public PersonViewModel Head { get; set; } = new();

        // Step 2: Family Members
        public List<MemberViewModel> Members { get; set; } = new();

        // Step 3: Housing & Special Cases
        public bool IsChildHeaded { get; set; }
        public string? ChildHeadedDetails { get; set; }
        public bool IsFemaleHeaded { get; set; }
        public string? FemaleHeadedDetails { get; set; }
        public bool IsHusbandAbroad { get; set; }
        public bool SupportsOutsidePerson { get; set; }
        public string? OutsidePersonName { get; set; }
        public string? OutsidePersonRelation { get; set; }
        public bool LivesInTent { get; set; }
        public string? TentType { get; set; }
        public string? OtherTentType { get; set; }
        public bool HasBathroom { get; set; }
        public string? BathroomType { get; set; }
        public bool NeedsDiapers { get; set; }
        public string? DiaperDetails { get; set; }
        public bool HasMultipleFamiliesInTent { get; set; }
        public int? AdditionalFamiliesCount { get; set; }

        public string? StatusNotes { get; set; }

        // Refugee Desires - ordered list of selected desire IDs (1st choice = index 0, etc.)
        public List<int> DesireIds { get; set; } = new();

        public string? Password { get; set; }
    }

    public class PersonViewModel
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "الاسم الثاني مطلوب")]
        public string SecondName { get; set; } = string.Empty;
        [Required(ErrorMessage = "الاسم الثالث مطلوب")]
        public string ThirdName { get; set; } = string.Empty;
        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {SecondName} {ThirdName} {LastName}";

        [Required(ErrorMessage = "رقم الهوية مطلوب")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "رقم الهوية يجب أن يتكون من 9 أرقام")]
        public string IdNumber { get; set; } = string.Empty;
        [Required(ErrorMessage = "القاطع مطلوب")]
        public string Sector { get; set; } = string.Empty;
        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        public DateTime DateOfBirth { get; set; } = DateTime.Today;
        [Required(ErrorMessage = "الجنس مطلوب")]
        public string Gender { get; set; } = "ذكر";
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [RegularExpression(@"^(059|056)\d{7}$", ErrorMessage = "رقم الهاتف يجب أن يتكون من 10 أرقام ويبدأ بـ 059 أو 056")]
        public string PhoneNumber { get; set; } = string.Empty;
        public string OriginalGovernorate { get; set; } = string.Empty;
        public string MaritalStatus { get; set; } = string.Empty;
        public string EmploymentStatus { get; set; } = string.Empty;
        public string EducationLevel { get; set; } = string.Empty;
        public bool IsHouseDestroyed { get; set; }
        [Required(ErrorMessage = "الحالة الصحية مطلوبة")]
        public string HealthStatus { get; set; } = "سليم";
        public string? ChronicDiseases { get; set; }
        public string? DisabilityTypes { get; set; }
        public bool HasInjury { get; set; }
        public DateTime? InjuryDate { get; set; }
        public string? InjuryDetails { get; set; }
        public bool? IsPregnant { get; set; }
        public int? PregnancyMonth { get; set; }
        public bool? IsNursing { get; set; }
        public string? NursingInfantName { get; set; }
        public DateTime? NursingInfantDOB { get; set; }
        public string? NursingInfantID { get; set; }
        public bool IsPrisoner { get; set; }
        [RegularExpression(@"^(059|056)\d{7}$", ErrorMessage = "رقم المحفظة يجب أن يتكون من 10 أرقام ويبدأ بـ 059 أو 056")]
        public string? Wallet { get; set; }
        public string? BathroomStatus { get; set; }
        public string? MotherIdNumber { get; set; }
        public string? HeadIdImagePath { get; set; }
        public string? MedicalImagePath { get; set; }
        public List<string> UploadedFiles { get; set; } = new();
    }

    public class MemberViewModel
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "الاسم الثاني مطلوب")]
        public string SecondName { get; set; } = string.Empty;
        [Required(ErrorMessage = "الاسم الثالث مطلوب")]
        public string ThirdName { get; set; } = string.Empty;
        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {SecondName} {ThirdName} {LastName}";

        [Required(ErrorMessage = "رقم الهوية مطلوب")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "رقم الهوية يجب أن يتكون من 9 أرقام")]
        public string IdNumber { get; set; } = string.Empty;
        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        public DateTime DateOfBirth { get; set; } = DateTime.Today;
        [Required(ErrorMessage = "الجنس مطلوب")]
        public string Gender { get; set; } = "ذكر";
        public string OriginalGovernorate { get; set; } = string.Empty;
        public string MaritalStatus { get; set; } = string.Empty;
        public string EmploymentStatus { get; set; } = string.Empty;
        public string EducationLevel { get; set; } = string.Empty;
        [Required(ErrorMessage = "الحالة الصحية مطلوبة")]
        public string HealthStatus { get; set; } = "سليم";
        public string? ChronicDiseases { get; set; }
        public string? DisabilityTypes { get; set; }
        public bool HasInjury { get; set; }
        public DateTime? InjuryDate { get; set; }
        public string? InjuryDetails { get; set; }
        public bool? IsPregnant { get; set; }
        public int? PregnancyMonth { get; set; }
        public bool? IsNursing { get; set; }
        public string? NursingInfantName { get; set; }
        public DateTime? NursingInfantDOB { get; set; }
        public string? NursingInfantID { get; set; }
        public bool IsPrisoner { get; set; }
        public string? BathroomStatus { get; set; }
        public string? MotherIdNumber { get; set; }

        [Required(ErrorMessage = "صلة القرابة مطلوبة")]
        public string RelationshipToHead { get; set; } = string.Empty;
    }
}
