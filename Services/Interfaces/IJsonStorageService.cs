using UrlStatusChecker.Models;

namespace UrlStatusChecker.Services.Interfaces;

public interface IJsonStorageService
{
    Task<List<MonitoredSite>> GetSitesAsync();
    Task SaveSitesAsync(List<MonitoredSite> sites);
    Task<List<CheckResult>> GetHistoryAsync(string siteId);
    Task AddCheckResultAsync(CheckResult result);
}