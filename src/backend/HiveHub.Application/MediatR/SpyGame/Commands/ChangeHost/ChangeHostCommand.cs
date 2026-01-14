using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Extensions;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.ChangeHost;

public record ChangeHostCommand(
    string RoomCode,
    string CurrentHostConnectionId,
    string NewHostPlayerId
) : IRequest<Result>;

public class ChangeHostHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<ChangeHostHandler> logger)
    : IRequestHandler<ChangeHostCommand, Result>
{
    public async Task<Result> Handle(ChangeHostCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var newHostId = string.Empty;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.IsInLobby())
            {
                return Results.ActionFailed(ProjectMessages.ChangeHost.CanNotChangeHostMidGame);
            }

            if (!room.TryGetPlayerByConnectionId(request.CurrentHostConnectionId, out var currentHost) || !currentHost.IsHost)
            {
                return Results.Forbidden(ProjectMessages.ChangeHost.OnlyHostCanChangePermission);
            }

            if (!room.TryGetPlayerByIdInRoom(request.NewHostPlayerId, out var newHost))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            currentHost.IsHost = false;
            newHost.IsHost = true;
            newHostId = newHost.IdInRoom;

            context.AddEvent(new HostChangedEventDto(room.RoomCode, newHostId));

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Host changed to {NewHostId} in room {RoomCode}", newHostId, request.RoomCode);
        }

        return result;
    }
}