using System.Text.Json;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CampRegistrationApp.Services;

public interface IAuditService
{
    Task LogAsync(int userId, string action, string tableName, string? recordId, object? oldValues, object? newValues,
        string? ipAddress = null, string source = "Web");
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpAccessor)
    {
        _context = context;
        _httpAccessor = httpAccessor;
    }

    public async Task LogAsync(int userId, string action, string tableName, string? recordId, object? oldValues, object? newValues,
        string? ipAddress = null, string source = "Web")
    {
        var httpContext = _httpAccessor.HttpContext;
        var admin = await _context.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Id == userId);

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Role = admin?.Role.ToString() ?? "",
            SectorId = admin?.SectorId,
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IPAddress = ipAddress ?? httpContext?.Connection.RemoteIpAddress?.ToString(),
            Source = source
        });
        await _context.SaveChangesAsync();
    }
}
