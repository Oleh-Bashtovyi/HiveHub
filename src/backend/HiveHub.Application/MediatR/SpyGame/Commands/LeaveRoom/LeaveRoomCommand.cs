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
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
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

        var result = await roomAccessor.ExecuteAsync(async (room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            removalResult = SpyGamePlayerRemover.Remove(room, player.IdInRoom);

            //
            //
            //
            // TODO: Dont publish inside of ExecuteAsync, store all events in list and publish outside of logic block
            //
            //
            //
            if (!removalResult.ShouldDeleteRoom)
            {
                await SpyGameLogicHelper.CheckAndResolveVoting(room, publisher, scheduler, repository, logger);
                await SpyGameLogicHelper.CheckAndResolveTimerStop(room, publisher, scheduler, logger);
            }

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

       logger.LogInformation("Player {PlayerId} left room {RoomCode}", removalResult.RemovedPlayerId, request.RoomCode);

        await publisher.RemovePlayerFromRoomGroupAsync(request.ConnectionId, request.RoomCode);

        await SpyGamePlayerRemover.PublishSideEffectAfterRemove(removalResult, publisher, repository, logger);

        return Result.Ok();
    }
}