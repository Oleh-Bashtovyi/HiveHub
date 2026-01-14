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

namespace HiveHub.Application.MediatR.SpyGame.Commands.HandleDisconnect;

public record HandleDisconnectCommand(string ConnectionId, string RoomCode) : IRequest<Result>;

public class HandleDisconnectHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
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

        var playerId = string.Empty;
        var wasConnected = false;

        await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Result.Ok();
            }

            playerId = player.IdInRoom;
            wasConnected = player.IsConnected;
            player.IsConnected = false;

            logger.LogInformation("Player {PlayerId} disconnected from room {RoomCode}", player.IdInRoom, roomCode);

            context.AddEvent(new PlayerConnectionChangedEventDto(roomCode, playerId, false));

            context.AddEvent(new ScheduleTaskEvent(
                TaskType.SpyGamePlayerDisconnectedTimeout, 
                roomCode, playerId, 
                TimeSpan.FromSeconds(ProjectConstants.PlayerDisconnectTimeoutSeconds)));

            SpyGameLogicHelper.CheckAndResolveVoting(room, context, repository, logger);
            SpyGameLogicHelper.CheckAndResolveTimerStop(room, context, logger);

            return Result.Ok();
        });

        return Result.Ok();
    }
}