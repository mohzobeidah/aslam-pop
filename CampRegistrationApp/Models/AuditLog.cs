namespace CampRegistrationApp.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public int? SectorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? RecordId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IPAddress { get; set; }
    public string Source { get; set; } = "Web"; // Web / Excel / API
    public DateTime CreatedAt { get; set; } = JerusalemTime.Now;
}
