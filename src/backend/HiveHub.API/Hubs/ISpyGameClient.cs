using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;

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
    Task RulesChanged(SpyGameRulesUpdatedEventDto eventDto);
    Task WordPacksChanged(SpyGameWordPacksUpdatedEventDto eventDto);
    Task GameStarted(SpyGameStartedEventDto eventDto);
    Task ChatMessageReceived(ChatMessageEventDto eventDto);
    Task ReturnedToLobby(ReturnToLobbyEventDto eventDto);
    Task GameEnded(SpyGameEndedEventDto eventDto);
    Task PlayerConnectionStatusChanged(PlayerConnectionChangedEventDto eventDto);
    Task VotingStarted(VotingStartedEventDto eventDto);
    Task VoteCast(VoteCastEventDto eventDto);
    Task VotingCompleted(VotingCompletedEventDto eventDto);
    Task TimerVoteUpdated(PlayerVotedToStopTimerEventDto eventDto);
    Task RoundTimerStateChanged(SpyGameRoundTimerStateChangedEventDto eventDto);
    Task GamePhaseChanged(GamePhaseChangedEventDto eventDto);
    Task PlayerEliminated(PlayerEliminatedEventDto eventDto);
    Task SpyRevealed(SpyRevealedEventDto eventDto);
    Task SpyLastChanceStarted(SpyLastChanceStartedEventDto eventDto);
    Task SpyGuessAttempted(SpyGuessAttemptedEventDto eventDto);
}