using HiveHub.Domain.Models;

namespace HiveHub.Application.Dtos.SpyGame;

public enum VotingType
{
    Accusation,
    Final
}

public record JoinRoomResponseDto(
    SpyPlayerDto Me,
    SpyRoomStateDto RoomState
);

public record CreateRoomResponseDto(
    SpyPlayerDto Me,
    SpyRoomStateDto RoomState
);

public record SpyRoomStateDto(
    string RoomCode,
    RoomStatus Status,
    List<SpyPlayerDto> Players,
    List<ChatMessageDto> Messages,
    SpyRoomGameSettingsDto Settings,
    GameStateDto? GameState,
    long Version
);

public record SpyPlayerDto(
    string Id,
    string Name,
    string AvatarId,
    bool IsHost,
    bool IsReady,
    bool IsConnected,
    bool? IsSpy,
    bool? IsVotedToStopTimer
);

public record ChatMessageDto(
    string PlayerId,
    string PlayerName,
    string Message,
    DateTime Timestamp
);

public record SpyRoomGameSettingsDto(
    int TimerMinutes,
    int MinSpiesCount,
    int MaxSpiesCount,
    bool SpiesKnowEachOther,
    bool ShowCategoryToSpy,
    List<WordsCategoryDto> CustomCategories
);

public record GameStateDto(
    string? CurrentSecretWord,
    string? Category,
    DateTime GameStartTime,
    DateTime? GameEndTime,
    bool IsTimerStopped,
    DateTime? TimerStoppedAt,
    int TimerVotesCount,
    SpyGamePhase Phase,
    VotingStateDto? ActiveVoting,
    string? CaughtSpyId,
    string? CaughtSpyName
);

public record VotingStateDto(
    VotingType Type,
    string? AccusedPlayerId,
    string? AccusedPlayerName,
    Dictionary<string, TargetVoteType>? TargetVoting, // player with id voted as ... against player with AccusedPlayerId
    Dictionary<string, string>? AgainstVoting,        // player with id against player with id
    int? VotesReqired,                                // votes reqired when accusing player
    DateTime StartedAt,
    DateTime EndsAt
);

public record WordsCategoryDto(
    string Name,
    List<string> Words
);

public record SpyRevealDto(
    string PlayerId,
    string PlayerName);


