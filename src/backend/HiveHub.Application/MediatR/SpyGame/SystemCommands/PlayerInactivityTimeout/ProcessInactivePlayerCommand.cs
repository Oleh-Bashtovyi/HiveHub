using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.SystemCommands.HandlePlayerTimeout;

public record ProcessInactivePlayerCommand(
    string RoomCode,
    string PlayerId
) : IRequest<Result>;

public class InactivePlayerHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<InactivePlayerHandler> logger)
    : IRequestHandler<ProcessInactivePlayerCommand, Result>
{
    public async Task<Result> Handle(ProcessInactivePlayerCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        PlayerRemovalResult? removalResult = null;

        await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.TryGetPlayerByIdInRoom(request.PlayerId, out var player) || player.IsConnected)
            {
                return Result.Ok();
            }

            removalResult = SpyGamePlayerRemover.Remove(room, context, request.PlayerId, repository, logger);

            return Result.Ok();
        });

        if (removalResult == null)
        {
            return Result.Ok();
        }

        logger.LogInformation("Timeout: Player {PlayerId} removed from room {RoomCode}", request.PlayerId, request.RoomCode);

        if (removalResult != null && removalResult.ShouldDeleteRoom)
        {
            await repository.RemoveRoomAsync(request.RoomCode);
        }

        return Result.Ok();
    }
}