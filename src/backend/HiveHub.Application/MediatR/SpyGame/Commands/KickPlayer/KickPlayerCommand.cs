using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.KickPlayer;

public record KickPlayerCommand(
    string RoomCode,
    string HostConnectionId,
    string TargetPlayerId
) : IRequest<Result>;

public class KickPlayerHandler(
    SpyGameManager gameManager,
    ISpyGamePublisher publisher,
    ILogger<KickPlayerHandler> logger)
    : IRequestHandler<KickPlayerCommand, Result>
{
    private readonly SpyGameManager _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<KickPlayerHandler> _logger = logger;

    public async Task<Result> Handle(KickPlayerCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        string kickedPlayerConnectionId = string.Empty;
        string kickedPlayerName = string.Empty;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed("Не можна виганяти гравців під час гри.");
            }

            if (!room.Players.TryGetValue(request.HostConnectionId, out var host) || !host.IsHost)
            {
                return Results.ActionFailed("Тільки хост може виганяти гравців.");
            }

            var targetPair = room.Players.FirstOrDefault(p => p.Value.IdInRoom == request.TargetPlayerId);

            if (targetPair.Value == null)
            {
                return Results.NotFound("Гравця не знайдено.");
            }

            if (targetPair.Value.IsHost)
            {
                return Results.ActionFailed("Хост не може вигнати сам себе.");
            }

            kickedPlayerConnectionId = targetPair.Key;
            kickedPlayerName = targetPair.Value.Name;

            if (!room.Players.TryRemove(targetPair.Key, out _))
            {
                return Results.ActionFailed("Помилка при видаленні гравця.");
            }

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Player {PlayerId} ({PlayerName}) was kicked from room {RoomCode} by {HostConnectionId}",
            request.TargetPlayerId, 
            kickedPlayerName, 
            request.RoomCode, 
            request.HostConnectionId);

        var kickEvent = new PlayerKickedEventDto(
            RoomCode: request.RoomCode,
            PlayerId: request.TargetPlayerId,
            KickedByPlayerId: "HOST"
        );

        await _publisher.PublishPlayerKickedAsync(kickEvent);

        if (!string.IsNullOrEmpty(kickedPlayerConnectionId))
        {
            await _publisher.RemovePlayerFromRoomGroupAsync(kickedPlayerConnectionId, request.RoomCode);
        }

        return Result.Ok();
    }
}

