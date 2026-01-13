using HiveHub.Application.Dtos.SpyGame;

namespace HiveHub.Application.Dtos.Shared;

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

public record ReturnToLobbyEventDto(
    string RoomCode);

public record AccusationStartedEventDto(
    string RoomCode,
    string InitiatorId,
    string TargetId);

public record ChatMessageEventDto(
    string RoomCode,
    ChatMessageDto Message);
