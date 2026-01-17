using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Domain.Models.SpyGame;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

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
            // 1. Шпигуна викрито
            context.AddEvent(new SpyRevealedEventDto(
                RoomCode: room.RoomCode,
                SpyId: targetId,
                SpyName: player.Name));

            // 2. Змінюємо фазу гри
            room.GameState.CurrentPhase = SpyGamePhase.SpyLastChance;
            room.GameState.CaughtSpyId = targetId;

            context.AddEvent(new GamePhaseChangedEventDto(
                RoomCode: room.RoomCode,
                NewPhase: SpyGamePhase.SpyLastChance,
                PreviousPhase: SpyGamePhase.Accusation // або FinalVote
            ));

            // 3. Запускаємо останній шанс
            var chanceDuration = TimeSpan.FromSeconds(ProjectConstants.SpyGame.FinacGuessingChanceDurationSeconds);
            room.GameState.SpyLastChanceEndsAt = DateTime.UtcNow.Add(chanceDuration);

            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, room.RoomCode, null));
            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null));

            context.AddEvent(new ScheduleTaskEvent(
                TaskType.SpyGameLastChanceTimeUp,
                room.RoomCode,
                targetId,
                chanceDuration));

            context.AddEvent(new SpyLastChanceStartedEventDto(
                RoomCode: room.RoomCode,
                SpyId: targetId,
                SpyName: player.Name,
                EndsAt: room.GameState.SpyLastChanceEndsAt.Value));
        }
        else
        {
            // 1. Цивільного викинули
            room.EliminatePlayer(targetId);

            context.AddEvent(new PlayerEliminatedEventDto(
                RoomCode: room.RoomCode,
                PlayerId: targetId,
                PlayerName: player.Name,
                WasSpy: false,
                Reason: EliminationReason.VotedOut));

            // 2. Перевірка режиму паранойї
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

            // 3. Звичайний режим - шпигуни виграли
            RoundEnd.EndGame(
                room,
                SpyTeam.Spies,
                SpyGameEndReason.CivilianKicked,
                $"Player {player.Name} was innocent! Spies win.",
                context);
        }
    }
}