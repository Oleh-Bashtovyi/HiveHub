using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
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
    SpyGameEventsContext context,
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

        var voterId = string.Empty;
        var timerStopped = false;
        var votesCount = 0;
        var requiredVotes = 0;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.IsInGame())
            {
                return Results.ActionFailed(ProjectMessages.VoteToStopTimer.VoteToStopTimerAvailvableOnlyMidGame);
            }

            if (room.GameState.RoundTimerState.IsStopped)
            {
                return Results.ActionFailed(ProjectMessages.VoteToStopTimer.TimerHasAlreadyStoped);
            }

            if (room.GameState.RoundTimerState.GetRemainingSeconds() <= 0.02)
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

            player.PlayerState.VotedToStopTimer = true;
            voterId = player.IdInRoom;

            var votesCount = room.CountVotedForTimerStop();
            var requiredVotes = room.GetMajorityRequiredVotes();

            context.AddEvent(new PlayerVotedToStopTimerEventDto(
                request.RoomCode,
                voterId,
                votesCount,
                requiredVotes));

            RoundTimer.CheckAndResolveTimerStop(room, context);

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: Player {PlayerId} voted to stop round timer. " +
                "Votes: {VotesCount}/{RequiredVotes}. Stopped: {IsStopped}",
                request.RoomCode,
                voterId, 
                votesCount, 
                requiredVotes, 
                timerStopped);
        }

        return result;
    }
}