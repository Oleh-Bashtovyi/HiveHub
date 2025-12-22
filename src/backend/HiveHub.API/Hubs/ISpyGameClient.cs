using HiveHub.Application.Dtos.Events;

namespace HiveHub.API.Hubs;

public interface ISpyGameClient
{
    Task PlayerJoined(PlayerJoinedEventDto playerJoinedDto);
    Task PlayerLeft(PlayerLeftEventDto playerLeftDto);
    Task PlayerChangedName(PlayerChangedNameEventDto playerChangedNameDto);
    Task PlayerKicked(PlayerKickedEventDto playerKickedDto);
}
