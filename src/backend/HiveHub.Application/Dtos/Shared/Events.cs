using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Models;

namespace HiveHub.Application.Dtos.Shared;

public interface IRoomEvent { }

public record AddPlayerToGroupEvent(
    string ConnectionId, 
    string RoomCode
) : IRoomEvent;

public record RemovePlayerFromGroupEvent(
    string ConnectionId, 
    string RoomCode
) : IRoomEvent;


public record ScheduleTaskEvent(
    TaskType Type, 
    string RoomCode,
    string? TargetId, 
    TimeSpan Delay
) : IRoomEvent;

public record CancelTaskEvent(
    TaskType Type, 
    string RoomCode,
    string? TargetId
) : IRoomEvent;

public record PlayerJoinedEventDto<TPlayer>(
    string RoomCode,
    TPlayer Player
) : IRoomEvent;

public record PlayerLeftEventDto(
    string RoomCode,
    string PlayerId
) : IRoomEvent;

public record PlayerChangedNameEventDto(
    string RoomCode,
    string PlayerId,
    string NewName
) : IRoomEvent;

public record PlayerKickedEventDto(
    string RoomCode,
    string PlayerId,
    string KickedByPlayerId
) : IRoomEvent;

public record PlayerConnectionChangedEventDto(
    string RoomCode,
    string PlayerId,
    bool IsConnected
) : IRoomEvent;

public record PlayerReadyStatusChangedEventDto(
    string RoomCode,
    string PlayerId,
    bool IsReady
) : IRoomEvent;

public record PlayerChangedAvatarEventDto(
    string RoomCode,
    string PlayerId,
    string NewAvatarId
) : IRoomEvent;

public record HostChangedEventDto(
    string RoomCode,
    string NewHostId
) : IRoomEvent;

public record ReturnToLobbyEventDto(
    string RoomCode
) : IRoomEvent;

public record AccusationStartedEventDto(
    string RoomCode,
    string InitiatorId,
    string TargetId
) : IRoomEvent;

public record ChatMessageEventDto(
    string RoomCode,
    ChatMessageDto Message
) : IRoomEvent;
