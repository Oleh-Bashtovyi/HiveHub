using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Models;
using HiveHub.Application.Services;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.Publishers;

public abstract class BaseEventsContext(IBaseEventsPublisher publisher, ITaskScheduler scheduler, ILogger logger)
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
            // Base handling
            switch (ev)
            {
                // Group managment
                case AddPlayerToGroupEvent e:
                    await publisher.AddPlayerToRoomGroupAsync(e.ConnectionId, e.RoomCode);
                    break;

                case RemovePlayerFromGroupEvent e:
                    await publisher.RemovePlayerFromRoomGroupAsync(e.ConnectionId, e.RoomCode);
                    break;

                // Connection
                case PlayerLeftEventDto e:
                    await publisher.PublishPlayerLeftAsync(e);
                    break;

                // Lobby
                case PlayerChangedNameEventDto e:
                    await publisher.PublishPlayerChangedNameAsync(e);
                    break;

                case PlayerChangedAvatarEventDto e:
                    await publisher.PublishPlayerChangedAvatarAsync(e);
                    break;

                // General
                case PlayerKickedEventDto e:
                    await publisher.PublishPlayerKickedAsync(e);
                    break;

                case PlayerConnectionChangedEventDto e:
                    await publisher.PublishPlayerConnectionChangedAsync(e);
                    break;

                case PlayerReadyStatusChangedEventDto e:
                    await publisher.PublishPlayerReadyStatusChangedAsync(e);
                    break;

                case HostChangedEventDto e:
                    await publisher.PublishHostChangedAsync(e);
                    break;

                case ReturnToLobbyEventDto e:
                    await publisher.PublishReturnToLobbyAsync(e);
                    break;

                // Task scheduling
                case ScheduleTaskEvent e:
                    await scheduler.ScheduleAsync(new ScheduledTask(e.Type, e.RoomCode, e.TargetId), e.Delay);
                    break;

                case CancelTaskEvent e:
                    await scheduler.CancelAsync(new ScheduledTask(e.Type, e.RoomCode, e.TargetId));
                    break;

                // Use overriden events handler
                default:
                    var handled = await HandleEvent(ev);
                    if (!handled)
                        throw new Exception($"Unknoen event type in {GetType().Name}: {ev.GetType().Name}");
                    break;
            }
        }
        _pendingEvents.Clear();
    }

    public abstract Task<bool> HandleEvent(IRoomEvent roomEvent);
}
