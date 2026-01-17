using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;

namespace HiveHub.Application.Publishers;

public interface IBaseEventsPublisher
{
    // Room grouping
    Task AddPlayerToRoomGroupAsync(string connectionId, string roomCode);
    Task RemovePlayerFromRoomGroupAsync(string connectionId, string roomCode);

    // Connection
    Task PublishPlayerLeftAsync(PlayerLeftEventDto eventDto);
    Task PublishPlayerKickedAsync(PlayerKickedEventDto eventDto);
    Task PublishReturnToLobbyAsync(ReturnToLobbyEventDto eventDto);

    // Lobby
    Task PublishPlayerChangedNameAsync(PlayerChangedNameEventDto eventDto);
    Task PublishPlayerChangedAvatarAsync(PlayerChangedAvatarEventDto eventDto);
    Task PublishPlayerReadyStatusChangedAsync(PlayerReadyStatusChangedEventDto eventDto);

    // General
    Task PublishHostChangedAsync(HostChangedEventDto eventDto);
    Task PublishPlayerConnectionChangedAsync(PlayerConnectionChangedEventDto eventDto);
}

public interface ISpyGamePublisher : IBaseEventsPublisher
{
    // Connection
    Task PublishPlayerJoinedAsync(PlayerJoinedEventDto<SpyPlayerDto> eventDto);

    // Lobby
    Task PublishGameStartedAsync(string connectionId, SpyGameStartedEventDto eventDto);
    Task PublishGameRulesUpdatedAsync(SpyGameRulesUpdatedEventDto eventDto);
    Task PublishWordPacksUpdatedAsync(SpyGameWordPacksUpdatedEventDto eventDto);

    // Gameplay
    Task PublishTimerVoteAsync(PlayerVotedToStopTimerEventDto eventDto);

    Task PublishGameEndedAsync(SpyGameEndedEventDto eventDto);
    Task PublishGamePhaseChangedAsync(GamePhaseChangedEventDto eventDto);
    Task PublishPlayerEliminatedAsync(PlayerEliminatedEventDto eventDto);
    Task PublishSpyRevealedAsync(SpyRevealedEventDto eventDto);
    Task PublishSpyLastChanceStartedAsync(SpyLastChanceStartedEventDto eventDto);
    Task PublishSpyGuessAttemptedAsync(SpyGuessAttemptedEventDto eventDto);
    Task PublishVotingStartedAsync(VotingStartedEventDto eventDto);
    Task PublishVoteCastAsync(VoteCastEventDto eventDto);
    Task PublishVotingCompletedAsync(VotingCompletedEventDto eventDto);
    Task PublishTimerStateChangedAsync(SpyGameRoundTimerStateChangedEventDto eventDto);

    // General
    Task PublishChatMessageAsync(ChatMessageEventDto eventDto);
}