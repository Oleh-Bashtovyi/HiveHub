namespace HiveHub.Application.Dtos.SpyGame;

public record JoinRoomResponseDto(
    PlayerDto Me,
    string RoomCode,
    List<PlayerDto> Players,
    RoomGameSettings Settings);

public record CreateRoomResponseDto(
    string RoomCode,
    PlayerDto Me,
    RoomGameSettings Settings
);

public record PlayerDto(
    string Id,
    string Name,
    bool IsHost);

public record RoomGameSettings(
    int TimerMinutes,
    int SpiesCount,
    bool SpiesKnowEachOther,
    bool ShowCategoryToSpy,
    List<WordsCategory> WordsCategories);

public record WordsCategory(
    string Name,
    List<string> Words);
