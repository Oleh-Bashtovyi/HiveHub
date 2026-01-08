using HiveHub.Domain;

namespace HiveHub.Application.Interfaces;

public interface IRoomStorage
{
    Task<SpyRoom?> LoadAsync(string roomCode);
    Task SaveAsync(SpyRoom room);
}
