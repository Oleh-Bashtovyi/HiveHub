using HiveHub.Application.Models;
using HiveHub.Application.Services;
using HiveHub.Domain.Models.SpyGame;
using System.Diagnostics.CodeAnalysis;

namespace HiveHub.Application.Extensions;

public static class GameRepositoryExtensions
{
    public static bool TryGetRoom(this ISpyGameRepository repository, string roomId, [NotNullWhen(true)] out IRoomAccessor<SpyRoom>? accessor)
    {
        accessor = repository.GetRoom(roomId);
        return accessor != null;
    }
}
