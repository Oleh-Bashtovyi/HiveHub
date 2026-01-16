using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Models;
using HiveHub.Application.Services;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.Publishers;

public class SpyGameEventsContext(
    ISpyGamePublisher publisher,
    ILogger<SpyGameEventsContext> logger,
    ITaskScheduler scheduler)
{
    private readonly List<IRoomEvent> _pendingEvents = new();

    public bool HasEvents => _pendingEvents.Count > 0;

    public void Clear() => _pendingEvents.Clear();

    public void AddEvent(IRoomEvent roomEvent)
    {
        _pendingEvents.Add(roomEvent);
    }

    public void AddRange(IEnumerable<IRoomEvent> events)
    {
        _pendingEvents.AddRange(events);
    }

    public async Task DispatchAsync()
    {
        foreach (var ev in _pendingEvents)
        {
            await HandleEvent(ev);
        }
        _pendingEvents.Clear();
    }

    public async Task HandleEvent(IRoomEvent roomEvent)
    {
        switch (roomEvent)
        {
            case ScheduleTaskEvent e:
                await scheduler.ScheduleAsync(new ScheduledTask(e.Type, e.RoomCode, e.TargetId), e.Delay);
                break;

            case CancelTaskEvent e:
                await scheduler.CancelAsync(new ScheduledTask(e.Type, e.RoomCode, e.TargetId));
                break;

            // --- Room Grouping (SignalR Groups) ---
            case AddPlayerToGroupEvent e:
                await publisher.AddPlayerToRoomGroupAsync(e.ConnectionId, e.RoomCode);
                break;

            case RemovePlayerFromGroupEvent e:
                await publisher.RemovePlayerFromRoomGroupAsync(e.ConnectionId, e.RoomCode);
                break;

            // --- Connection ---
            case PlayerJoinedEventDto<SpyPlayerDto> e:
                await publisher.PublishPlayerJoinedAsync(e);
/*                logger.LogDebug("Publishing: Player {PlayerId} joined room {RoomId}", 
                    e.Player.Id, e.RoomCode);*/
                break;

            case PlayerLeftEventDto e:
                await publisher.PublishPlayerLeftAsync(e);
/*                logger.LogDebug("Publishing: Player {PlayerId} left room {RoomId}", 
                    e.PlayerId, e.RoomCode);*/
                break;

            case PlayerKickedEventDto e:
                await publisher.PublishPlayerKickedAsync(e);
/*                logger.LogDebug("Publishing: Player {PlayerId} was kicked from room {RoomId}", 
                    e.PlayerId, e.RoomCode);*/
                break;

            case PlayerConnectionChangedEventDto e:
                await publisher.PublishPlayerConnectionChangedAsync(e);
/*                logger.LogDebug("Publishing: Player {PlayerId} changed connection to {IsConnected} in room {RoomId}", 
                    e.PlayerId, e.IsConnected, e.RoomCode);*/
                break;

            // --- Lobby ---
            case PlayerChangedNameEventDto e:
                await publisher.PublishPlayerChangedNameAsync(e);
                break;

            case PlayerChangedAvatarEventDto e:
                await publisher.PublishPlayerChangedAvatarAsync(e);
                break;

            case PlayerReadyStatusChangedEventDto e:
                await publisher.PublishPlayerReadyStatusChangedAsync(e);
                break;

            case SpyGameWordPacksUpdatedEvent e:
                await publisher.PublishWordPacksUpdatedAsync(e);
                break;

            case SpyGameRulesUpdatedEventDto e:
                await publisher.PublishGameRulesUpdatedAsync(e);
                break;

            // --- Game Started (Special case: Targeted message) ---
            case TargetedGameStartedEvent e:
                await publisher.PublishGameStartedAsync(e.ConnectionId, e.Payload);
                break;

            // --- Gameplay ---
            case PlayerVotedToStopTimerEventDto e:
                await publisher.PublishTimerVoteAsync(e);
                break;

            case ReturnToLobbyEventDto e:
                await publisher.PublishReturnToLobbyAsync(e);
/*                logger.LogCritical("Published: Return to lobby in room {RoomCode}", 
                    e.RoomCode);*/
                break;

            case SpyGameEndedEventDto e:
                await publisher.PublishGameEndedAsync(e);
/*                logger.LogCritical("Publish: game ended in room {RoomCode}, reason: {Reason}", 
                    e.RoomCode, e.Reason);*/
                break;

            case VotingStartedEventDto e:
                await publisher.PublishVotingStartedAsync(e);
/*                logger.LogCritical("Published: Voting started in room {RoomCode}, initiator {InitiatorId}, type {VoteType}",
                    e.RoomCode, e.InitiatorId, e.VotingType);*/
                break;

            case VoteCastEventDto e:
                await publisher.PublishVoteCastAsync(e);
/*                logger.LogCritical("Published: Vote cast in room {RoomCode}, voter {VoterId}", 
                    e.RoomCode, e.VoterId);*/
                break;

            case VotingResultEventDto e:
                await publisher.PublishVotingResultAsync(e);
/*                logger.LogCritical("Published: Voting result in room {RoomCode}, is success: {IsSuccess}, result message: {ResultMessage}", 
                    e.RoomCode, e.IsSuccess, e.ResultMessage);*/
                break;

            case ChatMessageEventDto e:
                await publisher.PublishChatMessageAsync(e);
                break;

            case HostChangedEventDto e:
                await publisher.PublishHostChangedAsync(e);
                break;

            case SpyGameRoundTimerStateChangedEventDto e:
                await publisher.PublishTimerStateChangedAsync(e);
/*                logger.LogCritical("Published: Round timer status changed to: {IsTimerStopped} in room {RoomCode}",
                    e.IsRoundTimerStopped, e.RoomCode);*/
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(roomEvent), roomEvent, "Unknown event type in SpyGameEventsContext");
        }
    }
}
