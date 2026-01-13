using HiveHub.Domain.Models;

namespace HiveHub.Application.Dtos.SpyGame;

public record JoinRoomResponseDto(
    PlayerDto Me,
    RoomStateDto RoomState
);

public record CreateRoomResponseDto(
    PlayerDto Me,
    RoomStateDto RoomState
);

public record RoomStateDto(
    string RoomCode,
    RoomStatus Status,
    List<PlayerDto> Players,
    List<ChatMessageDto> Messages,
    RoomGameSettingsDto Settings,
    GameStateDto? GameState,
    long Version
);

public record PlayerDto(
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

public record RoomGameSettingsDto(
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
    int TimerVotesCount
);

public record WordsCategoryDto(
    string Name,
    List<string> Words
);

public record SpyRevealDto(
    string PlayerId,
    string PlayerName);


