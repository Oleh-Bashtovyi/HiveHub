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
    ISpyGamePublisher publisher,
    ILogger<ChangeAvatarHandler> logger)
    : IRequestHandler<ChangeAvatarCommand, Result>
{
    public async Task<Result> Handle(ChangeAvatarCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string playerId = string.Empty;

        var result = await roomAccessor.ExecuteAsync((room) =>
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
            playerId = player.IdInRoom;

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        logger.LogInformation("Player {PlayerId} changed avatar to {AvatarId} in room {RoomCode}",
            playerId, request.NewAvatarId, request.RoomCode);

        var eventDto = new PlayerChangedAvatarEventDto(request.RoomCode, playerId, request.NewAvatarId);
        await publisher.PublishPlayerChangedAvatarAsync(eventDto);

        return Result.Ok();
    }
}