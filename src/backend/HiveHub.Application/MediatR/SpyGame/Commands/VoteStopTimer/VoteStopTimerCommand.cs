using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.VoteStopTimer;

public record VoteStopTimerCommand(
    string RoomCode,
    string ConnectionId
) : IRequest<Result>;

public class VoteStopTimerHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
    ILogger<VoteStopTimerHandler> logger)
    : IRequestHandler<VoteStopTimerCommand, Result>
{
    public async Task<Result> Handle(VoteStopTimerCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = repository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string votedPlayerId = null!;
        bool timerStopped = false;
        int votesCount = 0;
        int requiredVotes = 0;

        var result = await roomAccessor.ExecuteAsync(async (room) =>
        {
            if (!room.IsInGame())
            {
                return Results.ActionFailed(ProjectMessages.VoteToStopTimer.VoteToStopTimerAvailvableOnlyMidGame);
            }

            if (room.RoundTimerState.IsTimerStopped)
            {
                return Results.ActionFailed(ProjectMessages.VoteToStopTimer.TimerHasAlreadyStoped);
            }

            if (room.RoundTimerState.GetRemainingSeconds() <= 0.02)
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
                room.RoundTimerState.Pause();
                timerStopped = true;

                var timerTask = new ScheduledTask(TaskType.SpyGameRoundTimeUp, request.RoomCode, null);
                await scheduler.CancelAsync(timerTask);
            }

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        logger.LogInformation("Vote to stop timer in room {RoomCode}. Votes: {VotesCount}/{RequiredVotes}. Stopped: {IsStopped}",
            request.RoomCode, votesCount, requiredVotes, timerStopped);

        var eventDto = new PlayerVotedToStopTimerEventDto(request.RoomCode, votedPlayerId, votesCount, requiredVotes);
        await publisher.PublishTimerVoteAsync(eventDto);

        return Result.Ok();
    }
}