using HiveHub.Application.Dtos.Shared;
using HiveHub.Domain.Models;

namespace HiveHub.Application.Dtos.SpyGame;

public record TargetedGameStartedEvent(
    string ConnectionId, 
    SpyGameStartedEventDto Payload
) : IRoomEvent;

public record SpyGameStartedEventDto(
    SpyRoomStateDto State
) : IRoomEvent;

public record SpyGameEndedEventDto(
    string RoomCode,
    SpyTeam WinnerTeam,
    SpyGameEndReason Reason,
    List<SpyRevealDto> SpiesReveal,
    string? ReasonMessage
) : IRoomEvent;

public record SpyGameRoundTimerStateChangedEventDto(
    string RoomCode,
    bool IsRoundTimerStopped,
    DateTime? RoundTimerStartedAt,
    DateTime? RoundTimerWillStopAt,
    DateTime? RoundTimerPausedAt
) : IRoomEvent;

public record SpyGameSettingsUpdatedEventDto(
    string RoomCode,
    SpyRoomGameSettingsDto Settings
) : IRoomEvent;

public record PlayerVotedToStopTimerEventDto(
    string RoomCode,
    string PlayerId,
    int VotesCount,
    int RequiredVotes
) : IRoomEvent;

public record VotingResultEventDto(
    string RoomCode,
    bool IsSuccess,
    SpyGamePhase CurrentGamePhase,
    string? ResultMessage,
    string? AccusedId,
    bool? IsAccusedSpy,
    DateTime? LastChanceEndsAt
) : IRoomEvent;

public record VotingStartedEventDto(
    string RoomCode,
    string InitiatorId,
    string? TargetId,
    string? TargetName,
    SpyVotingType VotingType,
    SpyGamePhase CurrentGamePhase,
    DateTime EndsAt
) : IRoomEvent;

public record VoteCastEventDto(
    string RoomCode,
    string VoterId,
    TargetVoteType? TargetVoteType,
    string? AgainstPlayerId
) : IRoomEvent;

