using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.ChangeAvatar;

public record ChangeAvatarCommand(
    string RoomCode,
    string ConnectionId,
    string NewAvatarId
) : IRequest<Result>;

public class ChangeAvatarHandler(
    ISpyGameRepository gameManager,
    ISpyGamePublisher publisher,
    ILogger<ChangeAvatarHandler> logger)
    : IRequestHandler<ChangeAvatarCommand, Result>
{
    private readonly ISpyGameRepository _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<ChangeAvatarHandler> _logger = logger;

    public async Task<Result> Handle(ChangeAvatarCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        string playerId = string.Empty;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed("Не можна змінювати аватар під час гри.");
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound("Гравця не знайдено.");
            }

            if (string.IsNullOrWhiteSpace(request.NewAvatarId))
            {
                return Results.ActionFailed("Некоректний ідентифікатор аватара.");
            }

            player.AvatarId = request.NewAvatarId;
            playerId = player.IdInRoom;

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Player {PlayerId} changed avatar to {AvatarId} in room {RoomCode}",
            playerId, request.NewAvatarId, request.RoomCode);

        var eventDto = new PlayerChangedAvatarEventDto(request.RoomCode, playerId, request.NewAvatarId);
        await _publisher.PublishPlayerChangedAvatarAsync(eventDto);

        return Result.Ok();
    }
}