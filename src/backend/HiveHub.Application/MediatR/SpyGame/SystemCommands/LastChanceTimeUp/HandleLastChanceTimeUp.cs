using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.SystemCommands.HandleLastChanceTimeUp;

public record HandleLastChanceTimeUpCommand(
    string RoomCode,
    string SpyId
) : IRequest<Result>;

public class HandleLastChanceTimeUpHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<HandleLastChanceTimeUpHandler> logger)
    : IRequestHandler<HandleLastChanceTimeUpCommand, Result>
{
    public async Task<Result> Handle(HandleLastChanceTimeUpCommand request, CancellationToken token)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.IsInGame() || room.GameState.CurrentPhase != Domain.Models.SpyGame.SpyGamePhase.SpyLastChance)
            {
                return Result.Ok();
            }

            if (room.GameState.CaughtSpyId != request.SpyId)
            {
                return Result.Ok();
            }

            if (room.GameState.SpyLastChanceEndsAt > DateTime.UtcNow.AddSeconds(2))
            {
                return Result.Ok();
            }

            logger.LogInformation("Room [{RoomCode}]: Spy {SpyId} last chance expired",
                request.RoomCode,
                request.SpyId);

            RoundTimer.HandleLastChanceTimeUp(room, context, request.SpyId);

            return Result.Ok();
        });

        return Result.Ok();
    }
}