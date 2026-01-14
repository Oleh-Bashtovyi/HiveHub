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

namespace HiveHub.Application.MediatR.SpyGame.Commands.HandleTimeUp;

public record HandleGameTimeUpCommand(string RoomCode) : IRequest<Result>;

public class HandleGameTimeUpHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
    ILogger<HandleGameTimeUpHandler> logger)
    : IRequestHandler<HandleGameTimeUpCommand, Result>
{
    public async Task<Result> Handle(HandleGameTimeUpCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        VotingStartedEventDto? votingEvent = null;

        await roomAccessor.ExecuteAsync(async (room) =>
        {
            if (!room.IsInGame() || room.TimerState.IsTimerStopped)
            {
                return Result.Ok();
            }

            if (room.TimerState.PlannedGameEndTime > DateTime.UtcNow.AddSeconds(2))
            {
                return Result.Ok();
            }

            logger.LogInformation("Game timer expired in room {RoomCode}. Starting Final Vote.", request.RoomCode);

            room.TimerState.IsTimerStopped = true;
            room.TimerState.TimerStoppedAt = DateTime.UtcNow;

            room.CurrentPhase = SpyGamePhase.FinalVote;

            var votingDuration = TimeSpan.FromSeconds(ProjectConstants.SpyGame.FinalVoteDurationSeconds);
            var endsAt = DateTime.UtcNow.Add(votingDuration);

            room.ActiveVoting = new GeneralVotingState
            {
                VotingStartedAt = DateTime.UtcNow,
                VotingEndsAt = endsAt,
                Votes = new Dictionary<string, string?>()
            };

            var votingTask = new ScheduledTask(TaskType.SpyVotingTimeUp, room.RoomCode, null);
            await scheduler.ScheduleAsync(votingTask, votingDuration);

            votingEvent = new VotingStartedEventDto(
                RoomCode: room.RoomCode,
                InitiatorId: "System",
                TargetId: null,
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