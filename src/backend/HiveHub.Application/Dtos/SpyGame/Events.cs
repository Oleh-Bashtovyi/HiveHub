using HiveHub.Domain.Models;

namespace HiveHub.Application.Dtos.SpyGame;

public record SpyGameSettingsUpdatedEventDto(
    string RoomCode,
    SpyRoomGameSettingsDto Settings
);

public record TimerStoppedEventDto(
    string RoomCode,
    string PlayerId,
    int VotesCount,
    int RequiredVotes);

public record VotingResultEventDto(
    string RoomCode,
    bool IsSuccess,
    SpyGamePhase CurrentGamePhase,
    string? ResultMessage,
    string? AccusedId
);



public record GameStartedEventDto(
    SpyRoomStateDto State
);

public record VotingStartedEventDto(
    string RoomCode,
    string InitiatorId,
    string? TargetId,
    SpyVotingType VotingType,
    SpyGamePhase CurrentGamePhase,
    DateTime EndsAt
);

public record VoteCastEventDto(
    string RoomCode,
    string VoterId,
    TargetVoteType? TargetVoteType,
    string? AgainstPlayerId
);

public record GameEndedEventDto(
    string RoomCode,
    Team WinnerTeam,
    GameEndReason Reason,
    string? ReasonMessage
);