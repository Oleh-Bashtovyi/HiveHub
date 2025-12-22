using HiveHub.Domain;

namespace HiveHub.Application.Models;

public sealed class SpyRoomAccessor : IDisposable
{
    private readonly SpyRoom _room;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public string RoomCode => _room.RoomCode;

    public SpyRoomAccessor(SpyRoom room)
    {
        _room = room;
    }

    public async Task<T> ReadAsync<T>(Func<SpyRoom, T> selector)
    {
        await _semaphore.WaitAsync();
        try
        {
            return selector(_room);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<SpyRoom, Task<T>> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await action(_room);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<SpyRoom, T> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            return action(_room);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ExecuteAsync(Action<SpyRoom> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            action(_room);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> IsInactiveAsync(TimeSpan expirationThreshold)
    {
        await _semaphore.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;

            bool isEmpty = _room.Players.IsEmpty;
            bool isOld = (now - _room.CreatedAt) > expirationThreshold;

            return isEmpty || isOld;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _semaphore?.Dispose();
        _disposed = true;
    }
}
