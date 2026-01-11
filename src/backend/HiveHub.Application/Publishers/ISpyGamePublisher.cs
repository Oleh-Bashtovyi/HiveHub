using HiveHub.Application.Dtos.Events;

namespace HiveHub.Application.Publishers;

public interface ISpyGamePublisher
{
    Task AddPlayerToRoomGroupAsync(string connectionId, string roomCode);
    Task RemovePlayerFromRoomGroupAsync(string connectionId, string roomCode);

    Task PublishPlayerJoinedAsync(PlayerJoinedEventDto eventDto);
    Task PublishPlayerLeftAsync(PlayerLeftEventDto eventDto);
    Task PublishPlayerChangedNameAsync(PlayerChangedNameEventDto eventDto);
    Task PublishPlayerKickedAsync(PlayerKickedEventDto eventDto);
    Task PublishPlayerReadyStatusChangedAsync(PlayerReadyStatusChangedEventDto eventDto);
    Task PublishPlayerChangedAvatarAsync(PlayerChangedAvatarEventDto eventDto);

    Task PublishHostChangedAsync(HostChangedEventDto eventDto);
    Task PublishGameSettingsUpdatedAsync(GameSettingsUpdatedEventDto eventDto);

    Task PublishGameStartedAsync(string connectionId, GameStartedEventDto eventDto);
    Task PublishChatMessageAsync(ChatMessageEventDto eventDto);
    Task PublishTimerVoteAsync(TimerStoppedEventDto eventDto);
    Task PublishSpiesRevealedAsync(SpiesRevealedEventDto eventDto);
    Task PublishReturnToLobbyAsync(ReturnToLobbyEventDto eventDto);
    Task PublishPlayerConnectionChangedAsync(PlayerConnectionChangedEventDto eventDto);
}

