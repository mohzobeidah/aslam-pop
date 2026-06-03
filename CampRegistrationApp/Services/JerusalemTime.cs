public static class JerusalemTime
{
    private static readonly TimeZoneInfo _tz;

    static JerusalemTime()
    {
        try
        {
            _tz = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
        }
        catch
        {
            _tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");
        }
    }

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);
}
