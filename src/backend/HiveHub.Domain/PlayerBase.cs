using System.Collections.Concurrent;

namespace HiveHub.Domain;

public abstract class PlayerBase<TPlayerState>
{
    public string ConnectionId { get; init; }
    public string IdInRoom { get; init; }
    public string Name { get; set; }
    public string AvatarId { get; set; }
    public bool IsHost { get; set; }

    public TPlayerState PlayerState { get; init; }
}

public abstract class RoomBase<TGameSettings, TPlayer, TPlayerState> 
    where TPlayer : PlayerBase<TPlayerState>
{
    private readonly SemaphoreSlim _roomLock = new(1, 1);

    public string RoomCode { get; set; }
    public string GameType { get; set; }
    public RoomState State { get; set; } = RoomState.Lobby;
    public ConcurrentDictionary<string, TPlayer> Players { get; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TGameSettings GameSettings { get; init; }

    public async Task ExecuteLockedAsync(Func<Task> action)
    {
        await _roomLock.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            _roomLock.Release();
        }
    }

    public async Task<T> ExecuteLockedAsync<T>(Func<Task<T>> action)
    {
        await _roomLock.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            _roomLock.Release();
        }
    }

    public async Task<T> ExecuteLockedAsync<T>(Func<T> action)
    {
        await _roomLock.WaitAsync();
        try
        {
            return action();
        }
        finally
        {
            _roomLock.Release();
        }
    }
}

public enum RoomState
{
    Lobby,
    InGame,
    Ended
}

public sealed class SpyPlayerState
{
    public bool IsSpy { get; set; } = false;
}

public sealed class SpyPlayer : PlayerBase<SpyPlayerState>
{
    public SpyPlayer(string connectionId, string idInRoom)
    {
        ConnectionId = connectionId;
        IdInRoom = idInRoom;
        PlayerState = new SpyPlayerState();
    }
}

public class SpyRoomSettings
{
    public int TimerMinutes { get; set; } = 5;
    public int SpiesCount { get; set; } = 1;
    public bool SpiesKnowEachOther { get; set; } = false;
    public bool ShowCategoryToSpy { get; set; } = false;
    public List<SpyGameWordsCategory> Categories { get; set; } = new();
}

public sealed class SpyRoom : RoomBase<SpyRoomSettings, SpyPlayer, SpyPlayerState>
{
    public string CurrentSecretWord { get; set; }
    public DateTime? GameStartTime { get; set; }
    public TimeSpan RemainingTime { get; set; }

    public SpyRoom(string code)
    {
        GameSettings = new SpyRoomSettings();
        RoomCode = code;
        GameType = "Spy";
    }
}

public class SpyGameWordsCategory {
    public string Name { get; set; }
    public List<string> Words { get; set; } = new();
}