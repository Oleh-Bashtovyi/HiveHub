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
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
    ILogger<VoteHandler> logger) : IRequestHandler<VoteCommand, Result>
{
    public async Task<Result> Handle(VoteCommand request, CancellationToken token)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        string voterId = string.Empty;

        var result = await roomAccessor.ExecuteAsync(async (room) => 
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

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        await publisher.PublishVoteCastAsync(new VoteCastEventDto(
            RoomCode: request.RoomCode, 
            VoterId: voterId,
            TargetVoteType: request.VoteType,
            AgainstPlayerId: request.TargetPlayerId));

        //
        //
        //
        // TODO: Dont publish inside of ExecuteAsync, store all events in list and publish outside of logic block
        //
        //
        //
        var roomAccessorForResolution = repository.GetRoom(request.RoomCode);
        if (roomAccessorForResolution != null)
        {
            await roomAccessorForResolution.ExecuteAsync(async (room) =>
            {
                await SpyGameLogicHelper.CheckAndResolveVoting(room, publisher, scheduler, repository, logger);
            });
        }

        return Result.Ok();
    }
}
