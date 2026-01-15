using HiveHub.Domain.Models.Shared;

namespace HiveHub.Domain.Models.SpyGame;

public enum SpyGamePhase
{
    None,
    // Default phase of gae=me, players saying questions and trying to figure out who is spy
    Search,
    // A player suspects someone of spying and initiates a vote against them.
    Accusation,
    // The round timer has ended, and the final vote for the spy begins.
    FinalVote,
    // When a spy is chosen during the accusation or final vote, they have one "last chance" to guess the secret word.
    SpyLastChance
}

public enum SpyGameEndReason
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

public enum SpyTeam
{
    Civilians,
    Spies
}

public sealed class SpyRoom : RoomBase<SpyRoomGameState, SpyRoomSettings, SpyPlayer, SpyPlayerState>
{

    public List<ChatMessage> ChatMessages { get; set; } = new();

    public SpyRoom(string code) : base(code)
    {
        GameSettings = new SpyRoomSettings();
        GameState = new SpyRoomGameState();
    }
}

public sealed class SpyRoomGameState
{
    public string? CurrentSecretWord { get; set; }
    public string? CurrentCategory { get; set; }
    public SpyGamePhase CurrentPhase { get; set; } = SpyGamePhase.None;
    public TimerState RoundTimerState { get; init; } = default!;
    public DateTime? RoundStartedAt { get; set; }
    public VotingStateBase? ActiveVoting { get; set; }
    // Only caught spy can guess word during last chance phase
    public string? CaughtSpyId { get; set; }
    public DateTime? SpyLastChanceEndsAt { get; set; }
    public SpyGameEndReason? GameEndReason { get; set; }
    public SpyTeam? WinnerTeam { get; set; }

    public SpyRoomGameState()
    {
        RoundTimerState = new TimerState();
    }
}