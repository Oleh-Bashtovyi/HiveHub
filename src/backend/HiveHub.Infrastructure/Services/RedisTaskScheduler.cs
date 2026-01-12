using HiveHub.Application.Models;
using HiveHub.Application.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace HiveHub.Infrastructure.Services;

public class RedisTaskScheduler(IConnectionMultiplexer redis) : ITaskScheduler
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string ScheduleKey = "scheduler:tasks";

    public async Task ScheduleAsync(ScheduledTask task, TimeSpan delay)
    {
        var json = JsonSerializer.Serialize(task);
        var score = DateTimeOffset.UtcNow.Add(delay).ToUnixTimeSeconds();

        await _db.SortedSetAddAsync(ScheduleKey, json, score);
    }

    public async Task CancelAsync(ScheduledTask task)
    {
        var json = JsonSerializer.Serialize(task);
        await _db.SortedSetRemoveAsync(ScheduleKey, json);
    }
}