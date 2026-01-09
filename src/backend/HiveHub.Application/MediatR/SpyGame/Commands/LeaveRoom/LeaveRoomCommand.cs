using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.LeaveRoom;

public record LeaveRoomCommand(
    string RoomCode,
    string ConnectionId
) : IRequest<Result>;

public class LeaveRoomHandler(
    ISpyGameRepository gameManager,
    ISpyGamePublisher publisher,
    ILogger<LeaveRoomHandler> logger)
    : IRequestHandler<LeaveRoomCommand, Result>
{
    private readonly ISpyGameRepository _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<LeaveRoomHandler> _logger = logger;

    public async Task<Result> Handle(LeaveRoomCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        string leavingPlayerId = string.Empty;
        bool wasHost = false;
        string newHostId = string.Empty;
        bool roomShouldBeDeleted = false;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound("Гравця не знайдено в кімнаті.");
            }

            leavingPlayerId = player.IdInRoom;
            wasHost = player.IsHost;

            room.Players.Remove(player);

            if (room.Players.Count == 0)
            {
                roomShouldBeDeleted = true;
                return Result.Ok();
            }

            if (wasHost)
            {
                var newHost = room.Players.FirstOrDefault(x => x.IsConnected);

                if (newHost != null)
                {
                    newHost.IsHost = true;
                    newHostId = newHost.IdInRoom;
                }
            }

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Player {PlayerId} left room {RoomCode}", leavingPlayerId, request.RoomCode);

        await _publisher.RemovePlayerFromRoomGroupAsync(request.ConnectionId, request.RoomCode);

        if (roomShouldBeDeleted)
        {
            await _gameManager.RemoveRoomAsync(request.RoomCode);
            _logger.LogInformation("Room {RoomCode} deleted - no players left", request.RoomCode);
        }
        else
        {
            var leftEvent = new PlayerLeftEventDto(request.RoomCode, leavingPlayerId);
            await _publisher.PublishPlayerLeftAsync(leftEvent);

            if (!string.IsNullOrEmpty(newHostId))
            {
                var hostChangedEvent = new HostChangedEventDto(request.RoomCode, newHostId);
                await _publisher.PublishHostChangedAsync(hostChangedEvent);
                _logger.LogInformation("New host {HostId} assigned in room {RoomCode}", newHostId, request.RoomCode);
            }
        }

        return Result.Ok();
    }
}