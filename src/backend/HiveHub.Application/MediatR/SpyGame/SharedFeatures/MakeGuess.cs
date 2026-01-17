using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Domain.Models.SpyGame;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public static class MakeGuess
{
    public static void HandleSpyGuess(
        SpyRoom room,
        SpyGameEventsContext context,
        string playerId,
        bool isGuessCorrect,
        string? guessWord = null)
    {
        if (!room.TryGetPlayerByIdInRoom(playerId, out var player))
        {
            throw new Exception("Player not found");
        }

        // 1. Спочатку повідомляємо про спробу вгадування
        context.AddEvent(new SpyGuessAttemptedEventDto(
            RoomCode: room.RoomCode,
            SpyId: playerId,
            GuessedWord: guessWord ?? string.Empty,
            IsCorrect: isGuessCorrect));

        if (isGuessCorrect)
        {
            // 2. Шпигун вгадав - гра закінчується
            RoundEnd.EndGame(
                room,
                SpyTeam.Spies,
                SpyGameEndReason.SpyGuessedWord,
                "Spy guessed secret word!",
                context);
            return;
        }

        // 3. Неправильне вгадування - елімінація шпигуна
        room.EliminatePlayer(playerId);

        context.AddEvent(new PlayerEliminatedEventDto(
            RoomCode: room.RoomCode,
            PlayerId: playerId,
            PlayerName: player.Name,
            WasSpy: true,
            Reason: EliminationReason.FailedGuess));

        // 4. Перевірка: якщо команда шпигунів програє разом
        if (room.GameSettings.SpiesPlayAsTeam)
        {
            RoundEnd.EndGame(
                room,
                SpyTeam.Civilians,
                SpyGameEndReason.SpyWrongGuess,
                "Spy failed to guess word, civilians win!",
                context);
            return;
        }

        // 5. Перевірка: чи залишилися живі шпигуни
        var activeSpies = room.Players.Count(p => p.PlayerState.IsSpy && !p.PlayerState.IsDead);

        if (activeSpies <= 0)
        {
            RoundEnd.EndGame(
                room,
                SpyTeam.Civilians,
                SpyGameEndReason.AllSpiesEliminated,
                "All spies eliminated! Civilians win.",
                context);
            return;
        }

        // 6. Гра продовжується - повертаємося до попередньої фази
        if (room.GameState.CurrentPhase == SpyGamePhase.SpyLastChance)
        {
            var shouldResumeFinalVote = room.GameState.RoundTimerState.IsStopped;
            var previousPhase = shouldResumeFinalVote ? SpyGamePhase.FinalVote : SpyGamePhase.Search;

            room.GameState.CurrentPhase = previousPhase;
            room.GameState.CaughtSpyId = null;

            context.AddEvent(new GamePhaseChangedEventDto(
                RoomCode: room.RoomCode,
                NewPhase: previousPhase,
                PreviousPhase: SpyGamePhase.SpyLastChance));

            if (shouldResumeFinalVote)
            {
                RoundEnd.BeginFinalVoting(room, context);
            }
            else
            {
                RoundTimer.ResumeGameTimer(room, context);
            }
        }
    }

    public static void HandleSpyLeftDuringLastChance(
        SpyRoom room,
        SpyGameEventsContext context,
        string playerId)
    {
        if (room.GameState.CurrentPhase != SpyGamePhase.SpyLastChance)
        {
            return;
        }

        if (room.GameState.CaughtSpyId != playerId)
        {
            return;
        }

        if (room.GameSettings.SpiesPlayAsTeam)
        {
            RoundEnd.EndGame(
                room,
                SpyTeam.Civilians,
                SpyGameEndReason.SpyLastChanceFailed,
                "Caught spy left without guessing!",
                context);
        }
        else
        {
            room.EliminatePlayer(playerId);

            var player = room.Players.FirstOrDefault(p => p.IdInRoom == playerId);
            if (player != null)
            {
                context.AddEvent(new PlayerEliminatedEventDto(
                    RoomCode: room.RoomCode,
                    PlayerId: playerId,
                    PlayerName: player.Name,
                    WasSpy: true,
                    Reason: EliminationReason.FailedGuess));
            }

            var activeSpies = room.Players.Count(p => p.PlayerState.IsSpy && !p.PlayerState.IsDead);
            if (activeSpies <= 0)
            {
                RoundEnd.EndGame(
                    room,
                    SpyTeam.Civilians,
                    SpyGameEndReason.AllSpiesEliminated,
                    "All spies eliminated!",
                    context);
                return;
            }

            var shouldResumeFinalVote = room.GameState.RoundTimerState.IsStopped;
            var previousPhase = shouldResumeFinalVote ? SpyGamePhase.FinalVote : SpyGamePhase.Search;

            room.GameState.CurrentPhase = previousPhase;
            room.GameState.CaughtSpyId = null;

            context.AddEvent(new GamePhaseChangedEventDto(
                RoomCode: room.RoomCode,
                NewPhase: previousPhase,
                PreviousPhase: SpyGamePhase.SpyLastChance));

            if (shouldResumeFinalVote)
            {
                RoundEnd.BeginFinalVoting(room, context);
            }
            else
            {
                RoundTimer.ResumeGameTimer(room, context);
            }
        }
    }
}