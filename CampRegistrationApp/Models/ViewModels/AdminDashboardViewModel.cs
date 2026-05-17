namespace CampRegistrationApp.Models.ViewModels
{
    public class DashboardViewModel
    {
        public List<SectorDashboard> Sectors { get; set; } = new();
        public int TotalRegistrations { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalSectors { get; set; }
    }

    public class SectorDashboard
    {
        public string? SectorName { get; set; }
        public string? Camp { get; set; }
        public string? Area { get; set; }
        public int ManufacturedTents { get; set; }
        public int HandmadeTents { get; set; }
        public int Bathrooms { get; set; }
        public int RegistrationCount { get; set; }
    }

    public class RegistrationApprovalViewModel
    {
        public int Id { get; set; }
        public string RecordId { get; set; } = string.Empty;
        public string HeadName { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public RegistrationApprovalStatus ApprovalStatus { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int MemberCount { get; set; }
    }

    public class RefugeeViewModel
    {
        public int Id { get; set; }
        public string RecordId { get; set; } = string.Empty;
        public string HeadName { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Sector { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Nationality { get; set; }
        public string? HealthStatus { get; set; }
        public int MemberCount { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
    }

    public class RefugeeListPageViewModel
    {
        public List<RefugeeViewModel> Refugees { get; set; } = new();
        public int TotalCount { get; set; }
        public string? SectorFilter { get; set; }
        public string? SearchQuery { get; set; }
    }
}
