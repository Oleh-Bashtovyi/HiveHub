using HiveHub.API.Hubs;
using HiveHub.API.Services;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(HiveHub.Application.BllMarker).Assembly);
});

// Реєструємо сервіси
builder.Services.AddSingleton<SpyGameManager>();
builder.Services.AddSingleton<IIdGenerator, IdGenerator>();
builder.Services.AddSingleton<ISpyGamePublisher, SignalRSpyGamePublisher>();

// Додаємо логування
builder.Services.AddLogging();

// Додаємо CORS якщо потрібно
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Ваші frontend URLs
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Додаємо background service для очищення неактивних кімнат
builder.Services.AddHostedService<RoomCleanupService>();

var app = builder.Build();

app.UseCors();

app.MapHub<SpyGameHub>("/spy-game-hub");

app.MapGet("/", () => "SpyGame Server is running!");

app.Run();

// Background service для очищення неактивних кімнат
public class RoomCleanupService : BackgroundService
{
    private readonly ILogger<RoomCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public RoomCleanupService(
        ILogger<RoomCleanupService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var gameManager = scope.ServiceProvider.GetRequiredService<SpyGameManager>();

                var removedCount = await gameManager.RemoveInactiveRoomsAsync(TimeSpan.FromMinutes(3));

                if (removedCount > 0)
                {
                    _logger.LogInformation("Removed {Count} inactive rooms", removedCount);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in room cleanup service");
            }
        }
    }
}