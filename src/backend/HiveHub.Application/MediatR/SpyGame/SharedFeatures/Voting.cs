using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public static class Voting
{
    public static void CheckAndResolveVoting(
        SpyRoom room,
        SpyGameEventsContext context)
    {
        if (room.GameState.ActiveVoting == null)
        {
            return;
        }

        var totalPlayers = room.Players.Count(p => p.IsConnected && !p.PlayerState.IsDead);

        if (totalPlayers == 0)
        {
            return;
        }

        var requiredVotes = room.GetMajorityRequiredVotes();

        var votingResolved = false;

        if (room.GameState.CurrentPhase == SpyGamePhase.Accusation &&
            room.GameState.ActiveVoting is AccusationVotingState accState)
        {
            votingResolved = ResolveAccusation(room, accState, totalPlayers, requiredVotes, context);
        }
        else if (room.GameState.CurrentPhase == SpyGamePhase.FinalVote &&
                 room.GameState.ActiveVoting is GeneralVotingState finalState)
        {
            votingResolved = ResolveFinalVote(room, finalState, totalPlayers, requiredVotes, context);
        }

        if (votingResolved)
        {
            room.GameState.ActiveVoting = null;
            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null));

            RoundEnd.CheckGameEndConditions(room, context);
        }
    }

    private static bool ResolveAccusation(
        SpyRoom room,
        AccusationVotingState accState,
        int totalPlayers,
        int requiredVotes,
        SpyGameEventsContext context)
    {
        var yesVotes = accState.Votes.Count(v => v.Value == TargetVoteType.Yes);
        var totalCastVotes = accState.Votes.Count;

        // Majority reached
        if (yesVotes >= requiredVotes)
        {
            PlayerKick.HandlePlayerKick(room, context, accState.TargetId);
            return true;
        }

        // Everyone voted or impossible to reach majority
        var potentialYesVotes = yesVotes + (totalPlayers - totalCastVotes);

        if (totalCastVotes >= totalPlayers || potentialYesVotes < requiredVotes)
        {
            room.GameState.CurrentPhase = SpyGamePhase.Search;

            context.AddEvent(new VotingResultEventDto(
                RoomCode: room.RoomCode,
                IsSuccess: false,
                CurrentGamePhase: SpyGamePhase.Search,
                ResultMessage: "Not enough votes. Game resumes.",
                AccusedId: accState.TargetId,
                IsAccusedSpy: null,
                LastChanceEndsAt: null));

            RoundTimer.ResumeGameTimer(room, context);
            return true;
        }

        return false;
    }

    private static bool ResolveFinalVote(
        SpyRoom room,
        GeneralVotingState finalState,
        int totalPlayers,
        int requiredVotes,
        SpyGameEventsContext context)
    {
        if (finalState.Votes.Count < totalPlayers)
        {
            return false;
        }

        var validVotes = finalState.Votes
            .Where(x => !string.IsNullOrEmpty(x.Value) && x.Value != "SKIP")
            .GroupBy(x => x.Value!)
            .OrderByDescending(g => g.Count())
            .ToList();

        var skipsCount = finalState.Votes.Count(x => x.Value == "SKIP" || string.IsNullOrEmpty(x.Value));

        // Everyone skipped
        if (skipsCount == totalPlayers)
        {
            if (room.IsParanoyaMode())
            {
                RoundEnd.EndGame(
                    room,
                    SpyTeam.Civilians,
                    SpyGameEndReason.ParanoiaSurvived,
                    "Everyone skipped in Paranoia mode. Civilians win!",
                    context);
            }
            else
            {
                RoundEnd.EndGame(
                    room,
                    SpyTeam.Spies,
                    SpyGameEndReason.FinalVoteFailed,
                    "Civilians failed to identify spy.",
                    context);
            }
            return true;
        }

        // No consensus
        if (!validVotes.Any() || validVotes.First().Count() < requiredVotes)
        {
            RoundEnd.EndGame(
                room,
                SpyTeam.Spies,
                SpyGameEndReason.FinalVoteFailed,
                "Consensus not reached.",
                context);
            return true;
        }

        // Target selected
        var targetId = validVotes.First().Key;
        PlayerKick.HandlePlayerKick(room, context, targetId);
        return true;
    }

    public static void HandleVotingTimeUp(SpyRoom room, SpyGameEventsContext context)
    {
        if (room.GameState.ActiveVoting == null)
        {
            return;
        }

        // Auto-resolve based on current votes
        CheckAndResolveVoting(room, context);
    }
}
