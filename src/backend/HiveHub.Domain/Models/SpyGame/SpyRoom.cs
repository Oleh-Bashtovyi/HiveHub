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
    /// <summary>
    /// Spies Win: The timer ran out, and civilians failed to identify the spies.
    /// </summary>
    TimerExpired,

    /// <summary>
    /// Spies Win: Civilians voted to kick an innocent player.
    /// </summary>
    CivilianKicked,

    /// <summary>
    /// Spies Win: A spy correctly guessed the secret location.
    /// </summary>
    SpyGuessedWord,

    /// <summary>
    /// Spies Win: Civilians could not reach a consensus during the final vote.
    /// </summary>
    FinalVotingFailed,

    /// <summary>
    /// Civilians Win: A spy made a wrong guess during the game.
    /// </summary>
    SpyWrongGuess,

    /// <summary>
    /// Civilians Win: All spies have been eliminated (kicked via voting).
    /// </summary>
    AllSpiesEliminated,

    /// <summary>
    /// Civilians Win: A caught spy tried to guess the location as a "last chance" but failed.
    /// </summary>
    SpyLastChanceFailed,

    /// <summary>
    /// Paranoia Mode (No Spies): Civilians kicked a player, thinking they were a spy. (Technically a loss for civilians).
    /// </summary>
    ParanoiaSacrifice,

    /// <summary>
    /// Paranoia Mode (No Spies): Civilians survived until the timer expired without kicking anyone.
    /// </summary>
    ParanoiaSurvived
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
    // Store spy reveal results in case if somebody is leave
    public int SpiesCountSnapshot { get; private set; } = 0;
    public List<SpyReveal> SpyRevealSnapshot { get; private set; } = new ();

    public SpyRoomGameState()
    {
        RoundTimerState = new TimerState();
    }

    public void SetResultSnapshot(IEnumerable<SpyPlayer> players)
    {
        SpiesCountSnapshot = players.Count(p => p.PlayerState.IsSpy);
        SpyRevealSnapshot = players.Select(p => new SpyReveal() 
        { 
            IdInRoom = p.IdInRoom, 
            Name = p.Name, 
            IsSpy = p.PlayerState.IsSpy,
            IsDead = p.PlayerState.IsDead,
        }).ToList();
    }
}

public sealed class SpyReveal
{
    public required string IdInRoom { get; init; }
    public required string Name { get; init; }
    public bool IsSpy { get; init; }
    // Vote kick or wrong guess
    public bool IsDead { get; set; }
}