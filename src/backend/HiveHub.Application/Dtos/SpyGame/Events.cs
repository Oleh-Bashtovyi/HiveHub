using HiveHub.Application.Dtos.Shared;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;

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

public record SpyMadeGuessEventDto(
    string RoomCode,
    string PlayerId,
    string Word,
    bool IsGuessCorrect,
    bool IsSpyDead
) : IRoomEvent;

public record SpyGameRoundTimerStateChangedEventDto(
    string RoomCode,
    TimerStatus TimerStatus,
    double RemainingSeconds
) : IRoomEvent;

public record SpyGameRulesUpdatedEventDto(
    string RoomCode,
    SpyGameRulesDto Rules
) : IRoomEvent;

public record SpyGameWordPacksUpdatedEvent(
    string RoomCode,
    SpyGameWordPacksDto Packs
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
    string? AccusedSpyName,
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

