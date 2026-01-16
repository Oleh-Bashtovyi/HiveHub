using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
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

            if (room.GameState.RoundTimerState.IsTimerStopped)
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
            votedPlayerId = player.IdInRoom;

            SpyGameLogicHelper.CheckAndResolveTimerStop(room, context, logger);

            var votesCount = room.Players.Count(p => p.PlayerState.VotedToStopTimer && p.IsConnected);
            var requiredVotes = (int)Math.Ceiling(room.Players.Count(p => p.IsConnected) / 2.0);

            context.AddEvent(new PlayerVotedToStopTimerEventDto(request.RoomCode, player.IdInRoom, votesCount, requiredVotes));

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: Player {PlayerId} voted to stop round timer. " +
                "Votes: {VotesCount}/{RequiredVotes}. Stopped: {IsStopped}",
                request.RoomCode, 
                votedPlayerId, 
                votesCount, 
                requiredVotes, 
                timerStopped);
        }

        return result;
    }
}