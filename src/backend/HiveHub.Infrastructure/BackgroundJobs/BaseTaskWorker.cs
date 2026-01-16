using HiveHub.Application.MediatR.SpyGame.Handlers.HandleVotingTimeUp;
using HiveHub.Application.MediatR.SpyGame.SystemCommands.HandleGameTimeUp;
using HiveHub.Application.MediatR.SpyGame.SystemCommands.HandlePlayerTimeout;
using HiveHub.Application.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HiveHub.Infrastructure.BackgroundJobs;

public abstract class BaseTaskWorker : BackgroundService
{
    protected readonly ILogger _logger;
    protected readonly IServiceProvider _serviceProvider;

    protected BaseTaskWorker(
        ILogger logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected async Task ProcessTaskAsync(ScheduledTask task)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            switch (task.Type)
            {
                case TaskType.SpyGamePlayerDisconnectedTimeout:
                    if (task.TargetId != null)
                        await mediator.Send(new ProcessInactivePlayerCommand(task.RoomCode, task.TargetId));
                    break;

                case TaskType.SpyGameRoundTimeUp:
                    await mediator.Send(new ProcessRoundTimeUpCommand(task.RoomCode));
                    break;

                case TaskType.SpyGameVotingTimeUp:
                    await mediator.Send(new HandleVotingTimeUpCommand(task.RoomCode));
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command for task {Type} in room {Room}", task.Type, task.RoomCode);
        }
    }
}
