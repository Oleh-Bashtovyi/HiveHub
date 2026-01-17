using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Domain.Models.SpyGame;

public static class PlayerKick
{
    public static void HandlePlayerKick(
        SpyRoom room,
        SpyGameEventsContext context,
        string targetId)
    {
        if (!room.TryGetPlayerByIdInRoom(targetId, out var player))
        {
            throw new Exception("Player not found");
        }

        var isSpy = player.PlayerState.IsSpy;

        if (isSpy)
        {
            room.GameState.CurrentPhase = SpyGamePhase.SpyLastChance;
            room.GameState.CaughtSpyId = targetId;
            var chanceDuration = TimeSpan.FromSeconds(ProjectConstants.SpyGame.FinacGuessingChanceDurationSeconds);
            room.GameState.SpyLastChanceEndsAt = DateTime.UtcNow.Add(chanceDuration);

            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, room.RoomCode, null));
            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null));

            context.AddEvent(new ScheduleTaskEvent(
                TaskType.SpyGameLastChanceTimeUp,
                room.RoomCode,
                targetId,
                chanceDuration));

            context.AddEvent(new VotingResultEventDto(
                RoomCode: room.RoomCode,
                IsSuccess: true,
                CurrentGamePhase: SpyGamePhase.SpyLastChance,
                ResultMessage: $"Spy {player.Name} caught! Last chance to guess.",
                AccusedId: targetId,
                AccusedSpyName: player.Name,
                IsAccusedSpy: true,
                LastChanceEndsAt: room.GameState.SpyLastChanceEndsAt));
        }
        else
        {
            room.EliminatePlayer(targetId);

            if (room.IsParanoyaMode())
            {
                RoundEnd.EndGame(
                    room,
                    SpyTeam.Spies,
                    SpyGameEndReason.ParanoiaSacrifice,
                    "You sacrificed an innocent in Paranoia mode!",
                    context);
                return;
            }

            RoundEnd.EndGame(
                room,
                SpyTeam.Spies,
                SpyGameEndReason.CivilianKicked,
                $"Player {player.Name} was innocent! Spies win.",
                context);
        }
    }
}
