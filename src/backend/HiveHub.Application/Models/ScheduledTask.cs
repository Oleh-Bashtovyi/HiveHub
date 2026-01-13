namespace HiveHub.Application.Models;

public enum TaskType
{
    SpyGameEndTimeUp,
    SpyPlayerDisconnectTimeout,
    SpyVotingTimeUp
}

public record ScheduledTask(
    TaskType Type,
    string RoomCode,
    string? TargetId
);