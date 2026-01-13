using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.ReturnToLobby;

public record ReturnToLobbyCommand(
    string RoomCode,
    string HostConnectionId
) : IRequest<Result>;

public class ReturnToLobbyHandler(
    ISpyGameRepository gameManager,
    ISpyGamePublisher publisher,
    ILogger<ReturnToLobbyHandler> logger)
    : IRequestHandler<ReturnToLobbyCommand, Result>
{
    public async Task<Result> Handle(ReturnToLobbyCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.HostConnectionId, out var host) || !host.IsHost)
            {
                return Results.Forbidden(ProjectMessages.ReturnToLobby.OnlyHostCanReturnToLobby);
            }

            room.Status = RoomStatus.Lobby;
            room.CurrentSecretWord = null;
            room.TimerState.GameStartTime = null;
            room.TimerState.IsTimerStopped = false;
            room.TimerState.TimerStoppedAt = null;
            room.ChatMessages.Clear();

            foreach (var player in room.Players)
            {
                player.IsReady = false;
                player.PlayerState.IsSpy = false;
                player.PlayerState.VotedToStopTimer = false;
            }

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        logger.LogInformation("Room {RoomCode} returned to lobby", request.RoomCode);

        var eventDto = new ReturnToLobbyEventDto(request.RoomCode);
        await publisher.PublishReturnToLobbyAsync(eventDto);

        return Result.Ok();
    }
}