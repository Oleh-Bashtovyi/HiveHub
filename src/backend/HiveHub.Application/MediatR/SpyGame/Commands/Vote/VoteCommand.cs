using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;
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
            if (room.GameState.ActiveVoting == null)
            {
                return Results.ActionFailed(ProjectMessages.Accusation.NoActiveVoting);
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var voter))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            voterId = voter.IdInRoom;

            if (room.GameState.CurrentPhase == SpyGamePhase.Accusation &&
                room.GameState.ActiveVoting is AccusationVotingState accusationState)
            {
                if (request.VoteType == null)
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.VoteTypeWasNotSpecified);
                }

                if (!string.IsNullOrEmpty(request.TargetPlayerId) &&
                    request.TargetPlayerId != accusationState.TargetId)
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.VotingTargetMismatch);
                }

                if (!accusationState.TryVote(voter.IdInRoom, request.VoteType.Value))
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.YouAlreadyVoted);
                }

                context.AddEvent(new VoteCastEventDto(
                    RoomCode: request.RoomCode,
                    VoterId: voterId,
                    TargetVoteType: request.VoteType,
                    AgainstPlayerId: accusationState.TargetId));
            }
            else if (room.GameState.CurrentPhase == SpyGamePhase.FinalVote &&
                     room.GameState.ActiveVoting is GeneralVotingState finalState)
            {
                var targetId = request.TargetPlayerId ?? "SKIP";

                if (!string.IsNullOrEmpty(request.TargetPlayerId) &&
                    request.TargetPlayerId != "SKIP")
                {
                    if (!room.TryGetPlayerByIdInRoom(request.TargetPlayerId, out var targetPlayer))
                    {
                        return Results.NotFound(ProjectMessages.Accusation.TargetNotFound);
                    }

                    if (targetPlayer.PlayerState.IsDead)
                    {
                        return Results.ActionFailed(ProjectMessages.Accusation.CannotAccuseDeadPlayer);
                    }
                }

                if (!finalState.TryVote(voter.IdInRoom, targetId))
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.YouAlreadyVoted);
                }

                context.AddEvent(new VoteCastEventDto(
                    RoomCode: request.RoomCode,
                    VoterId: voterId,
                    TargetVoteType: null,
                    AgainstPlayerId: targetId));
            }
            else
            {
                return Results.ActionFailed(ProjectMessages.Accusation.UnknownVotingState);
            }

            Voting.CheckAndResolveVoting(room, context);

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: Player {PlayerId} voted",
                request.RoomCode,
                voterId);
        }

        return result;
    }
}
