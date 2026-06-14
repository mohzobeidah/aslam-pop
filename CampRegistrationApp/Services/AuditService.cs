using System.Text.Json;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CampRegistrationApp.Services;

public interface IAuditService
{
    Task LogAsync(int userId, string action, string tableName, string? recordId, object? oldValues, object? newValues,
        string? ipAddress = null, string? source = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpAccessor, ILogger<AuditService> logger)
    {
        _context = context;
        _httpAccessor = httpAccessor;
        _logger = logger;
    }

    public async Task LogAsync(int userId, string action, string tableName, string? recordId, object? oldValues, object? newValues,
        string? ipAddress = null, string? source = null)
    {
        try
        {
            var httpContext = _httpAccessor.HttpContext;
            Admin? admin = null;
            try
            {
                admin = await _context.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit: failed to resolve admin {UserId} for audit log", userId);
            }

            if (source == null)
            {
                var userAgent = httpContext?.Request.Headers["User-Agent"].ToString()?.ToLowerInvariant();
                if (string.IsNullOrEmpty(userAgent))
                {
                    source = "Web";
                }
                else
                {
                    string[] mobileKeywords = { "mobile", "android", "iphone", "ipad", "ipod", "blackberry", "windows phone", "opera mini", "iemobile" };
                    source = mobileKeywords.Any(k => userAgent.Contains(k)) ? "Mobile" : "Web";
                }
            }

            string? serializedOld = null;
            string? serializedNew = null;
            try
            {
                serializedOld = oldValues != null ? JsonSerializer.Serialize(oldValues) : null;
                serializedNew = newValues != null ? JsonSerializer.Serialize(newValues) : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit: failed to serialize values for {Action} on {Table}", action, tableName);
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Role = admin?.Role.ToString() ?? "",
                SectorId = admin?.SectorId,
                Action = action,
                TableName = tableName,
                RecordId = recordId,
                OldValues = serializedOld,
                NewValues = serializedNew,
                IPAddress = ipAddress ?? httpContext?.Connection.RemoteIpAddress?.ToString(),
                Source = source
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit: failed to log {Action} on {Table} (userId={UserId}, recordId={RecordId})",
                action, tableName, userId, recordId ?? "null");

            try
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Role = "",
                    Action = "AuditError",
                    TableName = tableName,
                    RecordId = recordId,
                    NewValues = $"{{\"originalAction\":\"{JsonSerializer.Serialize(action)}\",\"error\":\"{ex.Message}\"}}",
                    Source = "Web"
                });
                await _context.SaveChangesAsync();
            }
            catch
            {
            }
        }
    }
}
