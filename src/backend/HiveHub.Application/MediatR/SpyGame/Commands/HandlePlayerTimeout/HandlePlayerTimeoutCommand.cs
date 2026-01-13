using FluentResults;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
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
    ILogger<HandlePlayerTimeoutHandler> logger)
    : IRequestHandler<HandlePlayerTimeoutCommand, Result>
{
    public async Task<Result> Handle(HandlePlayerTimeoutCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = repository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Result.Ok();
        }

        PlayerRemovalResult removalResult = null!;

        await roomAccessor.ExecuteAsync((room) =>
        {
            var player = room.Players.FirstOrDefault(p => p.IdInRoom == request.PlayerId);

            if (player == null || player.IsConnected) return;

            removalResult = SpyGamePlayerRemover.Remove(room, request.PlayerId);
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