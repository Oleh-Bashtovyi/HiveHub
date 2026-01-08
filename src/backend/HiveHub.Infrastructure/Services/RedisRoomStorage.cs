using HiveHub.Application.Interfaces;
using HiveHub.Domain;
using StackExchange.Redis;

namespace HiveHub.Infrastructure.Services;

public class RedisRoomStorage : IRoomStorage
{
    private readonly IDatabase _db;

    public RedisRoomStorage(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<SpyRoom?> LoadAsync(string roomCode)
    {
        var json = await _db.StringGetAsync($"room:{roomCode}");
        if (!json.HasValue) return null;

        return System.Text.Json.JsonSerializer.Deserialize<SpyRoom>(json.ToString());
    }

    public async Task SaveAsync(SpyRoom room)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(room);

        await _db.StringSetAsync($"room:{room.RoomCode}", json);
        await _db.KeyExpireAsync($"room:{room.RoomCode}", TimeSpan.FromHours(1));
    }
}