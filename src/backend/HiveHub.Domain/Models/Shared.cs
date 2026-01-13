using System.Diagnostics.CodeAnalysis;

namespace HiveHub.Domain.Models;

public enum RoomStatus
{
    Lobby,
    InGame,
    Ended
}

public enum TargetVoteType
{
    Yes,
    No,
    Skip
}

public abstract class RoomBase<TGameSettings, TPlayer, TPlayerState>(string code)
    where TPlayer : PlayerBase<TPlayerState>
{
    public string RoomCode { get; } = code;
    public List<TPlayer> Players { get; } = new();
    public RoomStatus Status { get; set; } = RoomStatus.Lobby;
    public long StateVersion { get; private set; } = 0;
    public DateTime StateVersionChangedAt { get; private set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public TGameSettings GameSettings { get; init; } = default!;


    public void IncrementVersion()
    {
        StateVersion++;
        StateVersionChangedAt = DateTime.UtcNow;
    }

    public bool TryGetPlayerByConnectionId(string connectionId, [NotNullWhen(true)] out TPlayer? player)
    {
        player = Players.FirstOrDefault(x => x.ConnectionId == connectionId);
        return player != null;
    }

    public bool TryGetPlayerByIdInRoom(string idInRoom, [NotNullWhen(true)] out TPlayer? player)
    {
        player = Players.FirstOrDefault(x => x.IdInRoom == idInRoom);
        return player != null;
    }

    public bool IsInLobby() => Status == RoomStatus.Lobby;
    public bool IsInGame() => Status == RoomStatus.InGame;
}

public class ChatMessage(string playerId, string playerName, string message, DateTime timestamp)
{
    public string PlayerId { get; } = playerId;
    public string PlayerName { get; } = playerName;
    public string Message { get; } = message;
    public DateTime Timestamp { get; } = timestamp;
}

public abstract class PlayerBase<TPlayerState>
{
    public string ConnectionId { get; set; } = string.Empty;
    public string IdInRoom { get; init; } = string.Empty;
    public bool IsConnected { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public string AvatarId { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public bool IsReady { get; set; } = false;

    public TPlayerState PlayerState { get; init; } = default!;
}

public sealed class TimerState
{
    public DateTime? GameStartTime { get; set; }
    public DateTime? PlannedGameEndTime { get; set; }
    public DateTime? TimerStoppedAt { get; set; }
    public bool IsTimerStopped { get; set; } = true;

    public double GetRemainingSeconds()
    {
        if (IsTimerStopped)
        {
            return 0;
        }

        if (!PlannedGameEndTime.HasValue) return 0;

        var remaining = PlannedGameEndTime.Value - DateTime.UtcNow;
        return remaining.TotalSeconds > 0 ? remaining.TotalSeconds : 0;
    }
}

public abstract class VotingStateBase
{
    public DateTime VotingStartedAt { get; set; }
    public DateTime VotingEndsAt { get; set; }
}

public class AccusationVotingState : VotingStateBase
{
    public required string InitiatorId { get; init; }
    public required string TargetId { get; init; }
    public Dictionary<string, TargetVoteType> Votes { get; set; } = new();

    public bool TryVote(string voterPlayerId, TargetVoteType voteType)
    {
        return Votes.TryAdd(voterPlayerId, voteType);
    }
}

public sealed class GeneralVotingState : VotingStateBase
{
    public Dictionary<string, string?> Votes { get; set; } = new();

    public bool TryVote(string voterPlayerId, string targetPlayerId)
    {
        return Votes.TryAdd(voterPlayerId, targetPlayerId);
    }
}
