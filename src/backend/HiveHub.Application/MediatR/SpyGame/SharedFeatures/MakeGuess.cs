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

        context.AddEvent(new SpyMadeGuessEventDto(
            RoomCode: room.RoomCode,
            PlayerId: playerId,
            Word: guessWord ?? string.Empty,
            IsGuessCorrect: isGuessCorrect,
            IsSpyDead: player.PlayerState.IsDead));

        if (isGuessCorrect)
        {
            RoundEnd.EndGame(
                room,
                SpyTeam.Spies,
                SpyGameEndReason.SpyGuessedWord,
                "Spy guessed secret word!",
                context);
            return;
        }

        room.EliminatePlayer(playerId);

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

        // Resume phase based on current state
        if (room.GameState.CurrentPhase == SpyGamePhase.SpyLastChance)
        {
            // After last chance failed, check if we should continue final vote or search
            var shouldResumeFinalVote = room.GameState.RoundTimerState.IsStopped;

            if (shouldResumeFinalVote)
            {
                RoundEnd.BeginFinalVoting(room, context);
            }
            else
            {
                room.GameState.CurrentPhase = SpyGamePhase.Search;
                room.GameState.CaughtSpyId = null;
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
            if (shouldResumeFinalVote)
            {
                RoundEnd.BeginFinalVoting(room, context);
            }
            else
            {
                room.GameState.CurrentPhase = SpyGamePhase.Search;
                room.GameState.CaughtSpyId = null;
                RoundTimer.ResumeGameTimer(room, context);
            }
        }
    }
}

/*using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Domain.Models.SpyGame;
using MediatR;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public static class MakeGuess
{
    public static void HandleSpyGuess(SpyRoom room, SpyGameEventsContext context, string playerId, bool IsGuessCorrect, string? guessWord = null, bool isPlayerLeftRoom = false)
    {
        if (isPlayerLeftRoom)
        {
            if (!room.IsInGame() || room.GameState.CurrentPhase != SpyGamePhase.SpyLastChance)
            {
                return;
            }
            if (room.GameState.CaughtSpyId == playerId)
            {
                if (room.GameSettings.SpiesPlayAsTeam)
                {
                    RoundEnd.EndGame(
                        room,
                        SpyTeam.Civilians,
                        SpyGameEndReason.SpyWrongGuess,
                        "Spy didnt managed to guess word, civilians win!",
                        context);
                }
                else
                {
                    // Resume final voting
                }
            }

            return;
        }

        if (!room.TryGetPlayerByIdInRoom(playerId, out var player))
        {
            throw new Exception("Player not found");
        }

        context.AddEvent(new SpyMadeGuessEventDto(
            RoomCode: room.RoomCode,
            PlayerId: playerId,
            Word: guessWord ?? string.Empty,
            IsGuessCorrect: IsGuessCorrect,
            IsSpyDead: player.PlayerState.IsDead));

        if (IsGuessCorrect)
        {
            RoundEnd.EndGame(
                room,
                SpyTeam.Spies,
                SpyGameEndReason.SpyGuessedWord,
                "Spy guessed secret word!",
                context);
        }

        room.EliminatePlayer(playerId);

        if (room.GameSettings.SpiesPlayAsTeam)
        {
            RoundEnd.EndGame(
                room,
                SpyTeam.Civilians,
                SpyGameEndReason.SpyWrongGuess,
                "Spy didnt managed to guess word, civilians win!",
                context);
        }
        else
        {
            var activeSpies = room.Players.Count(p => p.PlayerState.IsSpy && !p.PlayerState.IsDead);

            if (activeSpies <= 0)
            {
                RoundEnd.EndGame(
                    room, 
                    SpyTeam.Civilians, 
                    SpyGameEndReason.AllSpiesEliminated, 
                    "All spies eliminated! Civilians win.", 
                    context);
            }

            // Resume final voting if round timer stopped or continue round timer
        }
    }
}
*/