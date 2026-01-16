using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Domain.Models.SpyGame;

namespace HiveHub.Application.Extensions;

public static class RoomExtensions
{
    public static List<SpyRevealDto> GetSpyRevealDto(this SpyRoom room)
    {
        return room.GameState.SpyRevealSnapshot.Select(p => new SpyRevealDto(
            PlayerId: p.IdInRoom, 
            PlayerName: p.Name,
            IsDead: p.IsDead,
            IsSpy: p.IsSpy)).ToList();
    }
}
