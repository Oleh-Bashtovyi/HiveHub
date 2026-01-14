using HiveHub.API.Hubs;
using HiveHub.API.Services; // Ensure this namespace exists if used
using HiveHub.Application.Interfaces;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models;
using HiveHub.Infrastructure.BackgroundJobs;
using HiveHub.Infrastructure.Services;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// --- Core Services ---
builder.Services.AddSignalR();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(HiveHub.Application.BllMarker).Assembly);
});

builder.Services.AddScoped<SpyGameEventsContext>();
builder.Services.AddSingleton<IIdGenerator, IdGenerator>();
builder.Services.AddSingleton<ISpyGamePublisher, SignalRSpyGamePublisher>();
builder.Services.AddLogging();

// --- CORS ---
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
    Console.WriteLine("Running in DEVELOPMENT mode (In-Memory Storage & Scheduler)");

    builder.Services.AddSingleton<ISpyGameRepository, InMemorySpyGameRepository>();
    builder.Services.AddSingleton<ITaskScheduler, InMemoryTaskScheduler>();

    builder.Services.AddHostedService<InMemoryTaskWorker>();
    builder.Services.AddHostedService<RoomCleanupService>();
}
else
{
    Console.WriteLine("Running in PRODUCTION mode (Redis Storage & Scheduler)");

    var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
                                ?? "localhost:6379";

    // Redis Connection
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnectionString));

    // RedLock (Distributed Lock)
    builder.Services.AddSingleton<IDistributedLockFactory>(sp =>
    {
        var redis = sp.GetRequiredService<IConnectionMultiplexer>();
        return RedLockFactory.Create(new List<RedLockMultiplexer>
        {
            new RedLockMultiplexer(redis)
        });
    });

    builder.Services.AddSingleton<IRoomStorage<SpyRoom>, RedisRoomStorage>();
    builder.Services.AddSingleton<ISpyGameRepository, RedisSpyGameRepository>();
    builder.Services.AddSingleton<ITaskScheduler, RedisTaskScheduler>();

    builder.Services.AddHostedService<RedisTaskWorker>();
}

var app = builder.Build();

app.UseCors();
app.MapHub<SpyGameHub>("/spy-game-hub");
app.MapGet("/", () => $"SpyGame Server is running! Mode: {app.Environment.EnvironmentName}");

app.Run();
