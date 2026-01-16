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
        var wasSpy = player.PlayerState.IsSpy;

        // 1. Remove Player
        room.Players.Remove(player);

        context.AddEvent(new PlayerLeftEventDto(room.RoomCode, player.IdInRoom));
        context.AddEvent(new RemovePlayerFromGroupEvent(player.ConnectionId, room.RoomCode));

        // 2. Handle Empty Room
        if (room.Players.Count == 0)
        {
            room.MarkAsDeleted();
            return new PlayerRemovalResult(playerId, null, true, room.RoomCode);
        }

        // 3. Handle Host Migration
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

        // 4. Handle In-Game Logic
        if (room.IsInGame())
        {
            // A. Check Active Voting Disruption
            if (room.GameState.ActiveVoting != null)
            {
                var shouldCancelVoting = false;
                var cancelReason = "";

                if (room.GameState.ActiveVoting is AccusationVotingState accState)
                {
                    // If Initiator left -> Cancel
                    if (accState.InitiatorId == playerId)
                    {
                        shouldCancelVoting = true;
                        cancelReason = "Accuser left the game.";
                    }
                    // If Target left -> Cancel (Cannot accuse someone who is gone)
                    else if (accState.TargetId == playerId)
                    {
                        shouldCancelVoting = true;
                        cancelReason = "Accused player left the game.";
                    }
                }

                if (shouldCancelVoting)
                {
                    room.GameState.ActiveVoting = null;
                    context.AddEvent(new CancelTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null));
                    context.AddEvent(new VotingResultEventDto(room.RoomCode, false, SpyGamePhase.Search, cancelReason, null, null, null));

                    // Resume timer if it was paused
                    SpyGameLogicHelper.ResumeGameTimer(room, context);
                }
                else
                {
                    // Update voting thresholds if voting persists (e.g. final vote or unrelated accusation)
                    SpyGameLogicHelper.CheckAndResolveVoting(room, context, repository, logger);
                }
            }

            // B. Handle Special Phases (Spy Last Chance)
            if (room.GameState.CurrentPhase == SpyGamePhase.SpyLastChance && room.GameState.CaughtSpyId == playerId)
            {
                // Caught spy left without guessing -> Treat as Fail
                context.AddEvent(new ChatMessageEventDto(
                    RoomCode: room.RoomCode,
                    Message: new ChatMessageDto(
                        PlayerId: "System", 
                        PlayerName: "System", 
                        Message: "Caught spy left! Treating as wrong guess.",
                        Timestamp: DateTime.UtcNow)));

                SpyGameLogicHelper.HandleSpyGuessedWrong(room, context);
            }

            // C. Check if Game Should End (e.g. only 1 player left, or all spies left)
            SpyGameLogicHelper.CheckAndResolveTimerStop(room, context, logger);
            SpyGameLogicHelper.CheckAndResolveVoting(room, context, repository, logger);
            SpyGameLogicHelper.CheckGameEndConditions(room, context, logger);
        }

        return new PlayerRemovalResult(playerId, newHostId, false, room.RoomCode);
    }
}
