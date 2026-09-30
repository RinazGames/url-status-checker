using UrlStatusChecker.Models;
using UrlStatusChecker.Services.Interfaces;

namespace UrlStatusChecker.BackgroundServices;

public class MonitorWorker : BackgroundService
{
    // Провайдер всех зависимостей.
    private readonly IServiceProvider _serviceProvider;
    // Логгер для записи событий.
    private readonly ILogger<MonitorWorker> _logger;

    // Задержка после успешной проверки (в минутах).
    private const int CHECK_DELAY = 1;

    public MonitorWorker(IServiceProvider serviceProvider, ILogger<MonitorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // Переопределяем основной абстрактный метод выполнения.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Логируем начало работы.
        _logger.LogInformation("Monitor Worker started.");

        // Пока не получили токен на остановку.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Новая область видимости для зависимостей.
                using (var scope = _serviceProvider.CreateScope())
                {
                    // Получаем экземпляры сервисов текущего скопа.
                    var storageService = scope.ServiceProvider.GetRequiredService<IJsonStorageService>();
                    var checkService = scope.ServiceProvider.GetRequiredService<IUrlCheckService>();

                    // Загружаем список всех сайтов и фильтруем по активности.
                    var sites = await storageService.GetSitesAsync();
                    var activeSites = sites.Where(s => s.IsActive).ToList();

                    // Перебираем каждый активный сайт.
                    foreach (var site in activeSites)
                    {
                        // Если приложений остановилось во время цикла, выходим.
                        if (stoppingToken.IsCancellationRequested) break;

                        // Вызов сервиса проверки.
                        var result = await checkService.CheckUrlAsync(site);
                        // Сохраняем время последней проверки.
                        await storageService.AddCheckResultAsync(result);

                        // Обновляем время последней проверки. 
                        site.LastCheckedAt = result.Timestamp;
                        await storageService.SaveSitesAsync(sites);

                        // Логируем результат проверки.
                        _logger.LogInformation(
                            "Checked {Url}: Status={Status}, Time={Time}ms",
                            site.Url, result.StatusCode, result.ResponseTimeMs);
                    }
                }

                // Ждем 1 минуту перед следующей проверкой.
                await Task.Delay(TimeSpan.FromMinutes(CHECK_DELAY), stoppingToken);
            }
            catch (Exception ex)
            {
                // Логируем ошибку.
                _logger.LogError(ex, "Error in Monitor Worker");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        // Логируем завершение.
        _logger.LogInformation("Monitor Worker stopped.");
    }
}