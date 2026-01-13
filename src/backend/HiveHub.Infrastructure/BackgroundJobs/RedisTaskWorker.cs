using HiveHub.Application.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace HiveHub.Infrastructure.BackgroundJobs;

public class RedisTaskWorker(
    IConnectionMultiplexer redis,
    IServiceProvider serviceProvider,
    ILogger<RedisTaskWorker> logger) : BaseTaskWorker(logger, serviceProvider)
{
    private readonly IConnectionMultiplexer _redis = redis;
    private const string ScheduleKey = "scheduler:tasks";
    private const int ProcessTasksAtOnce = 50;

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
}