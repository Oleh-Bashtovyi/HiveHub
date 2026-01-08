using HiveHub.API.Hubs;
using HiveHub.API.Services;
using HiveHub.Application.Interfaces;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Infrastructure.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(HiveHub.Application.BllMarker).Assembly);
});

builder.Services.AddSingleton<IIdGenerator, IdGenerator>();
builder.Services.AddSingleton<ISpyGamePublisher, SignalRSpyGamePublisher>();
builder.Services.AddSingleton<IConnectionMappingService, ConnectionMappingService>();
builder.Services.AddLogging();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("Running in DEVELOPMENT mode (In-Memory Storage)");
    builder.Services.AddSingleton<ISpyGameRepository, InMemorySpyGameRepository>();
}
else
{
    Console.WriteLine("Running in PRODUCTION mode (Redis Storage)");
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
                                ?? "localhost:6379"; // Fallback

    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnectionString));

    builder.Services.AddSingleton<IRoomStorage, RedisRoomStorage>();
    builder.Services.AddSingleton<ISpyGameRepository, RedisSpyGameRepository>();
}

builder.Services.AddHostedService<RoomCleanupService>();

var app = builder.Build();

app.UseCors();
app.MapHub<SpyGameHub>("/spy-game-hub");
app.MapGet("/", () => $"SpyGame Server is running! Mode: {app.Environment.EnvironmentName}");
app.Run();








public class RoomCleanupService : BackgroundService
{
    private readonly ISpyGameRepository _repository;
    private readonly ILogger<RoomCleanupService> _logger;

    public RoomCleanupService(ISpyGameRepository repository, ILogger<RoomCleanupService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var checkInterval = TimeSpan.FromMinutes(1);
        var expirationThreshold = TimeSpan.FromMinutes(10);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var removedCount = await _repository.RemoveInactiveRoomsAsync(expirationThreshold);

                if (removedCount > 0)
                {
                    _logger.LogInformation("Cleanup: Removed {Count} inactive rooms.", removedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during room cleanup.");
            }

            await Task.Delay(checkInterval, stoppingToken);
        }
    }
}