using HiveHub.Application.Dtos.Events;

namespace HiveHub.API.Hubs;

public interface ISpyGameClient
{
    Task PlayerJoined(PlayerJoinedEventDto eventDto);
    Task PlayerLeft(PlayerLeftEventDto eventDto);
    Task PlayerChangedName(PlayerChangedNameEventDto eventDto);
    Task PlayerKicked(PlayerKickedEventDto eventDto);
    Task PlayerReadyStatusChanged(PlayerReadyStatusChangedEventDto eventDto);
    Task PlayerChangedAvatar(PlayerChangedAvatarEventDto eventDto);
    Task HostChanged(HostChangedEventDto eventDto);
    Task GameSettingsUpdated(GameSettingsUpdatedEventDto eventDto);
    Task GameStarted(GameStartedEventDto eventDto);
    Task ChatMessageReceived(ChatMessageEventDto eventDto);
    Task TimerVoteUpdated(TimerStoppedEventDto eventDto);
    Task SpiesRevealed(SpiesRevealedEventDto eventDto);
    Task ReturnedToLobby(ReturnToLobbyEventDto eventDto);
    Task PlayerConnectionStatusChanged(PlayerConnectionChangedEventDto eventDto);
}
