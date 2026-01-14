using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;

namespace HiveHub.Application.Publishers;

public interface ISpyGamePublisher
{
    // Room grouping
    Task AddPlayerToRoomGroupAsync(string connectionId, string roomCode);
    Task RemovePlayerFromRoomGroupAsync(string connectionId, string roomCode);

    // Connection
    Task PublishPlayerJoinedAsync(PlayerJoinedEventDto<SpyPlayerDto> eventDto);
    Task PublishPlayerLeftAsync(PlayerLeftEventDto eventDto);
    Task PublishPlayerKickedAsync(PlayerKickedEventDto eventDto);
    
    // Lobby
    Task PublishPlayerChangedNameAsync(PlayerChangedNameEventDto eventDto);
    Task PublishPlayerChangedAvatarAsync(PlayerChangedAvatarEventDto eventDto);
    Task PublishPlayerReadyStatusChangedAsync(PlayerReadyStatusChangedEventDto eventDto);
    Task PublishGameStartedAsync(string connectionId, GameStartedEventDto eventDto);
    Task PublishGameSettingsUpdatedAsync(SpyGameSettingsUpdatedEventDto eventDto);

    // Gameplay
    Task PublishTimerVoteAsync(TimerStoppedEventDto eventDto);
    Task PublishReturnToLobbyAsync(ReturnToLobbyEventDto eventDto);
    Task PublishGameEndedAsync(GameEndedEventDto eventDto);
    Task PublishVotingStartedAsync(VotingStartedEventDto eventDto);
    Task PublishVoteCastAsync(VoteCastEventDto eventDto);
    Task PublishVotingResultAsync(VotingResultEventDto eventDto);

    // General
    Task PublishChatMessageAsync(ChatMessageEventDto eventDto);
    Task PublishHostChangedAsync(HostChangedEventDto eventDto);
    Task PublishPlayerConnectionChangedAsync(PlayerConnectionChangedEventDto eventDto);
}

