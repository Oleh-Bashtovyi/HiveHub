using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Publishers;
using HiveHub.Domain.Models;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public record PlayerRemovalResult(
    string RemovedPlayerId,
    string? NewHostId,
    bool ShouldDeleteRoom,
    string RoomCode
);

public static class SpyGamePlayerRemover
{
    public static PlayerRemovalResult Remove(SpyRoom room, SpyGameEventsContext context, string playerId)
    {
        var player = room.Players.FirstOrDefault(p => p.IdInRoom == playerId);

        if (player == null)
        {
            throw new Exception(ProjectMessages.PlayerNotFound);
        }

        bool wasHost = player.IsHost;

        room.Players.Remove(player);

        context.AddEvent(new PlayerLeftEventDto(room.RoomCode, player.IdInRoom));
        context.AddEvent(new RemovePlayerFromGroupEvent(player.ConnectionId, room.RoomCode));

        if (room.Players.Count == 0)
        {
            room.MarkAsDeleted();
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
                context.AddEvent(new HostChangedEventDto(room.RoomCode, newHost.IdInRoom));
            }
        }

        return new PlayerRemovalResult(
            RemovedPlayerId: playerId, 
            NewHostId: newHostId,
            RoomCode: room.RoomCode,
            ShouldDeleteRoom: false);
    }
}
