using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.LeaveRoom;

public record LeaveRoomCommand(
    string RoomCode,
    string ConnectionId
) : IRequest<Result>;

public class LeaveRoomHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<LeaveRoomHandler> logger)
    : IRequestHandler<LeaveRoomCommand, Result>
{
    public async Task<Result> Handle(LeaveRoomCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        PlayerRemovalResult removalResult = null!;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            removalResult = SpyGamePlayerRemover.Remove(room, context, player.IdInRoom, repository, logger);

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: Player {PlayerId} left room", 
                request.RoomCode, 
                removalResult.RemovedPlayerId);
        }

        if (removalResult != null && removalResult.ShouldDeleteRoom)
        {
            await repository.RemoveRoomAsync(request.RoomCode);
            logger.LogInformation("Room [{RoomCode}]: <DELETED>", 
                request.RoomCode);
        }

        return result;
    }
}