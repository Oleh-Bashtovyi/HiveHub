using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using Microsoft.AspNetCore.SignalR;

namespace HiveHub.API.Hubs;

public interface ISpyGameClient
{
    Task PlayerJoined(PlayerJoinedEventDto<SpyPlayerDto> eventDto);
    Task PlayerLeft(PlayerLeftEventDto eventDto);
    Task PlayerChangedName(PlayerChangedNameEventDto eventDto);
    Task PlayerKicked(PlayerKickedEventDto eventDto);
    Task PlayerReadyStatusChanged(PlayerReadyStatusChangedEventDto eventDto);
    Task PlayerChangedAvatar(PlayerChangedAvatarEventDto eventDto);
    Task HostChanged(HostChangedEventDto eventDto);
    Task GameSettingsUpdated(SpyGameSettingsUpdatedEventDto eventDto);
    Task GameStarted(GameStartedEventDto eventDto);
    Task ChatMessageReceived(ChatMessageEventDto eventDto);
    Task TimerVoteUpdated(TimerStoppedEventDto eventDto);
    Task ReturnedToLobby(ReturnToLobbyEventDto eventDto);
    Task GameEnded(GameEndedEventDto eventDto);
    Task PlayerConnectionStatusChanged(PlayerConnectionChangedEventDto eventDto);
    Task VotingStarted(VotingStartedEventDto eventDto);
    Task VoteCast(VoteCastEventDto eventDto);
    Task VotingResult(VotingResultEventDto eventDto);
}
