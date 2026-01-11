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

public record PlayerLeftEventDto(
    string RoomCode, 
    string PlayerId);

public record PlayerChangedNameEventDto(
    string RoomCode, 
    string PlayerId, 
    string NewName);

public record PlayerKickedEventDto(
    string RoomCode, 
    string PlayerId, 
    string KickedByPlayerId);

public record PlayerReadyStatusChangedEventDto(
    string RoomCode, 
    string PlayerId, 
    bool IsReady);

public record PlayerChangedAvatarEventDto(
    string RoomCode, 
    string PlayerId, 
    string NewAvatarId);

public record HostChangedEventDto(
    string RoomCode, 
    string NewHostId);

public record ChatMessageEventDto(
    string RoomCode, 
    ChatMessageDto Message);

public record TimerStoppedEventDto(
    string RoomCode, 
    string PlayerId,
    int VotesCount, 
    int RequiredVotes);

public record SpiesRevealedEventDto(
    string RoomCode, 
    List<SpyRevealDto> Spies);

public record PlayerConnectionChangedEventDto(
    string RoomCode,
    string PlayerId,
    bool IsConnected
);

public record GameEndedEventDto(string RoomCode);

public record ReturnToLobbyEventDto(string RoomCode);
