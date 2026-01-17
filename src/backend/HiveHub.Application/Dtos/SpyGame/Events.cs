using HiveHub.Application.Dtos.Shared;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;

namespace HiveHub.Application.Dtos.SpyGame;

public record TargetedGameStartedEventDto(
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
    string Category,
    string SecretWord,
    string? ReasonMessage
) : IRoomEvent;

public record SpyMadeGuessEventDto(
    string RoomCode,
    string PlayerId,
    string Word,
    bool IsGuessCorrect,
    bool IsSpyDead
) : IRoomEvent;

public record SpyGameRulesUpdatedEventDto(
    string RoomCode,
    SpyGameRulesDto Rules
) : IRoomEvent;

public record SpyGameWordPacksUpdatedEventDto(
    string RoomCode,
    SpyGameWordPacksDto Packs
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

public record GamePhaseChangedEventDto(
    string RoomCode,
    SpyGamePhase NewPhase,
    SpyGamePhase PreviousPhase
) : IRoomEvent;

public record SpyGameRoundTimerStateChangedEventDto(
    string RoomCode,
    TimerStatus Status,
    double RemainingSeconds,
    TimerChangeReason Reason
) : IRoomEvent;

public enum TimerChangeReason
{
    Started,
    Paused,
    Resumed,
    Stopped,
    Expired
}

public record PlayerVotedToStopTimerEventDto(
    string RoomCode,
    string PlayerId,
    int CurrentVotes,
    int RequiredVotes
) : IRoomEvent;

public record VotingStartedEventDto(
    string RoomCode,
    string InitiatorId,
    string? TargetId,
    string? TargetName,
    SpyVotingType VotingType,
    DateTime EndsAt
) : IRoomEvent;

public record VoteCastEventDto(
    string RoomCode,
    string VoterId,
    string VoterName,
    TargetVoteType? TargetVoteType,
    string? AgainstPlayerId,
    int CurrentVotes,
    int RequiredVotes
) : IRoomEvent;

public record VotingCompletedEventDto(
    string RoomCode,
    bool IsSuccess,
    SpyVotingType VotingType,
    string ResultMessage
) : IRoomEvent;

public record PlayerEliminatedEventDto(
    string RoomCode,
    string PlayerId,
    string PlayerName,
    bool WasSpy,
    EliminationReason Reason
) : IRoomEvent;

public enum EliminationReason
{
    VotedOut,
    FailedGuess
}

public record SpyRevealedEventDto(
    string RoomCode,
    string SpyId,
    string SpyName
) : IRoomEvent;

public record SpyLastChanceStartedEventDto(
    string RoomCode,
    string SpyId,
    string SpyName,
    DateTime EndsAt
) : IRoomEvent;

public record SpyGuessAttemptedEventDto(
    string RoomCode,
    string SpyId,
    string GuessedWord,
    bool IsCorrect
) : IRoomEvent;