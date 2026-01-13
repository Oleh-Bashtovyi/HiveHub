using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public record PlayerRemovalResult(
    string RemovedPlayerId,
    string? NewHostId,
    bool ShouldDeleteRoom,
    string RoomCode
);

public static class SpyGamePlayerRemover
{
    public static PlayerRemovalResult Remove(SpyRoom room, string playerId)
    {
        var player = room.Players.FirstOrDefault(p => p.IdInRoom == playerId);

        if (player == null)
        {
            throw new Exception(ProjectMessages.PlayerNotFound);
        }

        bool wasHost = player.IsHost;

        room.Players.Remove(player);

        if (room.Players.Count == 0)
        {
            return new PlayerRemovalResult(
                RemovedPlayerId: playerId,
                RoomCode:room.RoomCode,
                NewHostId: null,
                ShouldDeleteRoom: true);
        }

        string? newHostId = null;
        if (wasHost)
        {
            // First connected or just first othervive
            var newHost = room.Players.FirstOrDefault(x => x.IsConnected)
                          ?? room.Players.FirstOrDefault();

            if (newHost != null)
            {
                newHost.IsHost = true;
                newHostId = newHost.IdInRoom;
            }
        }

        return new PlayerRemovalResult(
            RemovedPlayerId: playerId, 
            NewHostId: newHostId,
            RoomCode: room.RoomCode,
            ShouldDeleteRoom: false);
    }

    public static async Task PublishSideEffectAfterRemove<TLogger>(
        PlayerRemovalResult removalResult, 
        ISpyGamePublisher publisher,
        ISpyGameRepository repository,
        TLogger logger) where TLogger : ILogger
    {
        if (removalResult.ShouldDeleteRoom)
        {
            await repository.RemoveRoomAsync(removalResult.RoomCode);
            logger.LogInformation("Room {RoomCode} deleted - no players left", removalResult.RoomCode);
        }
        else
        {
            if (!string.IsNullOrEmpty(removalResult.RemovedPlayerId))
            {
                var leftEvent = new PlayerLeftEventDto(removalResult.RoomCode, removalResult.RemovedPlayerId);
                await publisher.PublishPlayerLeftAsync(leftEvent);
                logger.LogDebug("Player {PlayerId} left room {RoomId}", removalResult.RemovedPlayerId, removalResult.RoomCode);
            }

            if (!string.IsNullOrEmpty(removalResult.NewHostId))
            {
                var hostChangedEvent = new HostChangedEventDto(removalResult.RoomCode, removalResult.NewHostId);
                await publisher.PublishHostChangedAsync(hostChangedEvent);
                logger.LogDebug("New host {HostId} assigned in room {RoomCode}", removalResult.NewHostId, removalResult.RoomCode);
            }
        }
    }
}
