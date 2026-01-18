using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace HiveHub.Application.MediatR.SpyGame.SystemCommands.PlayerDisconnect;

public record ProcessPlayerDisconnectCommand(string ConnectionId, string RoomCode) : IRequest<Result>;

public class ProcessPlayerDisconnectHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<ProcessPlayerDisconnectHandler> logger)
    : IRequestHandler<ProcessPlayerDisconnectCommand, Result>
{
    public async Task<Result> Handle(ProcessPlayerDisconnectCommand request, CancellationToken cancellationToken)
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

        var playerId = string.Empty;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player) || !player.IsConnected)
            {
                return Result.Ok();
            }

            player.IsConnected = false;
            playerId = player.IdInRoom;

            context.AddEvent(new PlayerConnectionChangedEventDto(roomCode, playerId, false));

            context.AddEvent(new ScheduleTaskEvent(
                Type: TaskType.SpyGamePlayerDisconnectedTimeout,
                RoomCode: roomCode, 
                TargetId: playerId,
                Delay: TimeSpan.FromSeconds(ProjectConstants.PlayerDisconnectTimeoutSeconds)));

            return Result.Ok();
        });

        if (result.IsSuccess && playerId != string.Empty)
        {
            logger.LogInformation("Room [{RoomCode}]: Player {PlayerId} marked as disconnected", playerId, roomCode);
        }

        return Result.Ok();
    }
}