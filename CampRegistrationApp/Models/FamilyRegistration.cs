using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models
{
    public enum RegistrationApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class FamilyRegistration
    {
        public int Id { get; set; }
        [Required]
        public string RecordId { get; set; } = string.Empty;
        public DateTime RegistrationTimestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public int FamilyHeadId { get; set; }
        public virtual Person FamilyHead { get; set; } = null!;

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
        [Required]
        public string Sector { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Wallet { get; set; }
        public string? WalletType { get; set; }
        public string? PasswordHash { get; set; }
        public string? StatusNotes { get; set; }

        public RegistrationApprovalStatus ApprovalStatus { get; set; } = RegistrationApprovalStatus.Pending;
        public int? ApprovedById { get; set; }
        public virtual Admin? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public bool IsDeleted { get; set; }
        public int? DeletedById { get; set; }
        public virtual Admin? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();
        public virtual ICollection<FamilyDesire> FamilyDesires { get; set; } = new List<FamilyDesire>();
    }
}
