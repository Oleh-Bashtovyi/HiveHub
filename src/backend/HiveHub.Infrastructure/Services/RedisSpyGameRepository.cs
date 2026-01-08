using HiveHub.Application.Interfaces;
using HiveHub.Application.Models;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using HiveHub.Infrastructure.Models;
using RedLockNet;
using StackExchange.Redis;

namespace HiveHub.Infrastructure.Services;

public class RedisSpyGameRepository : ISpyGameRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly IIdGenerator _idGenerator;
    private readonly IRoomStorage _storage;
    private readonly IDistributedLockFactory _lockFactory;

    public RedisSpyGameRepository(
        IConnectionMultiplexer redis,
        IIdGenerator idGenerator,
        IRoomStorage storage,
        IDistributedLockFactory lockFactory)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _idGenerator = idGenerator;
        _storage = storage;
        _lockFactory = lockFactory;
    }

    public async Task<string> GenerateUniqueRoomCodeAsync()
    {
        var code = _idGenerator.GenerateId(8);

        while (await _db.KeyExistsAsync($"room:{code}"))
        {
            code = _idGenerator.GenerateId(8);
        }

        return code;
    }

    public ISpyRoomAccessor? GetRoom(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode)) return null;
        return new RedisSpyRoomAccessor(roomCode, _storage, _lockFactory);
    }

    public async Task<bool> TryAddRoomAsync(SpyRoom room)
    {
        if (await _db.KeyExistsAsync($"room:{room.RoomCode}"))
        {
            return false;
        }

        await _storage.SaveAsync(room);

        await _db.KeyExpireAsync($"room:{room.RoomCode}", TimeSpan.FromHours(2));

        return true;
    }

    public async Task RemoveRoomAsync(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode)) return;
        await _db.KeyDeleteAsync($"room:{roomCode}");
        await _db.KeyDeleteAsync($"lock:room:{roomCode}");
    }

    public Task<int> RemoveInactiveRoomsAsync(TimeSpan expirationThreshold)
    {
        return Task.FromResult(0);
    }
}
