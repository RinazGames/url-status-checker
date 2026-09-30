using Microsoft.AspNetCore.Mvc;
using UrlStatusChecker.Models;
using UrlStatusChecker.Services.Interfaces;

namespace UrlStatusChecker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SitesController : ControllerBase
{
    // Сервисы хранения и проверки.
    private readonly IJsonStorageService _storageService;
    private readonly IUrlCheckService _checkService;

    public SitesController(IJsonStorageService storageService, IUrlCheckService checkService)
    {
        _storageService = storageService;
        _checkService = checkService;
    }


    [HttpGet]
    public async Task<ActionResult<List<MonitoredSite>>> GetAllSites()
    {
        // Получение всех сайтов.
        var sites = await _storageService.GetSitesAsync();
        // Возвращаем 200.
        return Ok(sites);
    }

    [HttpPost]
    public async Task<ActionResult<MonitoredSite>> AddSite([FromBody] MonitoredSite site)
    {
        // Проверяем валидность адреса.
        if (!Uri.TryCreate(site.Url, UriKind.Absolute, out _))
        {
            // Возвращаем 400.
            return BadRequest("Invalid URL format");
        }

        // Получаем список сайтов и добавляем новый.
        var sites = await _storageService.GetSitesAsync();
        sites.Add(site);
        // Сохраняем список с новым сайтов.
        await _storageService.SaveSitesAsync(sites);

        // Возвращаем 201 с результатом.
        return CreatedAtAction(nameof(GetAllSites), new { id = site.Id }, site);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSite(string id, [FromBody] MonitoredSite updatedSite)
    {
        // Получаем список сайтов и ищем нужный.
        var sites = await _storageService.GetSitesAsync();
        var site = sites.FirstOrDefault(s => s.Id == id);

        // Если не нашли сайт.
        if (site == null)
        {
            // Возвращаем 404.
            return NotFound();
        }

        // Формируем с данными.
        site.Url = updatedSite.Url;
        site.Name = updatedSite.Name;
        site.Category = updatedSite.Category;
        site.CheckInterval = updatedSite.CheckInterval;
        site.IsActive = updatedSite.IsActive;

        // Сохраняем список с обновлённым сайтом.
        await _storageService.SaveSitesAsync(sites);
        // Возвращаем 204.
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSite(string id)
    {
        // Получаем список сайтов и ищем нужный.
        var sites = await _storageService.GetSitesAsync();
        var site = sites.FirstOrDefault(s => s.Id == id);

        // Если не нашли сайт.
        if (site == null)
        {
            // Возвращаем 404.
            return NotFound();
        }

        // УДаляем из списка.
        sites.Remove(site);
        // Сохраняем список уже без сайта.
        await _storageService.SaveSitesAsync(sites);
        // Возвращаем 204.
        return NoContent();
    }

    [HttpPost("{id}/check")]
    public async Task<ActionResult<CheckResult>> ManualCheck(string id)
    {
        // Получаем список сайтов и ищем нужный.
        var sites = await _storageService.GetSitesAsync();
        var site = sites.FirstOrDefault(s => s.Id == id);

        // Если не нашли сайт.
        if (site == null)
        {
            // Возвращаем 404.
            return NotFound();
        }

        // Запускаем проверку сайта на доступность.
        var result = await _checkService.CheckUrlAsync(site);
        await _storageService.AddCheckResultAsync(result);

        // Обновляем время последней проверки.
        site.LastCheckedAt = result.Timestamp;
        await _storageService.SaveSitesAsync(sites);

        // Возвращаем 200.
        return Ok(result);
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<List<CheckResult>>> GetHistory(string id)
    {
        // Получаем список результатов для конкретного сайта.
        var history = await _storageService.GetHistoryAsync(id);
        // Возвращаем 200.
        return Ok(history);
    }
}