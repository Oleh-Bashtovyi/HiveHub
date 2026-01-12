using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.HandleDisconnect;

public record HandleDisconnectCommand(string ConnectionId) : IRequest<Result>;

public class HandleDisconnectHandler(
    ISpyGameRepository gameManager,
    IConnectionMappingService mappingService,
    ISpyGamePublisher publisher,
    ILogger<HandleDisconnectHandler> logger)
    : IRequestHandler<HandleDisconnectCommand, Result>
{
    public async Task<Result> Handle(HandleDisconnectCommand request, CancellationToken cancellationToken)
    {
        var roomCode = mappingService.GetRoomCode(request.ConnectionId);

        if (string.IsNullOrEmpty(roomCode))
        {
            return Result.Ok();
        }

        mappingService.Unmap(request.ConnectionId);

        var roomAccessor = gameManager.GetRoom(roomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string? playerId = null;
        bool wasConnected = false;

        await roomAccessor.ExecuteAsync(async (room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return;
            }

            playerId = player.IdInRoom;
            wasConnected = player.IsConnected;
            player.IsConnected = false;

            logger.LogInformation("Player {PlayerId} disconnected from room {RoomCode}", player.IdInRoom, roomCode);

            if (player.IsHost)
            {
                var newHost = room.Players.FirstOrDefault(p => p.ConnectionId != request.ConnectionId && p.IsConnected);

                if (newHost == null && !(room.Players.Count == 0))
                {
                    newHost = room.Players.FirstOrDefault(p => p.ConnectionId != request.ConnectionId);
                }

                if (newHost != null)
                {
                    player.IsHost = false;
                    newHost.IsHost = true;

                    var hostDto = new HostChangedEventDto(roomCode, newHost.IdInRoom);
                    await publisher.PublishHostChangedAsync(hostDto);

                    logger.LogInformation("Host auto-assigned to {NewHostId} due to disconnect", newHost.IdInRoom);
                }
            }
        });

        if (!string.IsNullOrEmpty(playerId))
        {
            if (wasConnected)
            {
                var eventDto = new PlayerConnectionChangedEventDto(roomCode, playerId, false);
                await publisher.PublishPlayerConnectionChangedAsync(eventDto);
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30));

                var accessor = gameManager.GetRoom(roomCode);
                if (accessor == null) return;

                PlayerRemovalResult removalResult = null!;

                await accessor.ExecuteAsync((room) =>
                {
                    var playerToDelete = room.Players.FirstOrDefault(p => p.IdInRoom == playerId);

                    if (playerToDelete == null || playerToDelete.IsConnected) return;

                    removalResult = SpyGamePlayerRemover.Remove(room, playerId);

                    logger.LogInformation("Player {PlayerId} removed from room {RoomCode} due to prolonged disconnect",
                        removalResult.RemovedPlayerId, roomCode);
                });

                if (removalResult.ShouldDeleteRoom)
                {
                    await gameManager.RemoveRoomAsync(roomCode);
                    logger.LogInformation("Room {RoomCode} deleted - no players left", roomCode);
                }
                else if (!string.IsNullOrEmpty(removalResult.RemovedPlayerId))
                {
                    var leftEvent = new PlayerLeftEventDto(roomCode, removalResult.RemovedPlayerId);
                    await publisher.PublishPlayerLeftAsync(leftEvent);

                    if (!string.IsNullOrEmpty(removalResult.NewHostId))
                    {
                        var hostChangedEvent = new HostChangedEventDto(roomCode, removalResult.NewHostId);
                        await publisher.PublishHostChangedAsync(hostChangedEvent);
                    }
                }
            });
        }

        return Result.Ok();
    }
}