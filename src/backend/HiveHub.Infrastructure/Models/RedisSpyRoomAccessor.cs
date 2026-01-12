using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Errors;
using HiveHub.Application.Interfaces;
using HiveHub.Application.Models;
using HiveHub.Domain;
using RedLockNet;

namespace HiveHub.Infrastructure.Models;

public class RedisSpyRoomAccessor : ISpyRoomAccessor
{
    private static readonly TimeSpan ExpireTime = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan WaitTime = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetryTime = TimeSpan.FromMilliseconds(200);

    private readonly string _roomCode;
    private readonly IRoomStorage _storage;
    private readonly IDistributedLockFactory _lockFactory;

    public string RoomCode => _roomCode;

    public RedisSpyRoomAccessor(string roomCode, IRoomStorage storage, IDistributedLockFactory lockFactory)
    {
        _roomCode = roomCode;
        _storage = storage;
        _lockFactory = lockFactory;
    }

    private async Task<Result<T>> RunInLockAsync<T>(
        Func<SpyRoom, Task<Result<T>>> logic,
        bool saveChanges)
    {
        var resource = $"lock:room:{_roomCode}";

        using (var redLock = await _lockFactory.CreateLockAsync(
            resource,
            ExpireTime,
            WaitTime,
            RetryTime))
        {
            if (!redLock.IsAcquired) return Result.Fail("Server busy. Could not acquire lock.");

            var room = await _storage.LoadAsync(_roomCode);
            if (room == null) return Result.Fail(new NotFound(ProjectMessages.RoomNotFound));

            var result = await logic(room);

            if (result.IsSuccess && saveChanges)
            {
                room.IncrementVersion();
                await _storage.SaveAsync(room);
            }
            return result;
        }
    }

    public async Task<Result<T>> ExecuteAsync<T>(Func<SpyRoom, Result<T>> action)
    {
        return await RunInLockAsync(room => Task.FromResult(action(room)), saveChanges: true);
    }

    public async Task<Result> ExecuteAsync(Func<SpyRoom, Result> action)
    {
        var result = await RunInLockAsync(room =>
        {
            var r = action(room);
            return Task.FromResult(r.IsSuccess ? Result.Ok(true) : Result.Fail<bool>(r.Errors));
        }, saveChanges: true);

        return result.ToResult();
    }

    public async Task<Result> ExecuteAsync(Action<SpyRoom> action)
    {
        var result = await RunInLockAsync(room =>
        {
            action(room);
            return Task.FromResult(Result.Ok(true));
        }, saveChanges: true);

        return result.ToResult();
    }

    public async Task<Result<T>> ExecuteAsync<T>(Func<SpyRoom, Task<Result<T>>> action)
    {
        return await RunInLockAsync(room => action(room), saveChanges: true);
    }

    public async Task<Result> ExecuteAsync(Func<SpyRoom, Task<Result>> action)
    {
        var result = await RunInLockAsync(async room =>
        {
            var r = await action(room);

            return r.IsSuccess ? Result.Ok(true) : Result.Fail<bool>(r.Errors);
        }, saveChanges: true);

        return result.ToResult();
    }

    public async Task<Result<T>> ReadAsync<T>(Func<SpyRoom, T> selector)
    {
        return await RunInLockAsync(room =>
        {
            var data = selector(room);
            return Task.FromResult(Result.Ok(data));
        }, saveChanges: false);
    }

    public async Task<Result<T>> ReadAsync<T>(Func<SpyRoom, Result<T>> action)
    {
        return await RunInLockAsync(room =>
        {
            return Task.FromResult(action(room));
        }, saveChanges: false);
    }

    public async Task<bool> IsInactiveAsync(TimeSpan expirationThreshold)
    {
        var room = await _storage.LoadAsync(_roomCode);
        if (room == null) return true;

        var now = DateTime.UtcNow;
        return room.Players.Count == 0 && (now - room.CreatedAt) > expirationThreshold;
    }
}