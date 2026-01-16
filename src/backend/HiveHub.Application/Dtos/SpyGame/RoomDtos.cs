using HiveHub.Application.Dtos.Shared;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;

namespace HiveHub.Application.Dtos.SpyGame;

public enum SpyVotingType
{
    Accusation,
    Final
}

public record SpyPlayerDto(
    string Id,
    string Name,
    string AvatarId,
    bool IsHost,
    bool IsReady,
    bool IsConnected,
    bool? HasUsedAccusation,
    bool? IsDead,
    bool? IsSpy,
    bool? IsVotedToStopTimer
) : PlayerDto(Id, Name, AvatarId, IsHost, IsReady, IsConnected);

public record SpyGameRulesDto(
    int TimerMinutes,
    int MinSpiesCount,
    int MaxSpiesCount,
    int MaxPlayersCount,
    bool IsSpiesKnowEachOther,
    bool IsShowCategoryToSpy,
    bool IsSpiesPlayAsTeam
);

public record SpyGameWordPacksDto(
    List<WordsCategoryDto> CustomCategories
);

public record WordsCategoryDto(
    string Name,
    List<string> Words
);

public record SpyRoomStateDto(
    string RoomCode,
    RoomStatus Status,
    List<SpyPlayerDto> Players,
    List<ChatMessageDto> Messages,
    SpyGameRulesDto Rules,
    SpyGameWordPacksDto WordPacks,
    SpyGameStateDto? GameState,
    long Version
);

public record SpyGameStateDto(
    // Secreat word and category
    string? CurrentSecretWord,
    string? CurrentCategory,
    // SpyFinalGuessing
    string? CaughtSpyId,
    string? CaughtSpyName,
    // Game phase
    SpyGamePhase Phase,
    // Voting
    VotingStateDto? ActiveVoting,
    // Round timer
    bool IsRoundTimerStopped,
    DateTime RoundStartedAt,
    DateTime? RoundTimerStartedAt,
    DateTime? RoundTimerWillStopAt,
    DateTime? RoundTimerPausedAt,
    DateTime? SpyLastChanceEndsAt,
    // Timer stop requirements
    int PlayersVotedToStopTimer,
    int VotesRequiredToStopTimer,
    // FinalResults
    List<SpyRevealDto>? SpiecsReveal,
    SpyGameEndReason? RoundEndReason
);

public record SpyRevealDto(
    string PlayerId,
    string PlayerName,
    bool IsSpy,
    bool IsDead
);

public record VotingStateDto(
    SpyVotingType Type,
    string? AccusedPlayerId,
    string? AccusedPlayerName,
    Dictionary<string, TargetVoteType>? TargetVoting, // player with id voted as ... against player with AccusedPlayerId
    Dictionary<string, string?>? AgainstVoting,        // player with id against player with id
    int? votesRequired,                               // votes reqired when accusing player
    DateTime StartedAt,
    DateTime EndsAt
);


