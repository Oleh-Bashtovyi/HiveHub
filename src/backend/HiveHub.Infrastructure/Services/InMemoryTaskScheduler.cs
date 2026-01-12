using HiveHub.Application.Models;
using HiveHub.Application.Services;
using System.Collections.Concurrent;

namespace HiveHub.Infrastructure.Services;

public class InMemoryTaskScheduler : ITaskScheduler
{
    public static readonly ConcurrentDictionary<string, (ScheduledTask Task, DateTime ExecuteAt)> Tasks = new();

    public Task ScheduleAsync(ScheduledTask task, TimeSpan delay)
    {
        var key = GetKey(task);
        var executeAt = DateTime.UtcNow.Add(delay);

        Tasks.AddOrUpdate(key, (task, executeAt), (k, v) => (task, executeAt));

        return Task.CompletedTask;
    }

    public Task CancelAsync(ScheduledTask task)
    {
        var key = GetKey(task);
        Tasks.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private static string GetKey(ScheduledTask task)
    {
        return $"{task.Type}:{task.RoomCode}:{task.TargetId ?? "Global"}";
    }
}