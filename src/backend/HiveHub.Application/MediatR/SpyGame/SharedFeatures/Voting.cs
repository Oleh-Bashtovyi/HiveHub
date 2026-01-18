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
        SpyGameEventsContext context,
        bool isTimeUp = false)
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
            votingResolved = ResolveAccusation(room, accState, totalPlayers, requiredVotes, context, isTimeUp);
        }
        else if (room.GameState.CurrentPhase == SpyGamePhase.FinalVote &&
                 room.GameState.ActiveVoting is GeneralVotingState finalState)
        {
            votingResolved = ResolveFinalVote(room, finalState, totalPlayers, requiredVotes, context, isTimeUp);
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
        SpyGameEventsContext context,
        bool isTimeUp)
    {
        var yesVotes = accState.Votes.Count(v => v.Value == TargetVoteType.Yes);
        var totalCastVotes = accState.Votes.Count;

        if (yesVotes >= requiredVotes)
        {
            // Голосування успішне
            context.AddEvent(new VotingCompletedEventDto(
                RoomCode: room.RoomCode,
                IsSuccess: true,
                VotingType: SpyVotingType.Accusation,
                ResultMessage: "Vote passed"));

            PlayerKick.HandlePlayerKick(room, context, accState.TargetId);
            return true;
        }

        var potentialYesVotes = yesVotes + (totalPlayers - totalCastVotes);
        var isImpossibleToWin = totalCastVotes >= totalPlayers || potentialYesVotes < requiredVotes;

        if (isTimeUp || isImpossibleToWin)
        {
            // Голосування провалилося
            context.AddEvent(new VotingCompletedEventDto(
                RoomCode: room.RoomCode,
                IsSuccess: false,
                VotingType: SpyVotingType.Accusation,
                ResultMessage: isTimeUp ? "Voting time expired" : "Not enough votes"));

            room.GameState.CurrentPhase = SpyGamePhase.Search;

            context.AddEvent(new GamePhaseChangedEventDto(
                RoomCode: room.RoomCode,
                NewPhase: SpyGamePhase.Search,
                PreviousPhase: SpyGamePhase.Accusation));

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
        SpyGameEventsContext context,
        bool isTimeUp)
    {
        if (!isTimeUp && finalState.Votes.Count < totalPlayers)
        {
            return false;
        }

        var validVotes = finalState.Votes
            .Where(x => !string.IsNullOrEmpty(x.Value) && x.Value != "SKIP")
            .GroupBy(x => x.Value!)
            .OrderByDescending(g => g.Count())
            .ToList();

        var explicitSkips = finalState.Votes.Count(x => x.Value == "SKIP" || string.IsNullOrEmpty(x.Value));
        var implicitSkips = isTimeUp ? (totalPlayers - finalState.Votes.Count) : 0;
        var totalSkips = explicitSkips + implicitSkips;

        if (totalSkips == totalPlayers)
        {
            context.AddEvent(new VotingCompletedEventDto(
                RoomCode: room.RoomCode,
                IsSuccess: false,
                VotingType: SpyVotingType.Final,
                ResultMessage: "Everyone skipped"));

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

        if (!validVotes.Any() || validVotes.First().Count() < requiredVotes)
        {
            context.AddEvent(new VotingCompletedEventDto(
                RoomCode: room.RoomCode,
                IsSuccess: false,
                VotingType: SpyVotingType.Final,
                ResultMessage: "Consensus not reached"));

            if (room.IsParanoyaMode())
            {
                RoundEnd.EndGame(
                    room,
                    SpyTeam.Civilians,
                    SpyGameEndReason.ParanoiaSurvived,
                    "Consensus failed, paranoya mode survived.",
                    context);
            }
            else
            {
                RoundEnd.EndGame(
                    room,
                    SpyTeam.Spies,
                    SpyGameEndReason.FinalVoteFailed,
                    "Consensus not reached.",
                    context);
            }
            return true;
        }

        var targetId = validVotes.First().Key;

        context.AddEvent(new VotingCompletedEventDto(
            RoomCode: room.RoomCode,
            IsSuccess: true,
            VotingType: SpyVotingType.Final,
            ResultMessage: "Vote passed"));

        PlayerKick.HandlePlayerKick(room, context, targetId);
        return true;
    }

    public static void HandleVotingTimeUp(SpyRoom room, SpyGameEventsContext context)
    {
        if (room.GameState.ActiveVoting == null)
        {
            return;
        }

        CheckAndResolveVoting(room, context, isTimeUp: true);
    }
}