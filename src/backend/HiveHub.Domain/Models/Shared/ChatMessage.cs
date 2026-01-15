namespace HiveHub.Domain.Models.Shared;

public class ChatMessage(string playerId, string playerName, string message, DateTime timestamp)
{
    public string PlayerId { get; } = playerId;
    public string PlayerName { get; } = playerName;
    public string Message { get; } = message;
    public DateTime Timestamp { get; } = timestamp;
}
