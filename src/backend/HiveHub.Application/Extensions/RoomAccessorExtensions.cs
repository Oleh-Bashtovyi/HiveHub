using FluentResults;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Domain.Models;

namespace HiveHub.Application.Extensions;

public static class RoomAccessorExtensions
{
    public static async Task<Result> ExecuteAndDispatchAsync(
        this IRoomAccessor<SpyRoom> accessor,
        SpyGameEventsContext context,
        Func<SpyRoom, Result> action)
    {
        var result = await accessor.ExecuteAsync(action);

        if (result.IsFailed)
        {
            context.Clear();
            return result;
        }

        if (context.HasEvents)
        {
            await context.DispatchAsync();
        }

        return result;
    }

    public static async Task<Result<T>> ExecuteAndDispatchAsync<T>(
        this IRoomAccessor<SpyRoom> accessor,
        SpyGameEventsContext context,
        Func<SpyRoom, Result<T>> action)
    {
        var result = await accessor.ExecuteAsync(action);

        if (result.IsFailed)
        {
            context.Clear();
            return result;
        }

        if (context.HasEvents)
        {
            await context.DispatchAsync();
        }

        return result;
    }
}
