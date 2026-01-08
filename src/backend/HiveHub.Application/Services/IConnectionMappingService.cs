using System.Collections.Concurrent;

namespace HiveHub.Application.Services;

public interface IConnectionMappingService
{
    void Map(string connectionId, string roomCode);
    void Unmap(string connectionId);
    string? GetRoomCode(string connectionId);
}

public class ConnectionMappingService : IConnectionMappingService
{
    // Key: ConnectionId, Value: RoomCode
    private readonly ConcurrentDictionary<string, string> _connections = new();

    public void Map(string connectionId, string roomCode)
    {
        _connections.TryAdd(connectionId, roomCode);
    }

    public void Unmap(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public string? GetRoomCode(string connectionId)
    {
        _connections.TryGetValue(connectionId, out var roomCode);
        return roomCode;
    }
}