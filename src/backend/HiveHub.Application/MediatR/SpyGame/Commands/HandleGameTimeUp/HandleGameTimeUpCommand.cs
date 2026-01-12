using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.HandleTimeUp;

public record HandleGameTimeUpCommand(string RoomCode) : IRequest<Result>;

public class HandleGameTimeUpHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ILogger<HandleGameTimeUpHandler> logger)
    : IRequestHandler<HandleGameTimeUpCommand, Result>
{
    public async Task<Result> Handle(HandleGameTimeUpCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = repository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Result.Ok();
        }

        bool timeIsUp = false;

        await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State == RoomState.InGame && !room.TimerState.IsTimerStopped)
            {
                // Check if the time has actually expired (in case of lags or outdated scheduled tasks)
                // Add a small buffer (2 seconds) to avoid race conditions
                if (room.TimerState.PlannedGameEndTime <= DateTime.UtcNow.AddSeconds(2))
                {
                    room.TimerState.IsTimerStopped = true;
                    timeIsUp = true;
                }
            }
            return Result.Ok();
        });

        if (timeIsUp)
        {
            logger.LogInformation("Game time up in room {RoomCode}", request.RoomCode);
            await publisher.PublishGameEndedAsync(new GameEndedEventDto(request.RoomCode));
        }

        return Result.Ok();
    }
}