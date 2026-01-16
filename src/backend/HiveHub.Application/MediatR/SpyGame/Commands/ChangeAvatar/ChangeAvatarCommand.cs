using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Extensions;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.ChangeAvatar;

public record ChangeAvatarCommand(
    string RoomCode,
    string ConnectionId,
    string NewAvatarId
) : IRequest<Result>;

public class ChangeAvatarHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<ChangeAvatarHandler> logger)
    : IRequestHandler<ChangeAvatarCommand, Result>
{
    public async Task<Result> Handle(ChangeAvatarCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var playerId = string.Empty;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.IsInLobby())
            {
                return Results.ActionFailed(ProjectMessages.ChangeAvatar.CanNotChangeAvatarMidGame);
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            if (string.IsNullOrWhiteSpace(request.NewAvatarId))
            {
                return Results.ValidationFailed(ProjectMessages.ChangeAvatar.AvatarHasBadFormat);
            }

            player.AvatarId = request.NewAvatarId;

            context.AddEvent(new PlayerChangedAvatarEventDto(request.RoomCode, player.IdInRoom, request.NewAvatarId));

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: Player {PlayerId} changed avatar to {AvatarId}",
                request.RoomCode,
                playerId,
                request.NewAvatarId);
        }

        return result;
    }
}