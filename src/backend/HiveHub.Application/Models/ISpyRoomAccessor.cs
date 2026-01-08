using FluentResults;
using HiveHub.Application.Errors;
using HiveHub.Application.Interfaces;
using HiveHub.Domain;
using RedLockNet;

namespace HiveHub.Application.Models;

public interface ISpyRoomAccessor
{
    string RoomCode { get; }
    Task<Result<T>> ExecuteAsync<T>(Func<SpyRoom, Result<T>> action);
    Task<Result> ExecuteAsync(Func<SpyRoom, Result> action);
    Task<Result> ExecuteAsync(Action<SpyRoom> action);
    Task<Result<T>> ReadAsync<T>(Func<SpyRoom, T> selector);
    Task<bool> IsInactiveAsync(TimeSpan expirationThreshold);
}










/*public class RedisSpyRoomAccessor : ISpyRoomAccessor
{
    private readonly string _roomCode;
    private readonly ISpyGameRepository _repository;
    private readonly IDistributedLockFactory _lockFactory;

    public string RoomCode => _roomCode;

    public RedisSpyRoomAccessor(
        string roomCode,
        IRoomRepository repository,
        IDistributedLockFactory lockFactory)
    {
        _roomCode = roomCode;
        _repository = repository;
        _lockFactory = lockFactory;
    }

    public async Task<Result<T>> ExecuteAsync<T>(Func<SpyRoom, Result<T>> action)
    {
        var resource = $"lock:room:{_roomCode}";
        var expiry = TimeSpan.FromSeconds(30); // Час життя лока (якщо сервер впаде)
        var wait = TimeSpan.FromSeconds(5);    // Скільки чекаємо, поки лок зайнятий
        var retry = TimeSpan.FromMilliseconds(200); // Як часто пінгуємо

        // RedLock сам спробує взяти лок кілька разів протягом 'wait' часу
        using (var redLock = await _lockFactory.CreateLockAsync(resource, expiry, wait, retry))
        {
            if (!redLock.IsAcquired)
            {
                // Якщо за 5 секунд не пробилися — тоді вже помилка
                return Results.Fail("Server is under heavy load. Please try again.");
            }

            // 1. Load (DB -> Object)
            var room = await _repository.GetAsync(_roomCode);
            if (room == null) return Results.Fail(new NotFound("Room not found"));

            // 2. Execute Business Logic
            var result = action(room);

            // 3. Save (Object -> DB)
            if (result.IsSuccess)
            {
                // Optimistic Concurrency Check можна додати тут, якщо треба
                await _repository.UpdateAsync(room);
            }

            return result;
        }
    }

    public Task<T> ExecuteAsync<T>(Func<SpyRoom, T> action)
    {
        throw new NotImplementedException();
    }

    public Task ExecuteAsync(Action<SpyRoom> action)
    {
        throw new NotImplementedException();
    }
}*/