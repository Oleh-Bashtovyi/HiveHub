using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public record PlayerRemovalResult(
    string RemovedPlayerId,
    string? NewHostId,
    bool ShouldDeleteRoom,
    string RoomCode
);

public static class SpyGamePlayerRemover
{
    public static PlayerRemovalResult Remove(
        SpyRoom room,
        SpyGameEventsContext context,
        string playerId,
        ISpyGameRepository repository,
        ILogger logger)
    {
        if (!room.TryGetPlayerByIdInRoom(playerId, out var player))
        {
            throw new Exception(ProjectMessages.PlayerNotFound);
        }

        var wasHost = player.IsHost;

        room.Players.Remove(player);

        context.AddEvent(new PlayerLeftEventDto(room.RoomCode, player.IdInRoom));
        context.AddEvent(new RemovePlayerFromGroupEvent(player.ConnectionId, room.RoomCode));

        if (room.Players.Count == 0)
        {
            room.MarkAsDeleted();
            return new PlayerRemovalResult(playerId, null, true, room.RoomCode);
        }

        string? newHostId = null;
        if (wasHost)
        {
            var newHost = room.Players.FirstOrDefault(x => x.IsConnected) ?? room.Players.FirstOrDefault();
            if (newHost != null)
            {
                newHost.IsHost = true;
                newHostId = newHost.IdInRoom;
                context.AddEvent(new HostChangedEventDto(room.RoomCode, newHostId));
            }
        }

        if (room.IsInGame())
        {
            HandleInGamePlayerRemoval(room, context, playerId, logger);
        }

        return new PlayerRemovalResult(playerId, newHostId, false, room.RoomCode);
    }

    private static void HandleInGamePlayerRemoval(
        SpyRoom room,
        SpyGameEventsContext context,
        string playerId,
        ILogger logger)
    {
        // Handle last chance phase
        if (room.GameState.CurrentPhase == SpyGamePhase.SpyLastChance)
        {
            MakeGuess.HandleSpyLeftDuringLastChance(room, context, playerId);
        }

        // Handle active voting
        if (room.GameState.ActiveVoting != null)
        {
            var shouldCancelVoting = false;
            var cancelReason = "";

            if (room.GameState.ActiveVoting is AccusationVotingState accState)
            {
                if (accState.InitiatorId == playerId)
                {
                    shouldCancelVoting = true;
                    cancelReason = "Accuser left the game.";
                }
                else if (accState.TargetId == playerId)
                {
                    shouldCancelVoting = true;
                    cancelReason = "Accused player left the game.";
                }
            }

            if (shouldCancelVoting)
            {
                room.GameState.ActiveVoting = null;
                room.GameState.CurrentPhase = SpyGamePhase.Search;

                context.AddEvent(new CancelTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null));
                context.AddEvent(new VotingResultEventDto(
                    room.RoomCode,
                    false,
                    SpyGamePhase.Search,
                    cancelReason,
                    null,
                    null,
                    null));

                RoundTimer.ResumeGameTimer(room, context);
            }
            else
            {
                Voting.CheckAndResolveVoting(room, context);
            }
        }

        RoundTimer.CheckAndResolveTimerStop(room, context);
        RoundEnd.CheckGameEndConditions(room, context);
    }
}
