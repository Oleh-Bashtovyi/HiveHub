using HiveHub.API.Hubs;
using HiveHub.API.Services;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Infrastructure.BackgroundJobs;
using HiveHub.Infrastructure.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --- Core Services ---
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(HiveHub.Application.BllMarker).Assembly);
});

builder.Services.AddScoped<SpyGameEventsContext>();
builder.Services.AddSingleton<IIdGenerator, IdGenerator>();
builder.Services.AddSingleton<ISpyGamePublisher, SignalRSpyGamePublisher>();
builder.Services.AddLogging();

// --- CORS ---
/*builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});*/
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

if (builder.Environment.IsDevelopment() || true)
{
    Console.WriteLine("Running in DEVELOPMENT mode (In-Memory Storage & Scheduler)");

    builder.Services.AddSingleton<ISpyGameRepository, InMemorySpyGameRepository>();
    builder.Services.AddSingleton<ITaskScheduler, InMemoryTaskScheduler>();

    builder.Services.AddHostedService<InMemoryTaskWorker>();
    builder.Services.AddHostedService<RoomCleanupService>();
}
// UNAVAILABLE
/*else
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
}*/

var app = builder.Build();

app.UseCors();
app.MapHub<SpyGameHub>("/spy-game-hub");
app.MapGet("/", () => $"SpyGame Server is running! Mode: {app.Environment.EnvironmentName}");

app.Run();
