using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.KickPlayer;

public record KickPlayerCommand(
    string RoomCode,
    string HostConnectionId,
    string TargetPlayerId
) : IRequest<Result>;

public class KickPlayerHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ILogger<KickPlayerHandler> logger)
    : IRequestHandler<KickPlayerCommand, Result>
{
    public async Task<Result> Handle(KickPlayerCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = repository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string kickedPlayerConnectionId = string.Empty;
        string kickedPlayerName = string.Empty;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed(ProjectMessages.Kick.CanNotKickPlayersMidGame);
            }

            if (!room.TryGetPlayerByConnectionId(request.HostConnectionId, out var host) || !host.IsHost)
            {
                return Results.ActionFailed(ProjectMessages.Kick.OnlyHostCanKickPlayers);
            }

            var target = room.Players.FirstOrDefault(p => p.IdInRoom == request.TargetPlayerId);
            
            if (target == null)
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            if (target.IsHost)
            {
                return Results.ActionFailed(ProjectMessages.Kick.HostCanNotKickItself);
            }

            kickedPlayerConnectionId = target.ConnectionId;
            kickedPlayerName = target.Name;

            room.Players.Remove(target);

            return Result.Ok();
        });

        if (result.IsFailed) return result;

        logger.LogInformation("Player {PlayerId} kicked from {RoomCode}", request.TargetPlayerId, request.RoomCode);

        var kickEvent = new PlayerKickedEventDto(request.RoomCode, request.TargetPlayerId, "HOST");
        await publisher.PublishPlayerKickedAsync(kickEvent);

        if (!string.IsNullOrEmpty(kickedPlayerConnectionId))
        {
            await publisher.RemovePlayerFromRoomGroupAsync(kickedPlayerConnectionId, request.RoomCode);
        }

        return Result.Ok();
    }
}