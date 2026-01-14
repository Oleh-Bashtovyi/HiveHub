using HiveHub.Application.Models;
using HiveHub.Domain.Models;

namespace HiveHub.Application.Services;

public interface ISpyGameRepository
{
    Task<string> GenerateUniqueRoomCodeAsync();
    IRoomAccessor<SpyRoom>? GetRoom(string roomCode);
    Task<bool> TryAddRoomAsync(SpyRoom room);
    Task RemoveRoomAsync(string roomCode);
    Task<int> RemoveInactiveRoomsAsync(TimeSpan expirationThreshold);
}
