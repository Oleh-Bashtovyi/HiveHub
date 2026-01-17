using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.SystemCommands.HandleVotingTimeUp;

public record HandleVotingTimeUpCommand(string RoomCode) : IRequest<Result>;

public class HandleVotingTimeUpHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<HandleVotingTimeUpHandler> logger)
    : IRequestHandler<HandleVotingTimeUpCommand, Result>
{
    public async Task<Result> Handle(HandleVotingTimeUpCommand request, CancellationToken token)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.IsInGame() || room.GameState.ActiveVoting == null)
            {
                return Result.Ok();
            }

            if (room.GameState.ActiveVoting.VotingEndsAt > DateTime.UtcNow.AddSeconds(2))
            {
                return Result.Ok();
            }

            logger.LogInformation("Room [{RoomCode}]: Voting time up", request.RoomCode);

            Voting.HandleVotingTimeUp(room, context);

            return Result.Ok();
        });

        return Result.Ok();
    }
}