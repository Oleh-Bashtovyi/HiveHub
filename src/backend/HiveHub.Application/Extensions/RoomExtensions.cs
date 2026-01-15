using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Domain.Models.SpyGame;

namespace HiveHub.Application.Extensions;

public static class RoomExtensions
{
    public static List<SpyRevealDto> GetSpyRevealDto(this SpyRoom room)
    {
        return room.Players.Select(p => new SpyRevealDto(p.IdInRoom, p.PlayerState.IsSpy)).ToList();
    }
}
