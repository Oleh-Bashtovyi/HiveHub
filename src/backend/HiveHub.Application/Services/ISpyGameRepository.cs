using HiveHub.Application.Models;
using HiveHub.Domain;

namespace HiveHub.Application.Services;

public interface ISpyGameRepository
{
    Task<string> GenerateUniqueRoomCodeAsync();
    ISpyRoomAccessor? GetRoom(string roomCode);
    Task<bool> TryAddRoomAsync(SpyRoom room);
    Task RemoveRoomAsync(string roomCode);
    Task<int> RemoveInactiveRoomsAsync(TimeSpan expirationThreshold);
}
