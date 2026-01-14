using HiveHub.Domain.Models;

namespace HiveHub.Application.Interfaces;

public interface IRoomStorage<TRoom>
{
    Task<TRoom?> LoadAsync(string roomCode);
    Task SaveAsync(TRoom room);
}
