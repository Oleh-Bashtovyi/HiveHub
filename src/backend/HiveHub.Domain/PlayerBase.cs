using System.Collections.Concurrent;

namespace HiveHub.Domain;

public abstract class PlayerBase<TPlayerState>
{
    public string ConnectionId { get; init; } = string.Empty;
    public string IdInRoom { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AvatarId { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public bool IsReady { get; set; } = false;
    
    public TPlayerState PlayerState { get; init; } = default!;
}

public abstract class RoomBase<TGameSettings, TPlayer, TPlayerState>(string code)
    where TPlayer : PlayerBase<TPlayerState>
{
    public string RoomCode { get; } = code;
    public RoomState State { get; set; } = RoomState.Lobby;
    public ConcurrentDictionary<string, TPlayer> Players { get; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TGameSettings GameSettings { get; init; } = default!;
}

public enum RoomState
{
    Lobby,
    InGame,
    Ended
}

public sealed class SpyPlayerState
{
    public bool IsSpy { get; set; } = false;
    public bool VotedToStopTimer { get; set; } = false;
}

public sealed class SpyPlayer : PlayerBase<SpyPlayerState>
{
    public SpyPlayer(string connectionId, string idInRoom)
    {
        ConnectionId = connectionId;
        IdInRoom = idInRoom;
        PlayerState = new SpyPlayerState();
    }
}

public class SpyRoomSettings
{
    public int TimerMinutes { get; set; } = 5;
    public int SpiesCount { get; set; } = 1;
    public bool SpiesKnowEachOther { get; set; } = false;
    public bool ShowCategoryToSpy { get; set; } = false;
    public List<SpyGameWordsCategory> Categories { get; set; } = new();
}

public sealed class SpyRoom : RoomBase<SpyRoomSettings, SpyPlayer, SpyPlayerState>
{
    public string? CurrentSecretWord { get; set; }
    public DateTime? GameStartTime { get; set; }
    public bool IsTimerStopped { get; set; } = false;
    public DateTime? TimerStoppedAt { get; set; }
    public List<ChatMessage> ChatMessages { get; set; } = new();

    public SpyRoom(string code) : base(code)
    {
        GameSettings = new SpyRoomSettings();
    }
}

public class SpyGameWordsCategory {
    public string Name { get; set; }
    public List<string> Words { get; set; } = new();
}

public class ChatMessage(string playerId, string playerName, string message, DateTime timestamp)
{
    public string PlayerId { get; } = playerId;
    public string PlayerName { get; } = playerName;
    public string Message { get; } = message;
    public DateTime Timestamp { get; } = timestamp;
}
