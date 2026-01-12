using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
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
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        PlayerRemovalResult removalResult = null!;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            var removalResult = SpyGamePlayerRemover.Remove(room, player.IdInRoom);

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Player {PlayerId} left room {RoomCode}", removalResult.RemovedPlayerId, request.RoomCode);

        await _publisher.RemovePlayerFromRoomGroupAsync(request.ConnectionId, request.RoomCode);

        if (removalResult.ShouldDeleteRoom)
        {
            await _gameManager.RemoveRoomAsync(request.RoomCode);
            _logger.LogInformation("Room {RoomCode} deleted - no players left", request.RoomCode);
        }
        else
        {
            var leftEvent = new PlayerLeftEventDto(request.RoomCode, removalResult.RemovedPlayerId);
            await _publisher.PublishPlayerLeftAsync(leftEvent);

            if (!string.IsNullOrEmpty(removalResult.NewHostId))
            {
                var hostChangedEvent = new HostChangedEventDto(request.RoomCode, removalResult.NewHostId);
                await _publisher.PublishHostChangedAsync(hostChangedEvent);
                _logger.LogInformation("New host {HostId} assigned in room {RoomCode}", removalResult.NewHostId, request.RoomCode);
            }
        }

        return Result.Ok();
    }
}