using System.Diagnostics;
using UrlStatusChecker.Models;
using UrlStatusChecker.Services.Interfaces;

namespace UrlStatusChecker.Services;

public class UrlCheckService : IUrlCheckService
{
    // Получаем клиент из контейнера.
    private readonly HttpClient _httpClient;
    public UrlCheckService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CheckResult> CheckUrlAsync(MonitoredSite site)
    {
        // Создаём возвращаемый объект.
        var result = new CheckResult
        {
            SiteId = site.Id,
            Timestamp = DateTime.UtcNow
        };

        // Для замера времени, фиксируем начало.
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Создаем запрос вручную, чтобы добавить заголовок запроса.
            using var request = new HttpRequestMessage(HttpMethod.Get, site.Url);
            // Имитация браузера.
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) UrlStatusChecker/1.0");

            // Отправляем пакет.
            var response = await _httpClient.SendAsync(request);
            // Фиксируем время запроса.
            stopwatch.Stop();

            // Записываем в результат.
            result.StatusCode = (int)response.StatusCode;
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            result.IsAvailable = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            // Фиксируем время запроса.
            stopwatch.Stop();
            // Данные не получили, как ошибка.
            result.StatusCode = 0;
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            result.IsAvailable = false;
            result.ErrorMessage = ex.Message;
        }

        // Возвращаем рузельтат.
        return result;
    }
}