using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Extensions;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
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
    SpyGameEventsContext context,
    ILogger<KickPlayerHandler> logger)
    : IRequestHandler<KickPlayerCommand, Result>
{
    public async Task<Result> Handle(KickPlayerCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var kickedPlayerName = string.Empty;
        var kickedPlayerConnectionId = string.Empty;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.IsInLobby())
            {
                return Results.ActionFailed(ProjectMessages.Kick.CanNotKickPlayersMidGame);
            }

            if (!room.TryGetPlayerByConnectionId(request.HostConnectionId, out var host) || !host.IsHost)
            {
                return Results.Forbidden(ProjectMessages.Kick.OnlyHostCanKickPlayers);
            }

            if (!room.TryGetPlayerByIdInRoom(request.TargetPlayerId, out var target))
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

            context.AddEvent(new PlayerKickedEventDto(room.RoomCode, target.IdInRoom, "HOST"));
            context.AddEvent(new RemovePlayerFromGroupEvent(target.ConnectionId, room.RoomCode));

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Player {PlayerId} kicked from {RoomCode}", request.TargetPlayerId, request.RoomCode);
        }

        return result;
    }
}