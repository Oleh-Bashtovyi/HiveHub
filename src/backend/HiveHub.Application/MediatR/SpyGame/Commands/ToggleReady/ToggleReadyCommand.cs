using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.ToggleReady;

public record ToggleReadyCommand(
    string RoomCode,
    string ConnectionId
) : IRequest<Result>;

public class ToggleReadyHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ILogger<ToggleReadyHandler> logger)
    : IRequestHandler<ToggleReadyCommand, Result>
{
    private readonly ISpyGameRepository _repository = repository;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<ToggleReadyHandler> _logger = logger;

    public async Task<Result> Handle(ToggleReadyCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _repository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string playerId = string.Empty;
        bool newReadyStatus = false;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed(ProjectMessages.ToggleReady.CanNotReadyStatusMidGame);
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            player.IsReady = !player.IsReady;
            playerId = player.IdInRoom;
            newReadyStatus = player.IsReady;

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Player {PlayerId} ready status changed to {IsReady} in room {RoomCode}",
            playerId, newReadyStatus, request.RoomCode);

        var eventDto = new PlayerReadyStatusChangedEventDto(request.RoomCode, playerId, newReadyStatus);
        await _publisher.PublishPlayerReadyStatusChangedAsync(eventDto);

        return Result.Ok();
    }
}