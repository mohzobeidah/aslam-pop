namespace CampRegistrationApp.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public int StatusCode { get; set; } = 500;

    public string? StatusText { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public string? IconType { get; set; }

    public bool ShowHomeButton { get; set; } = true;

    public bool ShowReportButton { get; set; }
}
