using HiveHub.Application.MediatR.SpyGame.Commands.HandleTimeout;
using HiveHub.Application.MediatR.SpyGame.Commands.HandleTimeUp;
using HiveHub.Application.Models;
using HiveHub.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace HiveHub.Infrastructure.BackgroundJobs;

public class InMemoryTaskWorker(
    IServiceProvider serviceProvider,
    ILogger<InMemoryTaskWorker> logger) : BackgroundService
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
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RAM worker");
            }

            await Task.Delay(500, stoppingToken);
        }
    }

    private async Task ProcessTaskAsync(ScheduledTask task)
    {
        using var scope = serviceProvider.CreateScope();
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
            logger.LogError(ex, "Failed to execute command for task {Type} in room {Room}", task.Type, task.RoomCode);
        }
    }
}