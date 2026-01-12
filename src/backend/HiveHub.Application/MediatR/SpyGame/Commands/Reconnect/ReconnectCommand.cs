using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.Reconnect;

public record ReconnectCommand(
    string RoomCode,
    string OldPlayerId,
    string NewConnectionId
) : IRequest<Result<RoomStateDto>>;

public class ReconnectHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
    ILogger<ReconnectHandler> logger)
    : IRequestHandler<ReconnectCommand, Result<RoomStateDto>>
{
    public async Task<Result<RoomStateDto>> Handle(ReconnectCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = repository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string? oldConnectionId = null;

        var result = await roomAccessor.ExecuteAsync(async (room) =>
        {
            var player = room.Players.FirstOrDefault(x => x.IdInRoom == request.OldPlayerId);
            if (player == null)
            {
                return Results.NotFound<RoomStateDto>(ProjectMessages.PlayerNotFound);
            }

            var task = new ScheduledTask(TaskType.SpyPlayerDisconnectTimeout, request.RoomCode, request.OldPlayerId);
            await scheduler.CancelAsync(task);

            oldConnectionId = player.ConnectionId;
            player.ConnectionId = request.NewConnectionId;
            player.IsConnected = true;

            var state = SpyGameStateMapper.GetRoomStateForPlayer(room, player.IdInRoom);

            return state;
        });

        if (result.IsFailed) return result;

        if (!string.IsNullOrEmpty(oldConnectionId))
        {
            await publisher.RemovePlayerFromRoomGroupAsync(oldConnectionId, request.RoomCode);
        }

        await publisher.AddPlayerToRoomGroupAsync(request.NewConnectionId, request.RoomCode);

        logger.LogInformation("Reconnect: {Old} -> {New} in {Room}",
            oldConnectionId, request.NewConnectionId, request.RoomCode);

        var connectionEvent = new PlayerConnectionChangedEventDto(request.RoomCode, request.OldPlayerId, true);
        await publisher.PublishPlayerConnectionChangedAsync(connectionEvent);

        return result;
    }
}