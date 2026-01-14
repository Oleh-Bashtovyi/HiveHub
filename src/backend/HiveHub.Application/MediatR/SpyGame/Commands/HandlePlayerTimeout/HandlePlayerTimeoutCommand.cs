using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.HandleTimeout;

public record HandlePlayerTimeoutCommand(
    string RoomCode,
    string PlayerId
) : IRequest<Result>;

public class HandlePlayerTimeoutHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
    ILogger<HandlePlayerTimeoutHandler> logger)
    : IRequestHandler<HandlePlayerTimeoutCommand, Result>
{
    public async Task<Result> Handle(HandlePlayerTimeoutCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        PlayerRemovalResult removalResult = null!;

        await roomAccessor.ExecuteAsync(async (room) =>
        {
            var player = room.Players.FirstOrDefault(p => p.IdInRoom == request.PlayerId);

            if (player == null || player.IsConnected) return;

            removalResult = SpyGamePlayerRemover.Remove(room, request.PlayerId);

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
        });

        if (removalResult == null)
        {
            return Result.Ok();
        }

        logger.LogInformation("Timeout: Player {PlayerId} removed from room {RoomCode}", request.PlayerId, request.RoomCode);

        await SpyGamePlayerRemover.PublishSideEffectAfterRemove(removalResult, publisher, repository, logger);

        return Result.Ok();
    }
}