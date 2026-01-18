using HiveHub.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HiveHub.Infrastructure.BackgroundJobs;

public class InMemoryTaskWorker(
    IServiceProvider serviceProvider,
    ILogger<InMemoryTaskWorker> logger) : BaseTaskWorker(logger, serviceProvider)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                var overdueTasks = InMemoryTaskScheduler.Tasks
                    .Where(x => x.Value.ExecuteAt <= now)
                    .ToList();

                foreach (var item in overdueTasks)
                {
                    if (InMemoryTaskScheduler.Tasks.TryRemove(item.Key, out _))
                    {
                        var task = item.Value.Task;
                        await ProcessTaskAsync(task);
                    }
                }

                await Task.Delay(500, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown, do nothing or log info
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RAM worker");

                // Add a small delay safely to prevent tight loop in case of persistent error
                try { await Task.Delay(1000, stoppingToken); } catch { }
            }
        }
    }
}