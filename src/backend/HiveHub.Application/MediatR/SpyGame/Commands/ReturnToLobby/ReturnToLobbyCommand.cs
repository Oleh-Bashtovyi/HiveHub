using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.ReturnToLobby;

public record ReturnToLobbyCommand(
    string RoomCode,
    string HostConnectionId
) : IRequest<Result>;

public class ReturnToLobbyHandler(
    SpyGameManager gameManager,
    ISpyGamePublisher publisher,
    ILogger<ReturnToLobbyHandler> logger)
    : IRequestHandler<ReturnToLobbyCommand, Result>
{
    private readonly SpyGameManager _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<ReturnToLobbyHandler> _logger = logger;

    public async Task<Result> Handle(ReturnToLobbyCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Ended)
            {
                return Results.ActionFailed("Гра ще не завершена.");
            }

            if (!room.Players.TryGetValue(request.HostConnectionId, out var host) || !host.IsHost)
            {
                return Results.ActionFailed("Тільки хост може повернутися в лобі.");
            }

            room.State = RoomState.Lobby;
            room.CurrentSecretWord = null;
            room.GameStartTime = null;
            room.IsTimerStopped = false;
            room.TimerStoppedAt = null;
            room.ChatMessages.Clear();

            foreach (var player in room.Players.Values)
            {
                player.IsReady = false;
                player.PlayerState.IsSpy = false;
                player.PlayerState.VotedToStopTimer = false;
            }

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Room {RoomCode} returned to lobby", request.RoomCode);

        var eventDto = new ReturnToLobbyEventDto(request.RoomCode);
        await _publisher.PublishReturnToLobbyAsync(eventDto);

        return Result.Ok();
    }
}