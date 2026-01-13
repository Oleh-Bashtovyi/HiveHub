namespace HiveHub.Domain.Models;

public enum SpyGamePhase
{
    None,
    Search,
    Accusation,
    FinalVote,
    SpyLastChance
}

public enum GameEndReason
{
    TimerExpired,
    // Wrong accuse or final vote target
    CivilianKicked,
    // Right guess during game or last chance of spy
    SpyGuessedWord,
    // Wrong guess during game
    SpyWrongGuess,
    // Players could not reach an agreement in final vote
    FinalVotingFailed,
    // Wrong guess during last chance of spy
    SpyFound
}

public enum Team
{
    Civilians,
    Spies
}

public sealed class SpyRoom : RoomBase<SpyRoomSettings, SpyPlayer, SpyPlayerState>
{
    public string? CurrentSecretWord { get; set; }
    public string? CurrentCategory { get; set; }
    public List<ChatMessage> ChatMessages { get; set; } = new();
    public TimerState TimerState { get; init; } = default!;
    public SpyGamePhase CurrentPhase { get; set; } = SpyGamePhase.None;
    public VotingStateBase? ActiveVoting { get; set; }
    // Only caught spy can guess word during last chance phase
    public string? CaughtSpyId { get; set; }
    public GameEndReason? GameEndReason { get; set; }
    public Team? WinnerTeam { get; set; }

    public SpyRoom(string code) : base(code)
    {
        GameSettings = new SpyRoomSettings();
        TimerState = new TimerState();
    }
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
    public bool HasUsedAccusation { get; set; } = false;
}

public sealed class SpyRoomSettings
{
    public int TimerMinutes { get; set; } = 5;
    public int MinSpiesCount { get; set; } = 1;
    public int MaxSpiesCount { get; set; } = 1;
    public bool SpiesKnowEachOther { get; set; } = false;
    public bool ShowCategoryToSpy { get; set; } = false;
    public List<SpyGameWordsCategory> CustomCategories { get; set; } = new();
}

public sealed class SpyGameWordsCategory
{
    public string Name { get; set; } = string.Empty;
    public List<string> Words { get; set; } = new();
}
