using FluentResults;
using HiveHub.Application.Constants;
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
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string votedPlayerId = null!;
        bool timerStopped = false;
        int votesCount = 0;
        int requiredVotes = 0;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.InGame)
            {
                return Results.ActionFailed(ProjectMessages.VoteToStopTimer.VoteToStopTimerAvailvableOnlyMidGame);
            }

            if (room.TimerState.IsTimerStopped)
            {
                return Results.ActionFailed(ProjectMessages.VoteToStopTimer.TimerHasAlreadyStoped);
            }

            if (room.TimerState.GetRemainingSeconds() <= 0.02)
            {
                return Results.ActionFailed(ProjectMessages.VoteToStopTimer.TimeHasPassed);
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            if (player.PlayerState.VotedToStopTimer)
            {
                return Results.ActionFailed(ProjectMessages.VoteToStopTimer.YouHaveAlreadyVoted);
            }

            votedPlayerId = player.IdInRoom;
            player.PlayerState.VotedToStopTimer = true;
            votesCount = room.Players.Count(p => p.PlayerState.VotedToStopTimer);

            var activePlayers = room.Players.Count(p => p.IsConnected);
            requiredVotes = (int)Math.Ceiling(activePlayers / 2.0);

            if (requiredVotes < 1) 
                requiredVotes = 1;

            if (votesCount >= requiredVotes)
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

        _logger.LogInformation("Vote to stop timer in room {RoomCode}. Votes: {VotesCount}/{RequiredVotes}. Stopped: {IsStopped}",
            request.RoomCode, votesCount, requiredVotes, timerStopped);

        var eventDto = new TimerStoppedEventDto(request.RoomCode, votedPlayerId, votesCount, requiredVotes);
        await _publisher.PublishTimerVoteAsync(eventDto);

        return Result.Ok();
    }
}