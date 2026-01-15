using HiveHub.Application.Constants;
using HiveHub.Application.Models;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models.SpyGame;
using HiveHub.Infrastructure.Models;
using System.Collections.Concurrent;

namespace HiveHub.Infrastructure.Services;

public sealed class InMemorySpyGameRepository(IIdGenerator idGenerator) : ISpyGameRepository
{
    private readonly ConcurrentDictionary<string, InMemorySpyRoomAccessor> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly IIdGenerator _idGenerator = idGenerator;

    public Task<string> GenerateUniqueRoomCodeAsync()
    {
        var code = _idGenerator.GenerateId(ProjectConstants.RoomCodeLength).ToUpperInvariant();
        while (_rooms.ContainsKey(code))
        {
            code = _idGenerator.GenerateId(ProjectConstants.RoomCodeLength).ToUpperInvariant();
        }
        return Task.FromResult(code);
    }

    public IRoomAccessor<SpyRoom>? GetRoom(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode)) return null;
        _rooms.TryGetValue(roomCode, out var accessor);
        return accessor;
    }

    public Task<bool> TryAddRoomAsync(SpyRoom room)
    {
        var accessor = new InMemorySpyRoomAccessor(room);
        var result = _rooms.TryAdd(room.RoomCode, accessor);
        return Task.FromResult(result);
    }

    public Task RemoveRoomAsync(string roomCode)
    {
        if (!string.IsNullOrEmpty(roomCode) && _rooms.TryRemove(roomCode, out var accessor))
        {
            accessor.Dispose();
        }
        return Task.CompletedTask;
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
