using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.VoteStopTimer;

public record VoteStopTimerCommand(
    string RoomCode,
    string ConnectionId
) : IRequest<Result>;

public class VoteStopTimerHandler(
    ISpyGameRepository gameManager,
    ISpyGamePublisher publisher,
    ILogger<VoteStopTimerHandler> logger)
    : IRequestHandler<VoteStopTimerCommand, Result>
{
    private readonly ISpyGameRepository _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<VoteStopTimerHandler> _logger = logger;

    public async Task<Result> Handle(VoteStopTimerCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        bool timerStopped = false;
        int votesCount = 0;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.InGame)
            {
                return Results.ActionFailed("Гра не йде.");
            }

            if (room.TimerState.IsTimerStopped)
            {
                return Results.ActionFailed("Таймер вже зупинено.");
            }

            if (!room.Players.TryGetValue(request.ConnectionId, out var player))
            {
                return Results.NotFound("Гравця не знайдено.");
            }

            if (player.PlayerState.VotedToStopTimer)
            {
                return Results.ActionFailed("Ви вже проголосували.");
            }

            player.PlayerState.VotedToStopTimer = true;
            votesCount = room.Players.Values.Count(p => p.PlayerState.VotedToStopTimer);

            // Потрібно 2 голоси для зупинки таймера
            if (votesCount >= 2)
            {
                room.TimerState.IsTimerStopped = true;
                room.TimerState.TimerStoppedAt = DateTime.UtcNow;
                timerStopped = true;
            }

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Vote to stop timer in room {RoomCode}. Votes: {VotesCount}/2. Stopped: {IsStopped}",
            request.RoomCode, votesCount, timerStopped);

        var eventDto = new TimerStoppedEventDto(request.RoomCode, votesCount, 2);
        await _publisher.PublishTimerVoteAsync(eventDto);

        return Result.Ok();
    }
}