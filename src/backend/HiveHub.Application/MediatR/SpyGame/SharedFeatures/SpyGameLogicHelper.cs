using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public static class SpyGameLogicHelper
{
    public static void CheckAndResolveVoting(
        SpyRoom room,
        SpyGameEventsContext context,
        ISpyGameRepository repository,
        ILogger logger)
    {
        if (room.GameState.ActiveVoting == null)
        {
            return;
        }

        var totalPlayersCount = room.Players.Count;

        if (totalPlayersCount == 0)
        {
            return;
        }

        var requiredVotes = room.GetMajorityRequiredVotes();

        var votingResolved = false;

        if (room.GameState.CurrentPhase == SpyGamePhase.Accusation && room.GameState.ActiveVoting is AccusationVotingState accState)
        {
            votingResolved = ResolveAccusation(room, accState, totalPlayersCount, requiredVotes, context, logger);
        }
        else if (room.GameState.CurrentPhase == SpyGamePhase.FinalVote && room.GameState.ActiveVoting is GeneralVotingState finalState)
        {
            votingResolved = ResolveFinalVote(room, finalState, totalPlayersCount, requiredVotes, context, logger);
        }

        if (votingResolved)
        {
            room.GameState.ActiveVoting = null;
            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null));

            // Always check if the game should end after a voting resolution (e.g. all spies kicked)
            CheckGameEndConditions(room, context, logger);
        }
    }

    private static bool ResolveAccusation(
        SpyRoom room,
        AccusationVotingState accState,
        int totalPlayers,
        int requiredVotes,
        SpyGameEventsContext context,
        ILogger logger)
    {
        var yesVotes = accState.Votes.Count(v => v.Value == TargetVoteType.Yes);
        // We count total CAST votes to prevent hanging if someone votes "No" or skips implicitly
        var totalCastVotes = accState.Votes.Count;

        // Case 1: Majority reached -> Success
        if (yesVotes >= requiredVotes)
        {
            HandlePlayerKicked(room, accState.TargetId, context, logger, isFinalVote: false);
            return true;
        }

        // Case 2: Mathematical impossibility or Everyone voted
        // If (Votes Cast == Total Players) AND (Yes < Required) -> Fail immediately
        // Or if remaining players cannot possibly provide enough YES votes
        var potentialYesVotes = yesVotes + (totalPlayers - totalCastVotes);

        if (totalCastVotes >= totalPlayers || potentialYesVotes < requiredVotes)
        {
            room.GameState.CurrentPhase = SpyGamePhase.Search;

            context.AddEvent(new VotingResultEventDto(
                RoomCode: room.RoomCode,
                IsSuccess: false,
                CurrentGamePhase: SpyGamePhase.Search,
                ResultMessage: "Not enough votes to accuse. Game resumes.",
                AccusedId: accState.TargetId,
                IsAccusedSpy: null,
                LastChanceEndsAt: null));

            ResumeGameTimer(room, context);
            return true;
        }

        return false; // Voting continues
    }

    private static bool ResolveFinalVote(
        SpyRoom room,
        GeneralVotingState finalState,
        int totalPlayers,
        int requiredVotes,
        SpyGameEventsContext context,
        ILogger logger)
    {
        // Only resolve if EVERYONE has voted (skipped or targeted)
        if (finalState.Votes.Count < totalPlayers)
        {
            return false;
        }

        // Group votes excluding Skips for target calculation
        var validVotes = finalState.Votes
            .Where(x => !string.IsNullOrEmpty(x.Value) && x.Value != "SKIP")
            .GroupBy(x => x.Value!)
            .OrderByDescending(g => g.Count())
            .ToList();

        var skipsCount = finalState.Votes.Count(x => x.Value == "SKIP" || string.IsNullOrEmpty(x.Value));

        // Logic: Paranoia Win (Everyone Skipped)
        if (skipsCount == totalPlayers)
        {
            // If no spies exist (Paranoia Mode), Civilians win by skipping
            if (room.GameState.SpiesCountSnapshot == 0)
            {
                EndGame(room, SpyTeam.Civilians, SpyGameEndReason.ParanoiaSurvived, "Everyone skipped in Paranoia mode. Civilians win!", context);
            }
            else
            {
                // If spies exist and everyone skips -> Spies Win
                EndGame(room, SpyTeam.Spies, SpyGameEndReason.FinalVotingFailed, "Civilians failed to identify the spy.", context);
            }
            return true;
        }

        // Logic: Check Consensus
        if (!validVotes.Any() || validVotes.First().Count() < requiredVotes)
        {
            EndGame(room, SpyTeam.Spies, SpyGameEndReason.FinalVotingFailed, "Consensus not reached.", context);
            return true;
        }

        // Target Selected
        var targetId = validVotes.First().Key;
        HandlePlayerKicked(room, targetId, context, logger, isFinalVote: true);
        return true;
    }

    // Common logic for Accusation Success or Final Vote Consensus
    private static void HandlePlayerKicked(
        SpyRoom room,
        string targetId,
        SpyGameEventsContext context,
        ILogger logger,
        bool isFinalVote)
    {
        var targetPlayer = room.Players.FirstOrDefault(p => p.IdInRoom == targetId);

        // Handling edge case: Player might have left during voting
        // If player not found, we treat it as if they were kicked, but skip spy checks if possible, or assume innocent?
        // Better: Assume they were innocent contextually unless we have data. 
        // But here we rely on PlayerState.

        if (targetPlayer == null)
        {
            // Fallback if player disconnects completely and is removed from list mid-logic
            // Usually handled by PlayerRemover, but for safety:
            context.AddEvent(new VotingResultEventDto(room.RoomCode, false, SpyGamePhase.Search, "Target player not found.", null, null, null));
            if (!isFinalVote) ResumeGameTimer(room, context);
            return;
        }

        bool isSpy = targetPlayer.PlayerState.IsSpy;

        if (isSpy)
        {
            // SPY CAUGHT logic
            targetPlayer.PlayerState.IsDead = true;
            // Update snapshot? No, snapshot tracks initial count. We track current live spies via IsDead.

            // Logic: Give them a "Last Chance" to guess
            room.GameState.CurrentPhase = SpyGamePhase.SpyLastChance;
            room.GameState.CaughtSpyId = targetId;
            var chanceDuration = TimeSpan.FromSeconds(ProjectConstants.SpyGame.FinacGuessingChanceDurationSeconds);
            room.GameState.SpyLastChanceEndsAt = DateTime.UtcNow.Add(chanceDuration);

            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, room.RoomCode, null));

            context.AddEvent(new VotingResultEventDto(
                RoomCode: room.RoomCode,
                IsSuccess: true,
                CurrentGamePhase: SpyGamePhase.SpyLastChance,
                ResultMessage: $"Spy {targetPlayer.Name} caught! Last chance to guess.",
                AccusedId: targetId,
                IsAccusedSpy: true,
                LastChanceEndsAt: room.GameState.SpyLastChanceEndsAt));
        }
        else
        {
            // INNOCENT KICKED logic
            targetPlayer.PlayerState.IsDead = true;

            // Paranoia check: If 0 spies were in game, kicking an innocent is a loss (Sacrifice)
            if (room.GameState.SpiesCountSnapshot == 0)
            {
                EndGame(room, SpyTeam.Spies, SpyGameEndReason.ParanoiaSacrifice, "You sacrificed an innocent in Paranoia mode!", context);
                return;
            }

            // Standard Spyfall: If innocent kicked -> Spies Win immediately
            EndGame(room, SpyTeam.Spies, SpyGameEndReason.CivilianKicked, $"Player {targetPlayer.Name} was Innocent! Spies win.", context);
        }
    }

    // Called when a spy fails their "Last Chance" guess
    public static void HandleSpyGuessedWrong(SpyRoom room, SpyGameEventsContext context)
    {
        // Spy is already IsDead = true from HandlePlayerKicked
        // Check if there are other spies remaining
        var remainingSpies = room.Players.Count(p => p.PlayerState.IsSpy && !p.PlayerState.IsDead);

        if (remainingSpies > 0)
        {
            // Continue Game
            room.GameState.CurrentPhase = SpyGamePhase.Search;
            room.GameState.CaughtSpyId = null;
            room.GameState.SpyLastChanceEndsAt = null;

            context.AddEvent(new VotingResultEventDto(
                RoomCode: room.RoomCode,
                IsSuccess: true,
                CurrentGamePhase: SpyGamePhase.Search,
                ResultMessage: "Spy failed to guess! But the game continues...",
                AccusedId: null,
                IsAccusedSpy: true,
                LastChanceEndsAt: null));

            ResumeGameTimer(room, context);
        }
        else
        {
            // All spies eliminated and failed guesses
            EndGame(room, SpyTeam.Civilians, SpyGameEndReason.AllSpiesEliminated, "All spies eliminated! Civilians win.", context);
        }
    }

    // Centralized Game End Checker (Call after player leave, timer end, or voting)
    public static void CheckGameEndConditions(SpyRoom room, SpyGameEventsContext context, ILogger logger)
    {
        if (!room.IsInGame()) return;

        var activePlayers = room.Players.Count; // Total in list
        var spies = room.Players.Count(p => p.PlayerState.IsSpy && !p.PlayerState.IsDead);
        var civilians = room.Players.Count(p => !p.PlayerState.IsSpy && !p.PlayerState.IsDead);

        // 1. Only 1 player left -> Game Over (Technical limitation)
        if (activePlayers < 2)
        {
            EndGame(room, SpyTeam.Spies, SpyGameEndReason.TimerExpired, "Not enough players to continue.", context);
            return;
        }

        // 2. 2 Players Left scenario (1 vs 1)
        if (activePlayers == 2)
        {
            if (spies >= 1)
            {
                // Spy wins because they can't be voted out (requires majority 2/2 usually, or spy just votes no)
                EndGame(room, SpyTeam.Spies, SpyGameEndReason.TimerExpired, "Spies control the room (1v1).", context);
                return;
            }
        }

        // 3. Spies >= Civilians (Standard Spyfall rule, mostly immediate win for spies)
        if (spies >= civilians && spies > 0)
        {
            EndGame(room, SpyTeam.Spies, SpyGameEndReason.TimerExpired, "Spies have majority or equality.", context);
            return;
        }

        // 4. All spies dead (handled in voting, but safety check)
        if (spies == 0 && room.GameState.SpiesCountSnapshot > 0)
        {
            EndGame(room, SpyTeam.Civilians, SpyGameEndReason.AllSpiesEliminated, "All spies are gone.", context);
            return;
        }
    }

    public static void ResumeGameTimer(SpyRoom room, SpyGameEventsContext context)
    {
        if (room.GameState.RoundTimerState.IsTimerStopped)
        {
            room.GameState.RoundTimerState.Resume();
            var remaining = room.GameState.RoundTimerState.GetRemainingSeconds();

            context.AddEvent(new ScheduleTaskEvent(
                Type: TaskType.SpyGameRoundTimeUp,
                RoomCode: room.RoomCode,
                TargetId: null,
                Delay: TimeSpan.FromSeconds(remaining)));

            context.AddEvent(new SpyGameRoundTimerStateChangedEventDto(
                 room.RoomCode, false,
                 room.GameState.RoundTimerState.TimerStartedAt,
                 room.GameState.RoundTimerState.TimerWillStopAt,
                 null));
        }
    }

    private static void EndGame(SpyRoom room, SpyTeam winner, SpyGameEndReason reason, string message, SpyGameEventsContext context)
    {
        room.Status = RoomStatus.Ended;
        room.GameState.WinnerTeam = winner;
        room.GameState.GameEndReason = reason;

        context.AddEvent(new SpyGameEndedEventDto(
            RoomCode: room.RoomCode,
            WinnerTeam: winner,
            Reason: reason,
            SpiesReveal: room.GetSpyRevealDto(),
            ReasonMessage: message));

        // Cleanup timers
        context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, room.RoomCode, null));
        context.AddEvent(new CancelTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null));
    }

    // Timer Stop Logic (Vote to stop timer)
    public static void CheckAndResolveTimerStop(SpyRoom room, SpyGameEventsContext context, ILogger logger)
    {
        if (room.GameState.RoundTimerState.IsTimerStopped || !room.IsInGame()) return;

        // Only CONNECTED players count for timer stop functionality (unlike voting where we wait for reconnect)
        var connectedPlayers = room.Players.Count(p => p.IsConnected);
        var votesCount = room.Players.Count(p => p.PlayerState.VotedToStopTimer && p.IsConnected);

        var requiredVotes = (int)Math.Ceiling(connectedPlayers / 2.0);
        if (requiredVotes < 1) requiredVotes = 1;

        if (votesCount >= requiredVotes)
        {
            room.GameState.RoundTimerState.Pause();
            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, room.RoomCode, null));
            context.AddEvent(new PlayerVotedToStopTimerEventDto(room.RoomCode, "System", votesCount, requiredVotes));

            // Trigger Final Voting Phase automatically
            room.GameState.CurrentPhase = SpyGamePhase.FinalVote;

            var votingDuration = TimeSpan.FromSeconds(ProjectConstants.SpyGame.FinalVoteDurationSeconds);
            room.GameState.ActiveVoting = new GeneralVotingState
            {
                VotingStartedAt = DateTime.UtcNow,
                VotingEndsAt = DateTime.UtcNow.Add(votingDuration),
                Votes = new Dictionary<string, string>()
            };

            context.AddEvent(new VotingStartedEventDto(
                room.RoomCode, "System", null, null, SpyVotingType.Final, SpyGamePhase.FinalVote, room.GameState.ActiveVoting.VotingEndsAt));

            context.AddEvent(new ScheduleTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null, votingDuration));
        }
    }
}
