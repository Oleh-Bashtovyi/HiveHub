using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Extensions;
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
    SpyGameEventsContext context,
    ILogger<RenamePlayerHandler> logger)
    : IRequestHandler<RenamePlayerCommand, Result>
{
    public async Task<Result> Handle(RenamePlayerCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var publicId = string.Empty;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
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

            context.AddEvent(new PlayerChangedNameEventDto(request.RoomCode, publicId, request.NewName));

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: Player {PlayerId} renamed to {NewName}",
                request.RoomCode,
                publicId,
                request.NewName);
        }

        return result;
    }
}
