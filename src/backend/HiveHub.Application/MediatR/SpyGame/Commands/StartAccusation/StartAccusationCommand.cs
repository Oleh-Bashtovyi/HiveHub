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
            if (room.GameState.CurrentPhase != SpyGamePhase.Search)
            {
                return Results.ActionFailed(ProjectMessages.Accusation.VotaCanBeDoneOnlyDuringSearch);
            }

            if (room.GameState.RoundTimerState.IsFinished)
            {
                return Results.ActionFailed(ProjectMessages.Accusation.CanNotAccuseAfterRoundTimeUp);
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

            if (targetPlayer.PlayerState.IsDead)
            {
                return Results.ActionFailed(ProjectMessages.Accusation.CannotAccuseDeadPlayer);
            }

            initiatorId = initiator.IdInRoom;

            // Pause main game timer
            if (room.GameState.RoundTimerState.IsRunning)
            {
                room.GameState.RoundTimerState.Pause();

                context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, room.RoomCode, null));

                context.AddEvent(new SpyGameRoundTimerStateChangedEventDto(
                    RoomCode: room.RoomCode,
                    TimerStatus: room.GameState.RoundTimerState.Status,
                    RemainingSeconds: room.GameState.RoundTimerState.GetRemainingSeconds()));
            }

            // Start accusation process
            room.GameState.CurrentPhase = SpyGamePhase.Accusation;
            initiator.PlayerState.HasUsedAccusation = true;

            var votingDuration = TimeSpan.FromSeconds(ProjectConstants.SpyGame.AccusationVoteDurationSeconds);
            var endsAt = DateTime.UtcNow.Add(votingDuration);

            room.GameState.ActiveVoting = new AccusationVotingState
            {
                InitiatorId = initiator.IdInRoom,
                TargetId = request.TargetPlayerId,
                VotingStartedAt = DateTime.UtcNow,
                VotingEndsAt = endsAt,
                Votes = new Dictionary<string, TargetVoteType>
                {
                    {
                        targetPlayer.IdInRoom, TargetVoteType.No
                    },
                    { 
                        initiator.IdInRoom, TargetVoteType.Yes 
                    }
                }
            };

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

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: Accusation started - {Initiator} -> {Target}",
                request.RoomCode, initiatorId, request.TargetPlayerId);
        }

        return result;
    }
}