using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.ChangeHost;

public record ChangeHostCommand(
    string RoomCode,
    string CurrentHostConnectionId,
    string NewHostPlayerId
) : IRequest<Result>;

public class ChangeHostHandler(
    SpyGameManager gameManager,
    ISpyGamePublisher publisher,
    ILogger<ChangeHostHandler> logger)
    : IRequestHandler<ChangeHostCommand, Result>
{
    private readonly SpyGameManager _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<ChangeHostHandler> _logger = logger;

    public async Task<Result> Handle(ChangeHostCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        string newHostId = string.Empty;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed("Не можна змінювати хоста під час гри.");
            }

            if (!room.Players.TryGetValue(request.CurrentHostConnectionId, out var currentHost) || !currentHost.IsHost)
            {
                return Results.ActionFailed("Тільки хост може передавати права.");
            }

            var newHostPair = room.Players.FirstOrDefault(p => p.Value.IdInRoom == request.NewHostPlayerId);
            if (newHostPair.Value == null)
            {
                return Results.NotFound("Нового хоста не знайдено.");
            }

            currentHost.IsHost = false;
            newHostPair.Value.IsHost = true;
            newHostId = newHostPair.Value.IdInRoom;

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Host changed to {NewHostId} in room {RoomCode}", newHostId, request.RoomCode);

        var eventDto = new HostChangedEventDto(request.RoomCode, newHostId);
        await _publisher.PublishHostChangedAsync(eventDto);

        return Result.Ok();
    }
}