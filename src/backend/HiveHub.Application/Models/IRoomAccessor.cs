using FluentResults;

namespace HiveHub.Application.Models;

public interface IRoomAccessor<TRoom>
{
    string RoomCode { get; }
    Task<Result<T>> ExecuteAsync<T>(Func<TRoom, Task<Result<T>>> action);
    Task<Result> ExecuteAsync(Func<TRoom, Task<Result>> action);
    Task<Result<T>> ExecuteAsync<T>(Func<TRoom, Result<T>> action);
    Task<Result> ExecuteAsync(Func<TRoom, Result> action);
    Task<Result> ExecuteAsync(Action<TRoom> action);
    Task<Result<T>> ReadAsync<T>(Func<TRoom, T> selector);
    Task<Result<T>> ReadAsync<T>(Func<TRoom, Result<T>> action);
    Task<bool> IsInactiveAsync(TimeSpan expirationThreshold);
}
