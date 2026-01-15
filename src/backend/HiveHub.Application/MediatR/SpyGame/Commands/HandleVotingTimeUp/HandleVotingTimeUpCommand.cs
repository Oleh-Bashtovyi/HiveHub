using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.HandleVotingTimeUp;

public record HandleVotingTimeUpCommand(string RoomCode) : IRequest<Result>;

public class HandleVotingTimeUpHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
    ILogger<HandleVotingTimeUpHandler> logger) : IRequestHandler<HandleVotingTimeUpCommand, Result>
{
    public async Task<Result> Handle(HandleVotingTimeUpCommand request, CancellationToken token)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        await roomAccessor.ExecuteAsync(async (room) =>
        {
            if (room.CurrentPhase != SpyGamePhase.Accusation && room.CurrentPhase != SpyGamePhase.FinalVote)
            {
                return Result.Ok();
            }

            if (room.ActiveVoting == null)
            {
                return Result.Ok();
            }

            if (room.ActiveVoting.VotingEndsAt > DateTime.UtcNow.AddSeconds(2))
            {
                return Result.Ok();
            }

            logger.LogInformation("Voting time up in room {Code}", request.RoomCode);

            if (room.CurrentPhase == SpyGamePhase.Accusation)
            {
                room.CurrentPhase = SpyGamePhase.Search;
                room.ActiveVoting = null;

                await publisher.PublishVotingResultAsync(new VotingResultEventDto(
                    RoomCode: room.RoomCode,
                    IsSuccess: false,
                    CurrentGamePhase: SpyGamePhase.Search,
                    ResultMessage: "Vote timed out. Game resumes.",
                    AccusedId: null));

                if (room.TimerState.TimerStoppedAt.HasValue && room.TimerState.PlannedGameEndTime.HasValue)
                {
                    var timeSpentPaused = DateTime.UtcNow - room.TimerState.TimerStoppedAt.Value;
                    room.TimerState.PlannedGameEndTime = room.TimerState.PlannedGameEndTime.Value.Add(timeSpentPaused);
                    room.TimerState.IsTimerStopped = false;
                    room.TimerState.TimerStoppedAt = null;

                    var remaining = room.TimerState.PlannedGameEndTime.Value - DateTime.UtcNow;
                    var delay = remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;

                    await scheduler.ScheduleAsync(new ScheduledTask(TaskType.SpyGameRoundTimeUp, room.RoomCode, null), delay);
                }
            }
            else if (room.CurrentPhase == SpyGamePhase.FinalVote)
            {
                room.Status = RoomStatus.Ended;
                room.WinnerTeam = SpyTeam.Spies;
                room.GameEndReason = SpyGameEndReason.FinalVotingFailed;
                room.ActiveVoting = null;

                await publisher.PublishVotingResultAsync(new VotingResultEventDto(
                    RoomCode: room.RoomCode,
                    IsSuccess: false,
                    CurrentGamePhase: SpyGamePhase.None,
                    ResultMessage: "Time is up! No decision made. Spies win!",
                    AccusedId: null));

                await publisher.PublishGameEndedAsync(new SpyGameEndedEventDto(
                    RoomCode: room.RoomCode, 
                    WinnerTeam: SpyTeam.Spies,
                    Reason: SpyGameEndReason.FinalVotingFailed,
                    SpiesReveal: room.GetSpyRevealDto(),
                    ReasonMessage: "Timeout on final vote"));
            }

            return Result.Ok();
        });

        return Result.Ok();
    }
}
