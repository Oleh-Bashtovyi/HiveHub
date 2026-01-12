namespace HiveHub.Application.Models;

public enum TaskType
{
    SpyGameEndTimeUp,
    SpyPlayerDisconnectTimeout
}

public record ScheduledTask(
    TaskType Type,
    string RoomCode,
    string? TargetId
);