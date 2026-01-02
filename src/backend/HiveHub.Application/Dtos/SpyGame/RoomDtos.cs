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
    List<WordsCategory> WordsCategories);

public record WordsCategory(
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