using System.Text.Json;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CampRegistrationApp.Middleware;

public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await LogExceptionAsync(context, ex, scopeFactory);
            throw;
        }
    }

    private async Task LogExceptionAsync(HttpContext context, Exception ex, IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var userId = context.Session?.GetInt32("AdminId") ?? 0;
        var adminName = context.Session?.GetString("AdminName") ?? "";

        var route = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";

        try
        {
            db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Role = context.Session?.GetString("AdminRole") ?? "",
                Action = "Exception",
                TableName = "System",
                RecordId = null,
                OldValues = JsonSerializer.Serialize(new
                {
                    route,
                    user = adminName
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    exceptionType = ex.GetType().Name,
                    message = ex.Message,
                    stackTrace = ex.StackTrace?.Substring(0, Math.Min(ex.StackTrace.Length, 2000)),
                    innerException = ex.InnerException?.Message
                }),
                IPAddress = context.Connection.RemoteIpAddress?.ToString(),
                Source = "Middleware",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch
        {
            // Silently fail — never throw from exception handler
        }
    }
}

public static class ExceptionLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionLoggingMiddleware>();
    }
}
