using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.Reconnect;

public record ReconnectCommand(
    string RoomCode,
    string OldPlayerId,
    string NewConnectionId
) : IRequest<Result<SpyRoomStateDto>>;

public class ReconnectHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<ReconnectHandler> logger)
    : IRequestHandler<ReconnectCommand, Result<SpyRoomStateDto>>
{
    public async Task<Result<SpyRoomStateDto>> Handle(ReconnectCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var playerId = string.Empty;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.TryGetPlayerByIdInRoom(request.OldPlayerId, out var player))
            {
                return Results.NotFound<SpyRoomStateDto>(ProjectMessages.PlayerNotFound);
            }

            context.AddEvent(new CancelTaskEvent(TaskType.SpyGamePlayerDisconnectedTimeout, request.RoomCode, request.OldPlayerId));

            var oldConnectionId = player.ConnectionId;
            playerId = player.IdInRoom;
            player.ConnectionId = request.NewConnectionId;
            player.IsConnected = true;

            SpyGameLogicHelper.CheckAndResolveTimerStop(room, context, logger);

            context.AddEvent(new RemovePlayerFromGroupEvent(oldConnectionId, request.RoomCode));
            context.AddEvent(new AddPlayerToGroupEvent(request.NewConnectionId, request.RoomCode));
            context.AddEvent(new PlayerConnectionChangedEventDto(request.RoomCode, request.OldPlayerId, true));

            var state = SpyGameStateMapper.GetRoomStateForPlayer(room, player.IdInRoom);

            return state;
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: player {PlayerId} was reconnected",
                request.RoomCode, 
                playerId);
        }

        return result;
    }
}