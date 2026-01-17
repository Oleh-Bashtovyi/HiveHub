using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Models;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models.SpyGame;

namespace HiveHub.Infrastructure.Models;

public sealed class InMemorySpyRoomAccessor(SpyRoom room) : IRoomAccessor<SpyRoom>, IDisposable
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
            if (_room.IsMarkedAsDeleted)
            {
                return Results.NotFound<T>(ProjectMessages.RoomNotFound);
            }

            var result = action(_room);

            if (result.IsSuccess)
            {
                _room.IncrementVersion();
            }

            return result;
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
            if (_room.IsMarkedAsDeleted)
            {
                return Results.NotFound<T>(ProjectMessages.RoomNotFound);
            }

            var data = selector(_room);
            return Result.Ok(data);
        }
        finally 
        { 
            _semaphore.Release(); 
        }
    }

    public async Task<Result<T>> ReadAsync<T>(Func<SpyRoom, Result<T>> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_room.IsMarkedAsDeleted)
            {
                return Results.NotFound<T>(ProjectMessages.RoomNotFound);
            }

            return action(_room);
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
            if (_room.IsMarkedAsDeleted)
            {
                return Results.NotFound(ProjectMessages.RoomNotFound);
            }

            var result = action(_room);

            if (result.IsSuccess)
            {
                _room.IncrementVersion();
            }

            return result;
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
            if (_room.IsMarkedAsDeleted)
            {
                return Results.NotFound(ProjectMessages.RoomNotFound);
            }

            action(_room);
            _room.IncrementVersion();
            return Result.Ok();
        }
        finally 
        { 
            _semaphore.Release(); 
        }
    }

    public async Task<Result<T>> ExecuteAsync<T>(Func<SpyRoom, Task<Result<T>>> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_room.IsMarkedAsDeleted)
            {
                return Results.NotFound<T>(ProjectMessages.RoomNotFound);
            }

            var result = await action(_room);

            if (result.IsSuccess)
            {
                _room.IncrementVersion();
            }

            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Result> ExecuteAsync(Func<SpyRoom, Task<Result>> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_room.IsMarkedAsDeleted)
            {
                return Results.NotFound(ProjectMessages.RoomNotFound);
            }

            var result = await action(_room);

            if (result.IsSuccess)
            {
                _room.IncrementVersion();
            }

            return result;
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
            if (_room.IsMarkedAsDeleted)
            {
                return true;
            }

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