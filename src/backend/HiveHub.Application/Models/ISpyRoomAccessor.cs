using FluentResults;
using HiveHub.Domain;

namespace HiveHub.Application.Models;

public interface ISpyRoomAccessor
{
    string RoomCode { get; }
    Task<Result<T>> ExecuteAsync<T>(Func<SpyRoom, Task<Result<T>>> action);
    Task<Result> ExecuteAsync(Func<SpyRoom, Task<Result>> action);
    Task<Result<T>> ExecuteAsync<T>(Func<SpyRoom, Result<T>> action);
    Task<Result> ExecuteAsync(Func<SpyRoom, Result> action);
    Task<Result> ExecuteAsync(Action<SpyRoom> action);
    Task<Result<T>> ReadAsync<T>(Func<SpyRoom, T> selector);
    Task<Result<T>> ReadAsync<T>(Func<SpyRoom, Result<T>> action);
    Task<bool> IsInactiveAsync(TimeSpan expirationThreshold);
}
