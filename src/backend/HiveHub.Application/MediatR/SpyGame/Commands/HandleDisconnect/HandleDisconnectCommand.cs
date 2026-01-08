using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.HandleDisconnect;

public record HandleDisconnectCommand(string ConnectionId) : IRequest<Result>;

public class HandleDisconnectHandler(
    ISpyGameRepository gameManager,
    IConnectionMappingService mappingService,
    ISpyGamePublisher publisher,
    ILogger<HandleDisconnectHandler> logger)
    : IRequestHandler<HandleDisconnectCommand, Result>
{
    public async Task<Result> Handle(HandleDisconnectCommand request, CancellationToken cancellationToken)
    {
        var roomCode = mappingService.GetRoomCode(request.ConnectionId);

        if (string.IsNullOrEmpty(roomCode))
        {
            return Result.Ok();
        }

        mappingService.Unmap(request.ConnectionId);

        var roomAccessor = gameManager.GetRoom(roomCode);
        if (roomAccessor == null) return Result.Ok();

        await roomAccessor.ExecuteAsync(async (room) =>
        {
            if (!room.Players.TryGetValue(request.ConnectionId, out var player))
            {
                return;
            }

            player.IsConnected = false;

            logger.LogInformation("Player {PlayerId} disconnected from room {RoomCode}", player.IdInRoom, roomCode);

            if (player.IsHost)
            {
                var newHostPair = room.Players.FirstOrDefault(p => p.Key != request.ConnectionId && p.Value.IsConnected);

                if (newHostPair.Value == null && !room.Players.IsEmpty)
                {
                    newHostPair = room.Players.FirstOrDefault(p => p.Key != request.ConnectionId);
                }

                if (newHostPair.Value != null)
                {
                    player.IsHost = false;
                    newHostPair.Value.IsHost = true;

                    var hostDto = new HostChangedEventDto(roomCode, newHostPair.Value.IdInRoom);
                    await publisher.PublishHostChangedAsync(hostDto);

                    logger.LogInformation("Host auto-assigned to {NewHostId} due to disconnect", newHostPair.Value.IdInRoom);
                }
            }
        });

        return Result.Ok();
    }
}