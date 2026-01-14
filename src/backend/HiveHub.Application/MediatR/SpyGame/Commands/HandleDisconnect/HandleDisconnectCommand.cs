using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.HandleDisconnect;

public record HandleDisconnectCommand(string ConnectionId, string RoomCode) : IRequest<Result>;

public class HandleDisconnectHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
    ILogger<HandleDisconnectHandler> logger)
    : IRequestHandler<HandleDisconnectCommand, Result>
{
    public async Task<Result> Handle(HandleDisconnectCommand request, CancellationToken cancellationToken)
    {
        var roomCode = request.RoomCode;

        if (string.IsNullOrEmpty(roomCode))
        {
            return Result.Ok();
        }

        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string? playerId = null;
        bool wasConnected = false;

        await roomAccessor.ExecuteAsync(async (room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return;
            }

            playerId = player.IdInRoom;
            wasConnected = player.IsConnected;
            player.IsConnected = false;

            logger.LogInformation("Player {PlayerId} disconnected from room {RoomCode}", player.IdInRoom, roomCode);

            var task = new ScheduledTask(TaskType.SpyPlayerDisconnectTimeout, roomCode, playerId);
            await scheduler.ScheduleAsync(task, TimeSpan.FromSeconds(30));
        });

        if (!string.IsNullOrEmpty(playerId) && wasConnected)
        {
            var eventDto = new PlayerConnectionChangedEventDto(roomCode, playerId, false);
            await publisher.PublishPlayerConnectionChangedAsync(eventDto);
        }

        return Result.Ok();
    }
}