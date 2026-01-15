namespace HiveHub.Domain.Models.Shared;

public abstract class PlayerBase<TPlayerState>
{
    public string ConnectionId { get; set; } = string.Empty;
    public string IdInRoom { get; init; } = string.Empty;
    public bool IsConnected { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public string AvatarId { get; set; } = string.Empty;
    public bool IsHost { get; set; } = false;
    public bool IsReady { get; set; } = false;

    public TPlayerState PlayerState { get; init; } = default!;
}