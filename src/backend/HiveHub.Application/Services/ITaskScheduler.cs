using HiveHub.Application.Models;

namespace HiveHub.Application.Services;

public interface ITaskScheduler
{
    Task ScheduleAsync(ScheduledTask task, TimeSpan delay);
    Task CancelAsync(ScheduledTask task);
}
