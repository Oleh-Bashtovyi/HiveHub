using HiveHub.Application.Models;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using System.Collections.Concurrent;

namespace HiveHub.Application.Services;

public sealed class SpyGameManager(IIdGenerator idGenerator)
{
    private readonly ConcurrentDictionary<string, SpyRoomAccessor> _rooms = new();
    private readonly IIdGenerator _idGenerator = idGenerator;

    public string GenerateUniqueRoomCode()
    {
        var code = _idGenerator.GenerateId(8);

        while (_rooms.ContainsKey(code))
        {
            code = _idGenerator.GenerateId(8);
        }

        return code;
    }

    public SpyRoomAccessor? GetRoom(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode)) return null;

        _rooms.TryGetValue(roomCode, out var accessor);
        
        return accessor;
    }

    public bool TryAddRoom(SpyRoom room)
    {
        var accessor = new SpyRoomAccessor(room);

        return _rooms.TryAdd(room.RoomCode, accessor);
    }

    public void RemoveRoom(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode)) return;
        _rooms.TryRemove(roomCode, out _);
    }

    public async Task<int> RemoveInactiveRoomsAsync(TimeSpan expirationThreshold)
    {
        var roomsToRemove = new List<string>();

        foreach (var (code, accessor) in _rooms)
        {
            if (await accessor.IsInactiveAsync(expirationThreshold))
            {
                roomsToRemove.Add(code);
            }
        }

        int count = 0;
        foreach (var code in roomsToRemove)
        {
            if (_rooms.TryRemove(code, out var accessor))
            {
                accessor.Dispose();
                count++;
            }
        }

        return count;
    }
}
