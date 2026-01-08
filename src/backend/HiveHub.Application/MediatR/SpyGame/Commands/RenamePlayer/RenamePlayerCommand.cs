using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.RenamePlayer;

public record RenamePlayerCommand(
    string RoomCode,
    string ConnectionId,
    string NewName
) : IRequest<Result>;

public class RenamePlayerHandler(
    ISpyGameRepository gameManager,
    ISpyGamePublisher publisher,
    ILogger<RenamePlayerHandler> logger)
    : IRequestHandler<RenamePlayerCommand, Result>
{
    private readonly ISpyGameRepository _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<RenamePlayerHandler> _logger = logger;

    public async Task<Result> Handle(RenamePlayerCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        string publicId = string.Empty;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed("Не можна змінювати ім'я під час гри.");
            }

            if (!room.Players.TryGetValue(request.ConnectionId, out var player))
            {
                return Results.NotFound("Гравця не знайдено.");
            }

            if (string.IsNullOrWhiteSpace(request.NewName) || request.NewName.Length > 20)
            {
                return Results.ActionFailed("Некоректне ім'я.");
            }

            player.Name = request.NewName;
            publicId = player.IdInRoom;

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Player {PlayerId} renamed to {NewName} in room {RoomCode}", 
            publicId, 
            request.NewName, 
            request.RoomCode);

        var eventDto = new PlayerChangedNameEventDto(request.RoomCode, publicId, request.NewName);
        
        await _publisher.PublishPlayerChangedNameAsync(eventDto);

        return Result.Ok();
    }
}
