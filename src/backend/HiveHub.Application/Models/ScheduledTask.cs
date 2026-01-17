namespace HiveHub.Application.Models;

public enum TaskType
{
    SpyGameRoundTimeUp,
    SpyGameVotingTimeUp,
    SpyGameLastChanceTimeUp,
    SpyGamePlayerDisconnectedTimeout,
}

public record ScheduledTask(
    TaskType Type,
    string RoomCode,
    string? TargetId
);
