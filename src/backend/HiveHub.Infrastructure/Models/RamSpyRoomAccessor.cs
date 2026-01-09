using FluentResults;
using HiveHub.Application.Models;
using HiveHub.Domain;

namespace HiveHub.Infrastructure.Models;

public sealed class RamSpyRoomAccessor(SpyRoom room) : ISpyRoomAccessor, IDisposable
{
    private readonly SpyRoom _room = room;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public string RoomCode => _room.RoomCode;

    public async Task<Result<T>> ExecuteAsync<T>(Func<SpyRoom, Result<T>> action)
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

    public async Task<Result<T>> ReadAsync<T>(Func<SpyRoom, T> selector)
    {
        await _semaphore.WaitAsync();
        try
        {
            var data = selector(_room);
            return Result.Ok(data);
        }
        finally 
        { 
            _semaphore.Release(); 
        }
    }

    public async Task<Result> ExecuteAsync(Func<SpyRoom, Result> action)
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

    public async Task<Result> ExecuteAsync(Action<SpyRoom> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            action(_room);
            return Result.Ok();
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
            return _room.Players.Count == 0 && (now - _room.CreatedAt) > expirationThreshold;
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