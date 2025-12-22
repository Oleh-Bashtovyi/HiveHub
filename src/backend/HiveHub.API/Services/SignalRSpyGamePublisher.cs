using HiveHub.API.Hubs;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using Microsoft.AspNetCore.SignalR;
using System.Xml.Linq;

namespace HiveHub.API.Services;

public class SignalRSpyGamePublisher : ISpyGamePublisher
{
    private readonly IHubContext<SpyGameHub, ISpyGameClient> _hub;

    public SignalRSpyGamePublisher(IHubContext<SpyGameHub, ISpyGameClient> hub)
    {
        _hub = hub;
    }

    public async Task AddPlayerToRoomGroupAsync(string connectionId, string roomCode)
    {
        await _hub.Groups.AddToGroupAsync(connectionId, roomCode);
    }

    public async Task PublishPlayerChangedNameAsync(PlayerChangedNameEventDto eventDto)
    {
        await _hub.Clients.Group(eventDto.RoomCode)
            .PlayerChangedName(eventDto);
    }

    public async Task PublishPlayerJoinedAsync(PlayerJoinedEventDto eventDto)
    {
        await _hub.Clients.Group(eventDto.RoomCode)
            .PlayerJoined(eventDto);
    }

    public async Task PublishPlayerKickedAsync(PlayerKickedEventDto eventDto)
    {
        await _hub.Clients.Group(eventDto.RoomCode)
            .PlayerKicked(eventDto);
    }

    public Task PublishPlayerLeftAsync(PlayerLeftEventDto eventDto)
    {
        return _hub.Clients.Group(eventDto.RoomCode)
            .PlayerLeft(eventDto);
    }
}
