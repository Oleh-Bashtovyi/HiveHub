using FluentResults;
using HiveHub.Application.Constants;
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

namespace HiveHub.Application.MediatR.SpyGame.SystemCommands.HandleGameTimeUp;

public record ProcessRoundTimeUpCommand(string RoomCode) : IRequest<Result>;

public class ProcessRoundTimeUpHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
    ILogger<ProcessRoundTimeUpHandler> logger)
    : IRequestHandler<ProcessRoundTimeUpCommand, Result>
{
    public async Task<Result> Handle(ProcessRoundTimeUpCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        VotingStartedEventDto? votingEvent = null;

        await roomAccessor.ExecuteAsync(async (room) =>
        {
            if (!room.IsInGame() || room.GameState.RoundTimerState.IsTimerStopped)
            {
                return Result.Ok();
            }

            if (room.GameState.RoundTimerState.TimerWillStopAt > DateTime.UtcNow.AddSeconds(2))
            {
                return Result.Ok();
            }

            logger.LogInformation("Game timer expired in room {RoomCode}. Starting Final Vote.", request.RoomCode);

            room.GameState.RoundTimerState.Pause();

            room.GameState.CurrentPhase = SpyGamePhase.FinalVote;

            var votingDuration = TimeSpan.FromSeconds(ProjectConstants.SpyGame.FinalVoteDurationSeconds);
            var endsAt = DateTime.UtcNow.Add(votingDuration);

            room.GameState.ActiveVoting = new GeneralVotingState
            {
                VotingStartedAt = DateTime.UtcNow,
                VotingEndsAt = endsAt,
                Votes = new Dictionary<string, string?>()
            };

            var votingTask = new ScheduledTask(TaskType.SpyGameVotingTimeUp, room.RoomCode, null);
            await scheduler.ScheduleAsync(votingTask, votingDuration);

            votingEvent = new VotingStartedEventDto(
                RoomCode: room.RoomCode,
                InitiatorId: "System",
                TargetId: null,
                TargetName: null,
                VotingType: SpyVotingType.Final,
                CurrentGamePhase: SpyGamePhase.FinalVote,
                EndsAt: endsAt
            );

            return Result.Ok();
        });

        if (votingEvent != null)
        {
            await publisher.PublishVotingStartedAsync(votingEvent);
        }

        return Result.Ok();
    }
}