using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models;
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
    public async Task<Result> Handle(RevealSpiesCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = repostiroty.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        List<SpyRevealDto> spies = new();

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (!room.IsInGame())
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

            room.Status = RoomStatus.Ended;

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        logger.LogInformation("Spies revealed in room {RoomCode}. Count: {SpiesCount}",
            request.RoomCode, spies.Count);

        var eventDto = new SpiesRevealedEventDto(request.RoomCode, spies);
        await publisher.PublishSpiesRevealedAsync(eventDto);

        return Result.Ok();
    }
}