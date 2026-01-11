using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Mappers;
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
) : IRequest<Result<RoomStateDto>>;

public class ReconnectHandler(
    ISpyGameRepository gameManager,
    IConnectionMappingService mappingService,
    ISpyGamePublisher publisher,
    ILogger<ReconnectHandler> logger)
    : IRequestHandler<ReconnectCommand, Result<RoomStateDto>>
{
    public async Task<Result<RoomStateDto>> Handle(ReconnectCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Room not found.");
        }

        string? oldConnectionId = null;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            var player = room.Players.FirstOrDefault(x => x.IdInRoom == request.OldPlayerId);
            if (player == null)
            {
                return Results.NotFound<RoomStateDto>("Player not found.");
            }

            oldConnectionId = player.ConnectionId;
            player.ConnectionId = request.NewConnectionId;
            player.IsConnected = true;

            var state = SpyGameStateMapper.GetRoomStateForPlayer(room, player.IdInRoom);

            return state;
        });

        if (result.IsFailed) return result;

        if (!string.IsNullOrEmpty(oldConnectionId))
        {
            mappingService.Unmap(oldConnectionId);
            await publisher.RemovePlayerFromRoomGroupAsync(oldConnectionId, request.RoomCode);
        }

        mappingService.Map(request.NewConnectionId, request.RoomCode);
        await publisher.AddPlayerToRoomGroupAsync(request.NewConnectionId, request.RoomCode);

        logger.LogInformation("Reconnect: {Old} -> {New} in {Room}",
            oldConnectionId, request.NewConnectionId, request.RoomCode);

        var connectionEvent = new PlayerConnectionChangedEventDto(request.RoomCode, request.OldPlayerId, true);
        await publisher.PublishPlayerConnectionChangedAsync(connectionEvent);

        return result;
    }
}