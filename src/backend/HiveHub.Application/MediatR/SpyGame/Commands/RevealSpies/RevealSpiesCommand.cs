using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.RevealSpies;

public record RevealSpiesCommand(
    string RoomCode,
    string ConnectionId
) : IRequest<Result>;

public class RevealSpiesHandler(
    ISpyGameRepository repostiroty,
    ISpyGamePublisher publisher,
    ILogger<RevealSpiesHandler> logger)
    : IRequestHandler<RevealSpiesCommand, Result>
{
    private readonly ISpyGameRepository _repository = repostiroty;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<RevealSpiesHandler> _logger = logger;

    public async Task<Result> Handle(RevealSpiesCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _repository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        List<SpyRevealDto> spies = new();

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.InGame)
            {
                return Results.ActionFailed(ProjectMessages.SpyGameRevealSpies.RevealSpiesCanBeDoneOnlyMidGame);
            }

            if (!room.TimerState.IsTimerStopped)
            {
                return Results.ActionFailed(ProjectMessages.SpyGameRevealSpies.TimerMustBeStoppedToRevealSpies);
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            if (!player.IsHost)
            {
                return Results.Forbidden(ProjectMessages.SpyGameRevealSpies.OnlyHostCanRevealSpies);
            }

            spies = room.Players
                .Where(p => p.PlayerState.IsSpy)
                .Select(p => new SpyRevealDto(p.IdInRoom, p.Name))
                .ToList();

            room.State = RoomState.Ended;

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Spies revealed in room {RoomCode}. Count: {SpiesCount}",
            request.RoomCode, spies.Count);

        var eventDto = new SpiesRevealedEventDto(request.RoomCode, spies);
        await _publisher.PublishSpiesRevealedAsync(eventDto);

        return Result.Ok();
    }
}