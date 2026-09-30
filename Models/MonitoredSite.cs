namespace UrlStatusChecker.Models;

public class MonitoredSite
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CheckInterval { get; set; } = 5; // минуты
    public bool IsActive { get; set; } = true;
    public DateTime? LastCheckedAt { get; set; }
}