using HiveHub.Application.Interfaces;
using HiveHub.Domain.Models.SpyGame;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace HiveHub.Infrastructure.Services;

public class RedisRoomStorage : IRoomStorage<SpyRoom>
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisRoomStorage> _logger;

    public RedisRoomStorage(IConnectionMultiplexer redis, ILogger<RedisRoomStorage> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<SpyRoom?> LoadAsync(string roomCode)
    {
        try
        {
            var json = await _db.StringGetAsync($"room:{roomCode}");
            if (!json.HasValue) return null;

            return JsonSerializer.Deserialize<SpyRoom>(json.ToString());
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize room {RoomCode}", roomCode);
            return null;
        }
    }

    public async Task SaveAsync(SpyRoom room)
    {
        try
        {
            var json = JsonSerializer.Serialize(room);
            await _db.StringSetAsync($"room:{room.RoomCode}", json);
            await _db.KeyExpireAsync($"room:{room.RoomCode}", TimeSpan.FromHours(2));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize room {RoomCode}", room.RoomCode);
            throw;
        }
    }
}