using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Extensions;
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
    ISpyGameRepository spyRepository,
    ISpyGamePublisher publisher,
    ILogger<ChangeHostHandler> logger)
    : IRequestHandler<ChangeHostCommand, Result>
{
    private readonly ISpyGameRepository _spyRepository = spyRepository;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<ChangeHostHandler> _logger = logger;

    public async Task<Result> Handle(ChangeHostCommand request, CancellationToken cancellationToken)
    {
        if (!_spyRepository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string newHostId = string.Empty;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed(ProjectMessages.ChangeHost.CanNotChangeHostMidGame);
            }

            if (!room.TryGetPlayerByConnectionId(request.CurrentHostConnectionId, out var currentHost) || !currentHost.IsHost)
            {
                return Results.Forbidden(ProjectMessages.ChangeHost.OnlyHostCanChangePermission);
            }

            if (!room.TryGetPlayerByIdInRoom(request.NewHostPlayerId, out var newHost))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            currentHost.IsHost = false;
            newHost.IsHost = true;
            newHostId = newHost.IdInRoom;

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