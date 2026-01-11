using HiveHub.Domain;

namespace HiveHub.Application.Dtos.SpyGame;

public record JoinRoomResponseDto(
    PlayerDto Me,
    string RoomCode,
    List<PlayerDto> Players,
    RoomGameSettingsDto Settings);

public record CreateRoomResponseDto(
    string RoomCode,
    PlayerDto Me,
    RoomGameSettingsDto Settings
);

public record PlayerDto(
    string Id,
    string Name,
    bool IsHost,
    bool IsReady,
    string AvatarId);

public record RoomGameSettingsDto(
    int TimerMinutes,
    int SpiesCount,
    bool SpiesKnowEachOther,
    bool ShowCategoryToSpy,
    List<WordsCategoryDto> WordsCategories);

public record WordsCategoryDto(
    string Name,
    List<string> Words);

public record ChatMessageDto(
    string PlayerId,
    string PlayerName,
    string Message,
    DateTime Timestamp);

public record RevealSpiesDto(
    string RoomCode,
    List<SpyRevealDto> Spies);

public record SpyRevealDto(
    string PlayerId,
    string PlayerName);

public record RoomStateDto(
    string RoomCode,
    RoomState State,
    List<PlayerDto> Players,
    RoomGameSettingsDto Settings,
    GameStateDto? GameState,
    long Version
);

public record GameStateDto(
    string? CurrentSecretWord,
    string? Category,
    DateTime GameStartTime,
    DateTime? GameEndTime,
    bool IsTimerStopped,
    DateTime? TimerStoppedAt,
    int TimerVotesCount,
    List<ChatMessageDto> RecentMessages
);

