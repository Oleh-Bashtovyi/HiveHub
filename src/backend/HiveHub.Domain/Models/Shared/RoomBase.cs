using System.Diagnostics.CodeAnalysis;

namespace HiveHub.Domain.Models.Shared;

public enum RoomStatus
{
    Lobby,
    InGame,
    Ended
}

public abstract class RoomBase<TGameState, TGameSettings, TPlayer, TPlayerState>(string code)
    where TPlayer : PlayerBase<TPlayerState>
{
    public string RoomCode { get; } = code;
    public List<TPlayer> Players { get; } = new();
    public RoomStatus Status { get; set; } = RoomStatus.Lobby;
    public TGameSettings GameSettings { get; init; } = default!;
    public TGameState GameState { get; protected set; } = default!;
    public long StateVersion { get; private set; } = 0;
    public DateTime StateVersionChangedAt { get; private set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public bool IsMarkedAsDeleted { get; private set; } = false;

    public bool IsInLobby() => Status == RoomStatus.Lobby;
    public bool IsInGame() => Status == RoomStatus.InGame;

    public bool TryGetPlayerByConnectionId(string connectionId, [NotNullWhen(true)] out TPlayer? player)
    {
        player = Players.FirstOrDefault(x => x.ConnectionId == connectionId);
        return player != null;
    }

    public bool TryGetPlayerByIdInRoom(string idInRoom, [NotNullWhen(true)] out TPlayer? player)
    {
        player = Players.FirstOrDefault(x => x.IdInRoom == idInRoom);
        return player != null;
    }

    public void StartGame(TGameState initialState)
    {
        if (initialState == null)
            throw new ArgumentNullException(nameof(initialState));

        if (Status == RoomStatus.InGame)
            throw new InvalidOperationException("Game is already in progress.");

        GameState = initialState;
        Status = RoomStatus.InGame;
    }

    public void IncrementVersion(DateTime? now = null)
    {
        StateVersion++;
        StateVersionChangedAt = now ?? DateTime.UtcNow;
    }

    public void MarkAsDeleted() => IsMarkedAsDeleted = true;
}
