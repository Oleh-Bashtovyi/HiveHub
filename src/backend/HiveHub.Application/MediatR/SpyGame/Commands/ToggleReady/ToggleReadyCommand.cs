using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Extensions;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.ToggleReady;

public record ToggleReadyCommand(
    string RoomCode,
    string ConnectionId
) : IRequest<Result>;

public class ToggleReadyHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<ToggleReadyHandler> logger)
    : IRequestHandler<ToggleReadyCommand, Result>
{
    public async Task<Result> Handle(ToggleReadyCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var playerId = string.Empty;
        var newReadyStatus = false;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.IsInLobby())
            {
                return Results.ActionFailed(ProjectMessages.ToggleReady.CanNotReadyStatusMidGame);
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            player.IsReady = !player.IsReady;
            playerId = player.IdInRoom;
            newReadyStatus = player.IsReady;

            context.AddEvent(new PlayerReadyStatusChangedEventDto(request.RoomCode, playerId, newReadyStatus));

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            logger.LogInformation("Room [{RoomCode}]: Player {PlayerId} changed ready status to {IsReady}",
                 request.RoomCode, 
                 playerId, 
                 newReadyStatus);
        }

        return result;
    }
}