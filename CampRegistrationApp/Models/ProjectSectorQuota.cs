namespace CampRegistrationApp.Models;

public class ProjectSectorQuota
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public virtual Project Project { get; set; } = null!;

    public int SectorId { get; set; }
    public virtual Sector Sector { get; set; } = null!;

    public int MaxCount { get; set; }
}
