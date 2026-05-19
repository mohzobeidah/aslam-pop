using System.ComponentModel.DataAnnotations;

namespace CampRegistrationApp.Models
{
    public enum RegistrationApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum NeedPriority
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
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
        public string? PasswordHash { get; set; }

        // Refugee Needs (Orders)
        public NeedPriority NeedTents { get; set; } = NeedPriority.None;
        public NeedPriority NeedBlankets { get; set; } = NeedPriority.None;
        public NeedPriority NeedMattresses { get; set; } = NeedPriority.None;
        public NeedPriority NeedKitchenTools { get; set; } = NeedPriority.None;
        public NeedPriority NeedTarpaulins { get; set; } = NeedPriority.None;
        public NeedPriority NeedClothes { get; set; } = NeedPriority.None;
        public NeedPriority NeedHygieneKit { get; set; } = NeedPriority.None;

        public RegistrationApprovalStatus ApprovalStatus { get; set; } = RegistrationApprovalStatus.Pending;
        public int? ApprovedById { get; set; }
        public virtual Admin? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public virtual ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();
    }
}
