namespace CampRegistrationApp.Models;

public class Notification
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public virtual Admin Admin { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = JerusalemTime.Now;
}
