namespace CampRegistrationApp.Models.ViewModels
{
    public class DashboardViewModel
    {
        public List<SectorDashboard> Sectors { get; set; } = new();
        public int TotalRegistrations { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalSectors { get; set; }
        public int TotalApprovedRefugees { get; set; }
    }

    public class SectorDashboard
    {
        public string? SectorName { get; set; }
        public string? Camp { get; set; }
        public string? Coordinate { get; set; }
        public string? Area { get; set; }
        public int ManufacturedTents { get; set; }
        public int HandmadeTents { get; set; }
        public int Bathrooms { get; set; }
        public int RegistrationCount { get; set; }
        public int ApprovedFamilyCount { get; set; }

        // Detailed statistics from the CTE query
        public int TotalPersons { get; set; }
        public int TotalFamilies { get; set; }
        public int FemaleLedFamilies { get; set; }
        public int ChildLedFamilies { get; set; }
        public int HeadCount { get; set; }
        public int WifeCount { get; set; }
        public int SonCount { get; set; }
        public int DaughterCount { get; set; }
        public int TotalMales { get; set; }
        public int TotalFemales { get; set; }

        // Age categories
        public int ChildrenUnder5 { get; set; }
        public int MaleUnder5 { get; set; }
        public int FemaleUnder5 { get; set; }
        public int InfantsUnder2 { get; set; }
        public int MaleInfants { get; set; }
        public int FemaleInfants { get; set; }
        public int Children2To5 { get; set; }
        public int Male2To5 { get; set; }
        public int Female2To5 { get; set; }
        public int ChildrenUnder18 { get; set; }
        public int MaleUnder18 { get; set; }
        public int FemaleUnder18 { get; set; }
        public int Adults18To60 { get; set; }
        public int MaleAdults { get; set; }
        public int FemaleAdults { get; set; }
        public int Elderly { get; set; }
        public int MaleElderly { get; set; }
        public int FemaleElderly { get; set; }

        // Disability & sickness
        public int Disabled { get; set; }
        public int MaleDisabled { get; set; }
        public int FemaleDisabled { get; set; }
        public int ChronicSick { get; set; }
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
        public string? RejectedByName { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }
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
        public string? HealthStatus { get; set; }
        public int MemberCount { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
    }

    public class RefugeeListPageViewModel
    {
        public List<RefugeeViewModel> Refugees { get; set; } = new();
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public string? SectorFilter { get; set; }
        public string? SearchQuery { get; set; }
        public string? StatusFilter { get; set; }
        public Dictionary<string, int> SectorApprovedCounts { get; set; } = new();
    }
}
