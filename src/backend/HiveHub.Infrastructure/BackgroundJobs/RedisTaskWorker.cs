using HiveHub.Application.MediatR.SpyGame.Commands.HandleTimeout;
using HiveHub.Application.MediatR.SpyGame.Commands.HandleTimeUp;
using HiveHub.Application.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace HiveHub.Infrastructure.BackgroundJobs;

public class RedisTaskWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RedisTaskWorker> _logger;
    private const string ScheduleKey = "scheduler:tasks";
    private const int ProcessTasksAtOnce = 50;

    public RedisTaskWorker(
        IConnectionMultiplexer redis,
        IServiceProvider serviceProvider,
        ILogger<RedisTaskWorker> logger)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var tasks = await db.SortedSetRangeByScoreAsync(
                    ScheduleKey, 
                    double.NegativeInfinity, 
                    now, 
                    Exclude.None, 
                    Order.Ascending, 
                    0, 
                    ProcessTasksAtOnce);

                if (tasks.Length == 0)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                foreach (var redisValue in tasks)
                {
                    if (await db.SortedSetRemoveAsync(ScheduleKey, redisValue))
                    {
                        var task = JsonSerializer.Deserialize<ScheduledTask>(redisValue.ToString());
                        if (task != null)
                        {
                            await ProcessTaskAsync(task);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled tasks");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task ProcessTaskAsync(ScheduledTask task)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            if (task.Type == TaskType.SpyPlayerDisconnectTimeout)
            {
                if (task.TargetId != null)
                {
                    await mediator.Send(new HandlePlayerTimeoutCommand(task.RoomCode, task.TargetId));
                }
            }
            else if (task.Type == TaskType.SpyGameEndTimeUp)
            {
                await mediator.Send(new HandleGameTimeUpCommand(task.RoomCode));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command for task {Type} in room {Room}", task.Type, task.RoomCode);
        }
    }
}