using HiveHub.Application.Models;
using HiveHub.Application.Services;
using System.Diagnostics.CodeAnalysis;

namespace HiveHub.Application.Extensions;

public static class GameRepositoryExtensions
{
    public static bool TryGetRoom(this ISpyGameRepository repository, string roomId, [NotNullWhen(true)] out ISpyRoomAccessor? accessor)
    {
        accessor = repository.GetRoom(roomId);
        return accessor != null;
    }
}
