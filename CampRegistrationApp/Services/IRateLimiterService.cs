namespace CampRegistrationApp.Services;

public interface IRateLimiterService
{
    bool IsRateLimited(string key, int maxAttempts, TimeSpan window);
}