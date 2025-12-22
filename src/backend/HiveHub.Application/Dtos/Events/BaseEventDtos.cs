using HiveHub.Application.Dtos.SpyGame;

namespace HiveHub.Application.Dtos.Events;

public record PlayerJoinedEventDto(
    string RoomCode,
    PlayerDto Player
);

public record GameStartedEventDto(
    string RoomCode,
    bool IsSpy,
    string? SecretWord,
    string Category,
    DateTime GameEndTime
);

public record GameSettingsUpdatedEventDto(
    string RoomCode,
    RoomGameSettingsDto Settings
);

public record PlayerLeftEventDto(string RoomCode, string PlayerId);
public record PlayerChangedNameEventDto(string RoomCode, string PlayerId, string NewName);
public record PlayerKickedEventDto(string RoomCode, string PlayerId, string KickedByPlayerId);
