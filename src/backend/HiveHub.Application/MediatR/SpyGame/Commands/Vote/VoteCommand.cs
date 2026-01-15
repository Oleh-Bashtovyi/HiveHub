using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.Vote;

public record VoteCommand(
    string RoomCode,
    string ConnectionId,
    string TargetPlayerId,
    TargetVoteType? VoteType
) : IRequest<Result>;

public class VoteHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<VoteHandler> logger) : IRequestHandler<VoteCommand, Result>
{
    public async Task<Result> Handle(VoteCommand request, CancellationToken token)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var voterId = string.Empty;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) => 
        {
            if (room.ActiveVoting == null)
            {
                return Results.ActionFailed(ProjectMessages.Accusation.NoActiveVoting);
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var voter))
            {
                return Results.NotFound(ProjectMessages.Accusation.InitiatorNotFound);
            }

            if (string.IsNullOrEmpty(request.TargetPlayerId))
            {
                return Results.ActionFailed(ProjectMessages.Accusation.TargetPlayerIdrequiredForFinalVote);
            }

            if (!room.TryGetPlayerByIdInRoom(request.TargetPlayerId, out var targetPlayer))
            {
                return Results.NotFound(ProjectMessages.Accusation.TargetNotFound);
            }

            voterId = voter.IdInRoom;

            if (room.CurrentPhase == SpyGamePhase.Accusation && room.ActiveVoting is AccusationVotingState accusationState)
            {
                if (request.VoteType == null)
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.VoteTypeWasNotSpecified);
                }
                if (request.TargetPlayerId != accusationState.TargetId)
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.VotingTargetMismatch);
                }
                if (!accusationState.TryVote(voter.IdInRoom, request.VoteType.Value))
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.YouAlreadyVoted);
                }
            }
            else if (room.CurrentPhase == SpyGamePhase.FinalVote && room.ActiveVoting is GeneralVotingState finalState)
            {
                if (!finalState.TryVote(voter.IdInRoom, targetPlayer.IdInRoom))
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.YouAlreadyVoted);
                }
            }
            else
            {
                return Results.ActionFailed(ProjectMessages.Accusation.UnknownVotingState);
            }

            context.AddEvent(new VoteCastEventDto(
                RoomCode: request.RoomCode,
                VoterId: voterId,
                TargetVoteType: request.VoteType,
                AgainstPlayerId: request.TargetPlayerId));

            SpyGameLogicHelper.CheckAndResolveVoting(room, context, repository, logger);

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            if (request.VoteType.HasValue)
            {
                logger.LogInformation("Player {PlayerId} susccessfuly voted in room {RoomId} against {TargetId} with vote type {VoteType}",
                    voterId, request.RoomCode, request.TargetPlayerId, request.VoteType);
            }
            else
            {
                logger.LogInformation("Player {PlayerId} susccessfuly voted in room {RoomId} against {TargetId}",
                    voterId, request.RoomCode, request.TargetPlayerId);
            }
        }

        return result;
    }
}
