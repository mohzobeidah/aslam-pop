using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;

namespace CampRegistrationApp.Services;

public interface INotificationService
{
    Task NotifyMandoobsAsync(string sector, string message, string? link);
    Task NotifyAdminAsync(int adminId, string message, string? link);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task NotifyMandoobsAsync(string sector, string message, string? link)
    {
        var mandoobs = await _context.Admins
            .Include(a => a.Sector)
            .Where(a => a.Role == AdminRole.Mandoob && a.Sector != null && a.Sector.Name == sector)
            .ToListAsync();

        foreach (var mandoob in mandoobs)
        {
            _context.Notifications.Add(new Notification
            {
                AdminId = mandoob.Id,
                Message = message,
                Link = link
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task NotifyAdminAsync(int adminId, string message, string? link)
    {
        _context.Notifications.Add(new Notification
        {
            AdminId = adminId,
            Message = message,
            Link = link
        });
        await _context.SaveChangesAsync();
    }
}
