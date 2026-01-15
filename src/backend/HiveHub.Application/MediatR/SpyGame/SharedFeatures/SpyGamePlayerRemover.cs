using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Publishers;
using HiveHub.Domain.Models.SpyGame;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public record PlayerRemovalResult(
    string RemovedPlayerId,
    string? NewHostId,
    bool ShouldDeleteRoom,
    string RoomCode
);

// Remove player 
// If (other players) -> publish player left
// If (player was host) -> change host -> publishhost changed
// If (removed player been voted) -> {
//     If (removed player was accused) -> publish voting unsuccessful (accuser left) -> continuing game 
//     If (removed player final guessing) -> treat as thier fail -> (finish game if spies in group) or (continue if not in group)
//
// If leave mid game and was spy -> do nothing (dot finish game)

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
