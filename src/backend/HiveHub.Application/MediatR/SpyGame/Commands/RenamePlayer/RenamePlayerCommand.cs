using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.RenamePlayer;

public record RenamePlayerCommand(
    string RoomCode,
    string ConnectionId,
    string NewName
) : IRequest<Result>;

public class RenamePlayerHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ILogger<RenamePlayerHandler> logger)
    : IRequestHandler<RenamePlayerCommand, Result>
{
    public async Task<Result> Handle(RenamePlayerCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = repository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string publicId = string.Empty;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (!room.IsInLobby())
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

        logger.LogInformation("Player {PlayerId} renamed to {NewName} in room {RoomCode}", 
            publicId, 
            request.NewName, 
            request.RoomCode);

        var eventDto = new PlayerChangedNameEventDto(request.RoomCode, publicId, request.NewName);
        
        await publisher.PublishPlayerChangedNameAsync(eventDto);

        return Result.Ok();
    }
}
