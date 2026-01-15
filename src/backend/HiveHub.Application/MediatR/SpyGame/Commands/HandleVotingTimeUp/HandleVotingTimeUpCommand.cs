using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
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
    SpyGameEventsContext context,
    ILogger<HandleVotingTimeUpHandler> logger) : IRequestHandler<HandleVotingTimeUpCommand, Result>
{
    public async Task<Result> Handle(HandleVotingTimeUpCommand request, CancellationToken token)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        return await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
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

                context.AddEvent(new VotingResultEventDto(
                    RoomCode: room.RoomCode,
                    IsSuccess: false,
                    CurrentGamePhase: SpyGamePhase.Search,
                    ResultMessage: "Vote timed out. Game resumes.",
                    LastChanceEndsAt: null,
                    IsAccusedSpy: null,
                    AccusedId: null));

                if (room.RoundTimerState.IsTimerStopped)
                {
                    room.RoundTimerState.Resume();
                    var remainingTime = TimeSpan.FromSeconds(room.RoundTimerState.GetRemainingSeconds());
                    
                    context.AddEvent(new ScheduleTaskEvent(
                        TaskType.SpyGameRoundTimeUp, 
                        room.RoomCode, 
                        null, 
                        remainingTime));

                    context.AddEvent(new SpyGameRoundTimerStateChangedEventDto(
                        room.RoomCode,
                        room.RoundTimerState.IsTimerStopped,
                        room.RoundTimerState.TimerStartedAt,
                        room.RoundTimerState.TimerWillStopAt,
                        room.RoundTimerState.TimerPausedAt));
                }
            }
            else if (room.CurrentPhase == SpyGamePhase.FinalVote)
            {
                room.Status = RoomStatus.Ended;
                room.WinnerTeam = SpyTeam.Spies;
                room.GameEndReason = SpyGameEndReason.FinalVotingFailed;
                room.ActiveVoting = null;

                context.AddEvent(new VotingResultEventDto(
                    RoomCode: room.RoomCode,
                    IsSuccess: false,
                    CurrentGamePhase: SpyGamePhase.None,
                    ResultMessage: "Time is up! No decision made. Spies win!",
                    LastChanceEndsAt: null,
                    IsAccusedSpy: null,
                    AccusedId: null));

                context.AddEvent(new SpyGameEndedEventDto(
                    RoomCode: room.RoomCode,
                    WinnerTeam: SpyTeam.Spies,
                    Reason: SpyGameEndReason.FinalVotingFailed,
                    SpiesReveal: room.GetSpyRevealDto(),
                    ReasonMessage: "Timeout on final vote"));
            }
            return Result.Ok();
        });
    }
}
