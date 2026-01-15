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
    Task GameStarted(SpyGameStartedEventDto eventDto);
    Task ChatMessageReceived(ChatMessageEventDto eventDto);
    Task TimerVoteUpdated(PlayerVotedToStopTimerEventDto eventDto);
    Task ReturnedToLobby(ReturnToLobbyEventDto eventDto);
    Task GameEnded(SpyGameEndedEventDto eventDto);
    Task PlayerConnectionStatusChanged(PlayerConnectionChangedEventDto eventDto);
    Task VotingStarted(VotingStartedEventDto eventDto);
    Task VoteCast(VoteCastEventDto eventDto);
    Task VotingResult(VotingResultEventDto eventDto);
    Task RoundTimerStateChanged(SpyGameRoundTimerStateChangedEventDto eventDto);
}
