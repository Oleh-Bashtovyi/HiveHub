namespace HiveHub.Application.Models;

public enum TaskType
{
    SpyGameRoundTimeUp,
    SpyGameVotingTimeUp,
    SpyGamePlayerDisconnectedTimeout,
}

public record ScheduledTask(
    TaskType Type,
    string RoomCode,
    string? TargetId
);