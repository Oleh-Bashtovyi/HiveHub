using HiveHub.Application.Dtos.Shared;
using HiveHub.Domain.Models;

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
    bool? IsSpy,
    bool? IsVotedToStopTimer
) : PlayerDto(Id, Name, AvatarId, IsHost, IsReady, IsConnected);

public record SpyRevealDto(
    string PlayerId,
    bool IsSpy
);

public record SpyRoomStateDto(
    string RoomCode,
    RoomStatus Status,
    List<SpyPlayerDto> Players,
    List<ChatMessageDto> Messages,
    SpyRoomGameSettingsDto Settings,
    SpyGameStateDto? GameState,
    long Version
);

public record SpyRoomGameSettingsDto(
    int TimerMinutes,
    int MinSpiesCount,
    int MaxSpiesCount,
    bool SpiesKnowEachOther,
    bool ShowCategoryToSpy,
    List<WordsCategoryDto> CustomCategories
);

public record SpyGameStateDto(
    string? CurrentSecretWord,
    string? Category,
    DateTime RoundStartedAt,
    bool IsRoundTimerStopped,
    DateTime? RoundTimerStartedAt,
    DateTime? RoundTimerWillStopAt,
    DateTime? RoundTimerPausedAt,
    DateTime? SpyLastChanceEndsAt,
    int TimerVotesCount,
    SpyGamePhase Phase,
    VotingStateDto? ActiveVoting,
    string? CaughtSpyId,
    string? CaughtSpyName
);

public record VotingStateDto(
    SpyVotingType Type,
    string? AccusedPlayerId,
    string? AccusedPlayerName,
    Dictionary<string, TargetVoteType>? TargetVoting, // player with id voted as ... against player with AccusedPlayerId
    Dictionary<string, string>? AgainstVoting,        // player with id against player with id
    int? votesRequired,                               // votes reqired when accusing player
    DateTime StartedAt,
    DateTime EndsAt
);

public record WordsCategoryDto(
    string Name,
    List<string> Words
);
