using FluentResults;
using HiveHub.Application.Constants;
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
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string publicId = string.Empty;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed(ProjectMessages.Rename.CanNotChangeGameMidGame);
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            if (string.IsNullOrWhiteSpace(request.NewName) || request.NewName.Length > ProjectConstants.PlayerNameMaxLength)
            {
                return Results.ValidationFailed(ProjectMessages.Rename.PlayerNameMustHaveLength);
            }

            if (room.Players.Any(p => p.ConnectionId != request.ConnectionId &&
                                  p.Name.Equals(request.NewName, StringComparison.OrdinalIgnoreCase)))
            {
                return Results.ActionFailed(ProjectMessages.Rename.PlayerWithThisNameAlreadyExistsInRoom);
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
