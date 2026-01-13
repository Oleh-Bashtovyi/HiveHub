using HiveHub.Domain.Models;

namespace HiveHub.Application.Dtos.SpyGame;

public record GameSettingsUpdatedEventDto(
    string RoomCode,
    SpyRoomGameSettingsDto Settings
);

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

public record VotingResultEventDto(
    string RoomCode,
    bool IsSuccess,
    SpyGamePhase CurrentGamePhase,
    string? ResultMessage,
    string? AccusedId
);

public record PhaseChangedEventDto(
    string RoomCode,
    SpyGamePhase Phase
);

public record PlayerJoinedEventDto(
    string RoomCode,
    SpyPlayerDto Player
);

public record GameStartedEventDto(
    SpyRoomStateDto State
);

public record VotingStartedEventDto(
    string RoomCode,
    string InitiatorId,
    string? TargetId,
    VotingType VotingType,
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