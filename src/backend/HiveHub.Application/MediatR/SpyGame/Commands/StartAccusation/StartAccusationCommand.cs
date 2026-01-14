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

namespace HiveHub.Application.MediatR.SpyGame.Commands.StartAccusation;

public record StartAccusationCommand(
    string RoomCode,
    string ConnectionId,
    string TargetPlayerId
) : IRequest<Result>;

public class StartAccusationHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<StartAccusationHandler> logger) : IRequestHandler<StartAccusationCommand, Result>
{
    public async Task<Result> Handle(StartAccusationCommand request, CancellationToken token)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var initiatorId = string.Empty;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (room.CurrentPhase != SpyGamePhase.Search)
            {
                return Results.ActionFailed(ProjectMessages.Accusation.VotaCanBeDoneOnlyDuringSearch);
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var initiator))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            if (initiator.PlayerState.HasUsedAccusation)
            {
                return Results.ActionFailed(ProjectMessages.Accusation.AlreadyUsed);
            }

            if (initiator.IdInRoom == request.TargetPlayerId)
            {
                return Results.ActionFailed(ProjectMessages.Accusation.CannotAccuseSelf);
            }

            if (!room.TryGetPlayerByIdInRoom(request.TargetPlayerId, out var targetPlayer))
            {
                return Results.NotFound(ProjectMessages.Accusation.TargetNotFound);
            }

            initiatorId = initiator.IdInRoom;

            // Stop main game timer
            if (!room.TimerState.IsTimerStopped)
            {
                room.TimerState.IsTimerStopped = true;
                room.TimerState.TimerStoppedAt = DateTime.UtcNow;

                context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, room.RoomCode, null));
            }

            // Start accusation process
            room.CurrentPhase = SpyGamePhase.Accusation;
            initiator.PlayerState.HasUsedAccusation = true;

            var votingDuration = TimeSpan.FromSeconds(ProjectConstants.SpyGame.AccusationVoteDurationSeconds);
            var endsAt = DateTime.UtcNow.Add(votingDuration);

            room.ActiveVoting = new AccusationVotingState
            {
                InitiatorId = initiator.IdInRoom,
                TargetId = request.TargetPlayerId,
                VotingStartedAt = DateTime.UtcNow,
                VotingEndsAt = endsAt,
                Votes = new Dictionary<string, TargetVoteType>
                {
                    { initiator.IdInRoom, TargetVoteType.Yes }
                }
            };

            // Start voting timer
            context.AddEvent(new ScheduleTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null, votingDuration));

            context.AddEvent(new VotingStartedEventDto(
                RoomCode: room.RoomCode,
                InitiatorId: initiator.IdInRoom,
                TargetId: targetPlayer.IdInRoom,
                TargetName: targetPlayer.Name,
                VotingType: SpyVotingType.Accusation,
                CurrentGamePhase: SpyGamePhase.Accusation,
                EndsAt: endsAt
            ));

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            logger.LogInformation("Accusation started in room {Room}: {Initiator} -> {Target}",
                request.RoomCode, initiatorId, request.TargetPlayerId);
        }

        return result;
    }
}
