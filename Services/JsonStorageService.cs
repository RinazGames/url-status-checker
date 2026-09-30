using System.Text.Json;
using UrlStatusChecker.Models;
using UrlStatusChecker.Services.Interfaces;

namespace UrlStatusChecker.Services;

public class JsonStorageService : IJsonStorageService
{
    // Директории.
    private readonly string _sitesFilePath;
    private readonly string _historyFilePath;
    // Семафор для ограничения одновременных запросов (1 слот).
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public JsonStorageService(IHostEnvironment environment)
    {
        // Формируем начальную директорию.
        var dataPath = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataPath);

        // Формируем пути.
        _sitesFilePath = Path.Combine(dataPath, "sites.json");
        _historyFilePath = Path.Combine(dataPath, "history.json");

        // Создание файлов, если их нет.
        InitializeFiles();
    }


    /// <summary>
    /// Создание пустых файлов, если их не существует.
    /// </summary>
    private void InitializeFiles()
    {
        if (!File.Exists(_sitesFilePath))
        {
            File.WriteAllText(_sitesFilePath, JsonSerializer.Serialize(new List<MonitoredSite>()));
        }

        if (!File.Exists(_historyFilePath))
        {
            File.WriteAllText(_historyFilePath, JsonSerializer.Serialize(new List<CheckResult>()));
        }
    }


    /// <summary>
    /// Чтение списка сайтов.
    /// </summary>
    public async Task<List<MonitoredSite>> GetSitesAsync()
    {
        // Блокировка доступа для других потоков.
        await _semaphore.WaitAsync();

        try
        {
            // Асинхронно читаем файл.
            var json = await File.ReadAllTextAsync(_sitesFilePath);
            // Возвращаем список классов из файла, либо пустой список (если пусто в файле).
            return JsonSerializer.Deserialize<List<MonitoredSite>>(json) ?? new List<MonitoredSite>();
        }
        finally
        {
            // Освобождаеи доступ при любом исходе.
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Запись спаска сайтов.
    /// </summary>
    public async Task SaveSitesAsync(List<MonitoredSite> sites)
    {
        // Блокировка доступа для других потоков.
        await _semaphore.WaitAsync();

        try
        {
            // Формируем json в читаемом формате.
            var json = JsonSerializer.Serialize(sites, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            // Асинхронно записываем в файл.
            await File.WriteAllTextAsync(_sitesFilePath, json);
        }
        finally
        {
            // Освобождаеи доступ при любом исходе.
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Чтение истории запросов.
    /// </summary>
    public async Task<List<CheckResult>> GetHistoryAsync(string siteId)
    {
        // Блокировка доступа для других потоков.
        await _semaphore.WaitAsync();

        try
        {
            // Читаем файл.
            var json = await File.ReadAllTextAsync(_historyFilePath);
            // Преобразуем в список результатов.
            var allResults = JsonSerializer.Deserialize<List<CheckResult>>(json) ?? new List<CheckResult>();
            // Возвращаем отфильтрованный список (по запрошенному сайту).
            return allResults.Where(r => r.SiteId == siteId).ToList();
        }
        finally
        {
            // Освобождаеи доступ при любом исходе.
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Добавляем результат запроса.
    /// </summary>
    public async Task AddCheckResultAsync(CheckResult result)
    {
        // Блокировка доступа для других потоков.
        await _semaphore.WaitAsync();

        try
        {
            // Читаем файл.
            var json = await File.ReadAllTextAsync(_historyFilePath);
            // Преобразуем в список результатов.
            var allResults = JsonSerializer.Deserialize<List<CheckResult>>(json) ?? new List<CheckResult>();

            // Добавляем в список новый результат.
            allResults.Add(result);

            // Ограничиваем историю последними 100 записями для каждого сайта.
            var limitedResults = allResults
                .GroupBy(r => r.SiteId)
                .SelectMany(g => g.OrderByDescending(r => r.Timestamp).Take(100))
                .ToList();

            // Формируем новый читаемый json.
            var newJson = JsonSerializer.Serialize(limitedResults, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            // Асинхронно записываем в файл.
            await File.WriteAllTextAsync(_historyFilePath, newJson);
        }
        finally
        {
            // Освобождаеи доступ при любом исходе.
            _semaphore.Release();
        }
    }
}