using HiveHub.API.Hubs;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using Microsoft.AspNetCore.SignalR;

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

    public async Task RemovePlayerFromRoomGroupAsync(string connectionId, string roomCode)
    {
        await _hub.Groups.RemoveFromGroupAsync(connectionId, roomCode);
    }

    public Task PublishGameSettingsUpdatedAsync(GameSettingsUpdatedEventDto eventDto)
    {
        return _hub.Clients.Group(eventDto.RoomCode)
            .GameSettingsUpdated(eventDto);
    }

    public async Task PublishGameStartedAsync(string connectionId, GameStartedEventDto eventDto)
    {
        await _hub.Clients.Client(connectionId)
            .GameStarted(eventDto);
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

    public Task PublishPlayerReadyStatusChangedAsync(PlayerReadyStatusChangedEventDto eventDto)
    {
        return _hub.Clients.Group(eventDto.RoomCode)
            .PlayerReadyStatusChanged(eventDto);
    }

    public Task PublishPlayerChangedAvatarAsync(PlayerChangedAvatarEventDto eventDto)
    {
        return _hub.Clients.Group(eventDto.RoomCode)
            .PlayerChangedAvatar(eventDto);
    }

    public Task PublishHostChangedAsync(HostChangedEventDto eventDto)
    {
        return _hub.Clients.Group(eventDto.RoomCode)
            .HostChanged(eventDto);
    }

    public Task PublishChatMessageAsync(ChatMessageEventDto eventDto)
    {
        return _hub.Clients.Group(eventDto.RoomCode)
            .ChatMessageReceived(eventDto);
    }

    public Task PublishTimerVoteAsync(TimerStoppedEventDto eventDto)
    {
        return _hub.Clients.Group(eventDto.RoomCode)
            .TimerVoteUpdated(eventDto);
    }

    public Task PublishSpiesRevealedAsync(SpiesRevealedEventDto eventDto)
    {
        return _hub.Clients.Group(eventDto.RoomCode)
            .SpiesRevealed(eventDto);
    }

    public Task PublishReturnToLobbyAsync(ReturnToLobbyEventDto eventDto)
    {
        return _hub.Clients.Group(eventDto.RoomCode)
            .ReturnedToLobby(eventDto);
    }
}
