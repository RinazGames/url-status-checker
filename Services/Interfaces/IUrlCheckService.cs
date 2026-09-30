using UrlStatusChecker.Models;

namespace UrlStatusChecker.Services.Interfaces;

public interface IUrlCheckService
{
    Task<CheckResult> CheckUrlAsync(MonitoredSite site);
}