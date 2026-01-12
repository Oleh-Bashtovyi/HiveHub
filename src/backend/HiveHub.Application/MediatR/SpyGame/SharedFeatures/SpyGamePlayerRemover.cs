using HiveHub.Application.Constants;
using HiveHub.Domain;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public record PlayerRemovalResult(
    string RemovedPlayerId,
    string? NewHostId,
    bool ShouldDeleteRoom
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
            return new PlayerRemovalResult(playerId, null, true);
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

        return new PlayerRemovalResult(playerId, newHostId, false);
    }
}
