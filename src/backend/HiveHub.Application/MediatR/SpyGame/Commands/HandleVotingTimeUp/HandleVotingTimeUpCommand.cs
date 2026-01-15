using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;
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
            if (!room.IsInGame())
            {
                return Result.Ok();
            }
            if (room.GameState.CurrentPhase != SpyGamePhase.Accusation && room.GameState.CurrentPhase != SpyGamePhase.FinalVote)
            {
                return Result.Ok();
            }

            if (room.GameState.ActiveVoting == null)
            {
                return Result.Ok();
            }

            if (room.GameState.ActiveVoting.VotingEndsAt > DateTime.UtcNow.AddSeconds(2))
            {
                return Result.Ok();
            }

            logger.LogInformation("Voting time up in room {Code}", request.RoomCode);

            if (room.GameState.CurrentPhase == SpyGamePhase.Accusation)
            {
                room.GameState.CurrentPhase = SpyGamePhase.Search;
                room.GameState.ActiveVoting = null;

                context.AddEvent(new VotingResultEventDto(
                    RoomCode: room.RoomCode,
                    IsSuccess: false,
                    CurrentGamePhase: SpyGamePhase.Search,
                    ResultMessage: "Vote timed out. Game resumes.",
                    LastChanceEndsAt: null,
                    IsAccusedSpy: null,
                    AccusedId: null));

                if (room.GameState.RoundTimerState.IsTimerStopped)
                {
                    room.GameState.RoundTimerState.Resume();
                    var remainingTime = TimeSpan.FromSeconds(room.GameState.RoundTimerState.GetRemainingSeconds());
                    
                    context.AddEvent(new ScheduleTaskEvent(
                        TaskType.SpyGameRoundTimeUp, 
                        room.RoomCode, 
                        null, 
                        remainingTime));

                    context.AddEvent(new SpyGameRoundTimerStateChangedEventDto(
                        room.RoomCode,
                        room.GameState.RoundTimerState.IsTimerStopped,
                        room.GameState.RoundTimerState.TimerStartedAt,
                        room.GameState.RoundTimerState.TimerWillStopAt,
                        room.GameState.RoundTimerState.TimerPausedAt));
                }
            }
            else if (room.GameState.CurrentPhase == SpyGamePhase.FinalVote)
            {
                room.Status = RoomStatus.Ended;
                room.GameState.WinnerTeam = SpyTeam.Spies;
                room.GameState.GameEndReason = SpyGameEndReason.FinalVotingFailed;
                room.GameState.ActiveVoting = null;

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
