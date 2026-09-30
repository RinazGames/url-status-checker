namespace UrlStatusChecker.Models;

public class CheckResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SiteId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public bool IsAvailable { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}