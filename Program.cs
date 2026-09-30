// Пространства имён.
using UrlStatusChecker.BackgroundServices;
using UrlStatusChecker.Services;
using UrlStatusChecker.Services.Interfaces;

// Создаём билдер.
var builder = WebApplication.CreateBuilder(args);

// Добавляем Swagger.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрируем сервисы.
builder.Services.AddSingleton<IJsonStorageService, JsonStorageService>();
builder.Services.AddScoped<IUrlCheckService, UrlCheckService>();
builder.Services.AddHttpClient<IUrlCheckService, UrlCheckService>();

// Добавляем фоновую службу.
builder.Services.AddHostedService<MonitorWorker>();

// Поддержка контроллеров.
builder.Services.AddControllers();

// Билд на основе конфигурации.
var app = builder.Build();

// Если в режиме разработки.
if (app.Environment.IsDevelopment())
{
    // Инициализация swagger.
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Перенаправление запросов на HTTPS.
app.UseHttpsRedirection();
// Обслуживание статических файлов из папки wwwroot.
app.UseStaticFiles();
// Использование авторизации.
app.UseAuthorization();
// Маршрутизация контроллеров.
app.MapControllers();

// Для одностраничного приложения.
app.MapFallbackToFile("index.html");

// Запускаем сервер.
app.Run();