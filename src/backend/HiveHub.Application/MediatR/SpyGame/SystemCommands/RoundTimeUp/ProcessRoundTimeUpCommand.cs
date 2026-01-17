using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.SystemCommands.HandleRoundTimeUp;

public record ProcessRoundTimeUpCommand(string RoomCode) : IRequest<Result>;

public class ProcessRoundTimeUpHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<ProcessRoundTimeUpHandler> logger)
    : IRequestHandler<ProcessRoundTimeUpCommand, Result>
{
    public async Task<Result> Handle(ProcessRoundTimeUpCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.IsInGame() || room.GameState.RoundTimerState.IsStopped)
            {
                return Result.Ok();
            }

            if (room.GameState.RoundTimerState.TimerWillStopAt > DateTime.UtcNow.AddSeconds(2))
            {
                return Result.Ok();
            }

            logger.LogInformation("Room [{RoomCode}]: Round timer expired", request.RoomCode);

            RoundTimer.HandleRoundTimeUp(room, context);

            return Result.Ok();
        });

        return Result.Ok();
    }
}