using HiveHub.Domain.Models.Shared;

namespace HiveHub.Domain.Models.SpyGame;

public sealed class SpyPlayer : PlayerBase<SpyPlayerState>
{
    public SpyPlayer(string connectionId, string idInRoom)
    {
        ConnectionId = connectionId;
        IdInRoom = idInRoom;
        PlayerState = new SpyPlayerState();
    }
}

public sealed class SpyPlayerState
{
    public bool IsSpy { get; set; } = false;
    public bool VotedToStopTimer { get; set; } = false;
    public bool HasUsedAccusation { get; set; } = false;
    // Wrong guess or final guess
    public bool IsDead { get; set; } = false;
}
