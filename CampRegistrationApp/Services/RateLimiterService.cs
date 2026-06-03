using System.Collections.Concurrent;

namespace CampRegistrationApp.Services;

public class RateLimiterService : IRateLimiterService
{
    private readonly ConcurrentDictionary<string, List<DateTime>> _attempts = new();

    public bool IsRateLimited(string key, int maxAttempts, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var attempts = _attempts.GetOrAdd(key, _ => new List<DateTime>());

        lock (attempts)
        {
            attempts.RemoveAll(t => now - t > window);
            if (attempts.Count >= maxAttempts)
                return true;
            attempts.Add(now);
            return false;
        }
    }
}