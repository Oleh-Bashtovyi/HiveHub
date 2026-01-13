namespace HiveHub.Domain.Models;

public sealed class SpyRoom : RoomBase<SpyRoomSettings, SpyPlayer, SpyPlayerState>
{
    public string? CurrentSecretWord { get; set; }
    public string? CurrentCategory { get; set; }
    public List<ChatMessage> ChatMessages { get; set; } = new();
    public TimerState TimerState { get; init; } = default!;

    public SpyRoom(string code) : base(code)
    {
        GameSettings = new SpyRoomSettings();
        TimerState = new TimerState();
    }
}

public sealed class SpyRoomGameState
{
    public string? CurrentSecretWord { get; set; }
    public string? CurrentCategory { get; set; }
    public List<ChatMessage> ChatMessages { get; set; } = new();
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

public sealed class SpyPlayerState
{
    public bool IsSpy { get; set; } = false;
    public bool VotedToStopTimer { get; set; } = false;
}

public sealed class SpyRoomSettings
{
    public int TimerMinutes { get; set; } = 5;
    public int MinSpiesCount { get; set; } = 1;
    public int MaxSpiesCount { get; set; } = 1;
    public bool SpiesKnowEachOther { get; set; } = false;
    public bool ShowCategoryToSpy { get; set; } = false;
    public List<SpyGameWordsCategory> Categories { get; set; } = new();
}

public sealed class SpyGameWordsCategory
{
    public string Name { get; set; } = string.Empty;
    public List<string> Words { get; set; } = new();
}
