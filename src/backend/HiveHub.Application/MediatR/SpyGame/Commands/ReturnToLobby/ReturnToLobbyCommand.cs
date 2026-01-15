using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Extensions;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.ReturnToLobby;

public record ReturnToLobbyCommand(
    string RoomCode,
    string HostConnectionId
) : IRequest<Result>;

public class ReturnToLobbyHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<ReturnToLobbyHandler> logger)
    : IRequestHandler<ReturnToLobbyCommand, Result>
{
    public async Task<Result> Handle(ReturnToLobbyCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.HostConnectionId, out var host) || !host.IsHost)
            {
                return Results.Forbidden(ProjectMessages.ReturnToLobby.OnlyHostCanReturnToLobby);
            }

            // Main status
            room.Status = RoomStatus.Lobby;

            // Secret word and category
            room.GameState.CurrentSecretWord = null;
            room.GameState.CurrentCategory = null;

            // Timer
            room.GameState.RoundStartedAt = null;
            room.GameState.RoundTimerState.Clear();

            // Phase, voting and game over reason
            room.GameState.CurrentPhase = SpyGamePhase.None;
            room.GameState.ActiveVoting = null;
            room.GameState.CaughtSpyId = null;
            room.GameState.WinnerTeam = null;
            room.GameState.GameEndReason = null;

            room.ChatMessages.Clear();

            // Players state
            foreach (var player in room.Players)
            {
                player.IsReady = false;
                player.PlayerState.IsSpy = false;
                player.PlayerState.VotedToStopTimer = false;
                player.PlayerState.HasUsedAccusation = false;
            }

            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, room.RoomCode, null));
            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null));
            context.AddEvent(new ReturnToLobbyEventDto(request.RoomCode));

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room {RoomCode} returned to lobby", request.RoomCode);
        }

        return result;
    }
}