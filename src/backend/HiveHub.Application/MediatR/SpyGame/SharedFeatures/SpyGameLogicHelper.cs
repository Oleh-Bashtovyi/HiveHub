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
        if (room.GameState.ActiveVoting == null) return;

        var activePlayers = room.Players.Where(p => p.IsConnected).ToList();
        if (activePlayers.Count == 0) return;

        // Recalculate required votes based on currently connected players
        int requiredVotes = (int)Math.Floor(activePlayers.Count / 2.0) + 1;

        bool votingResolved = false;

        // Handle Accusation Logic
        if (room.GameState.CurrentPhase == SpyGamePhase.Accusation && room.GameState.ActiveVoting is AccusationVotingState accState)
        {
            var yesVotes = accState.Votes.Count(v => v.Value == TargetVoteType.Yes);
            // Check if we have enough YES votes OR if everyone has voted (even if not enough yes, it fails)
            var totalVotes = accState.Votes.Count;

            // Success
            if (yesVotes >= requiredVotes)
            {
                votingResolved = true;
                var accused = room.Players.FirstOrDefault(p => p.IdInRoom == accState.TargetId);

                if (accused != null && accused.PlayerState.IsSpy)
                {
                    // Spy Caught -> Last Chance
                    var finalGuessingDuration = TimeSpan.FromSeconds(ProjectConstants.SpyGame.FinacGuessingChanceDurationSeconds);
                    var now = DateTime.UtcNow;

                    room.GameState.CurrentPhase = SpyGamePhase.SpyLastChance;
                    room.GameState.CaughtSpyId = accused.IdInRoom;
                    room.GameState.SpyLastChanceEndsAt = now.Add(finalGuessingDuration);

                    logger.LogInformation("Voting Passed: Spy {SpyId} caught in room {RoomCode}", accused.IdInRoom, room.RoomCode);

                    context.AddEvent(new CancelTaskEvent(
                        Type: TaskType.SpyGameRoundTimeUp,
                        RoomCode: room.RoomCode,
                        TargetId: null));

                    context.AddEvent(new VotingResultEventDto(
                        RoomCode: room.RoomCode,
                        IsSuccess: true,
                        CurrentGamePhase: SpyGamePhase.SpyLastChance,
                        ResultMessage: $"Spy {accused.Name} caught! They have a last chance to guess the location.",
                        LastChanceEndsAt: room.GameState.SpyLastChanceEndsAt,
                        IsAccusedSpy: true,
                        AccusedId: accused.IdInRoom));
                }
                else
                {
                    // Civilian Kicked -> Spies Win
                    room.Status = RoomStatus.Ended;
                    room.GameState.WinnerTeam = SpyTeam.Spies;
                    room.GameState.GameEndReason = SpyGameEndReason.CivilianKicked;

                    logger.LogInformation("Voting Passed: Innocent {PlayerId} kicked in room {RoomCode}. Spies win.", 
                        accState.TargetId, room.RoomCode);

                    context.AddEvent(new VotingResultEventDto(
                        RoomCode: room.RoomCode,
                        IsSuccess: true,
                        CurrentGamePhase: SpyGamePhase.None,
                        ResultMessage: $"Player {accused?.Name} was NOT a spy. Spies win!",
                        LastChanceEndsAt: null,
                        IsAccusedSpy: false,
                        AccusedId: accState.TargetId));

                    context.AddEvent(new SpyGameEndedEventDto(
                        RoomCode: room.RoomCode, 
                        WinnerTeam: SpyTeam.Spies,
                        Reason: SpyGameEndReason.CivilianKicked,
                        SpiesReveal: room.GetSpyRevealDto(),
                        ReasonMessage: "Innocent player kicked"));
                }
            }
            // Resolution: Failure (Everyone voted, but not enough Yes, OR strictly impossible to reach majority)
            else if (totalVotes >= activePlayers.Count)
            {
                votingResolved = true;
                room.GameState.CurrentPhase = SpyGamePhase.Search;

                logger.LogInformation("Voting Failed: Not enough votes in room {RoomCode}. Resuming game.", room.RoomCode);

                context.AddEvent(new VotingResultEventDto(
                    RoomCode: room.RoomCode,
                    IsSuccess: false,
                    CurrentGamePhase: SpyGamePhase.Search,
                    ResultMessage: "Not enough votes. Game resumes.",
                    IsAccusedSpy: null,
                    LastChanceEndsAt: null,
                    AccusedId: accState.TargetId));

                // Resume Timer Logic
                if (room.GameState.RoundTimerState.IsTimerStopped)
                {
                    room.GameState.RoundTimerState.Resume();

                    var remaining = room.GameState.RoundTimerState.GetRemainingSeconds();
                    var timerResumeDelay = TimeSpan.FromSeconds(remaining);

                    context.AddEvent(new ScheduleTaskEvent(
                        Type: TaskType.SpyGameRoundTimeUp,
                        RoomCode: room.RoomCode,
                        TargetId: null,
                        Delay: timerResumeDelay));
                }
            }
        }
        // Final Vote Logic
        else if (room.GameState.CurrentPhase == SpyGamePhase.FinalVote && room.GameState.ActiveVoting is GeneralVotingState finalState)
        {
            var totalVotes = finalState.Votes.Count;
            // Only resolve final vote if EVERYONE (connected) has voted.
            if (totalVotes >= activePlayers.Count)
            {
                votingResolved = true;

                var groupedVotes = finalState.Votes
                    .Where(x => !string.IsNullOrEmpty(x.Value))
                    .GroupBy(x => x.Value!)
                    .OrderByDescending(g => g.Count())
                    .ToList();

                if (!groupedVotes.Any() || groupedVotes.First().Count() < requiredVotes)
                {
                    // Consensus Failed -> Spies Win
                    room.Status = RoomStatus.Ended;
                    room.GameState.WinnerTeam = SpyTeam.Spies;
                    room.GameState.GameEndReason = SpyGameEndReason.FinalVotingFailed;

                    logger.LogInformation("Final Voting Failed: Consensus not reached in room {RoomCode}.", room.RoomCode);

                    context.AddEvent(new VotingResultEventDto(
                        RoomCode: room.RoomCode,
                        IsSuccess: false,
                        CurrentGamePhase: SpyGamePhase.None,
                        ResultMessage: "Consensus not reached. Spies win!",
                        IsAccusedSpy: null,
                        LastChanceEndsAt: null,
                        AccusedId: null));

                    context.AddEvent(new SpyGameEndedEventDto(
                        RoomCode: room.RoomCode, 
                        WinnerTeam: SpyTeam.Spies, 
                        Reason: SpyGameEndReason.FinalVotingFailed,
                        SpiesReveal: room.GetSpyRevealDto(),
                        ReasonMessage: "Civilians failed to agree on a suspect"));
                }
                else
                {
                    // Target Selected
                    var targetId = groupedVotes.First().Key;
                    var target = room.Players.FirstOrDefault(p => p.IdInRoom == targetId);

                    if (target != null && target.PlayerState.IsSpy)
                    {
                        // Spy Found -> Last Chance
                        var finalGuessingDuration = TimeSpan.FromSeconds(ProjectConstants.SpyGame.FinacGuessingChanceDurationSeconds);
                        var now = DateTime.UtcNow;

                        room.GameState.CurrentPhase = SpyGamePhase.SpyLastChance;
                        room.GameState.CaughtSpyId = targetId;
                        room.GameState.SpyLastChanceEndsAt = now.Add(finalGuessingDuration);

                        logger.LogInformation("Final Voting Success: Spy {SpyId} identified in room {RoomCode}.", targetId, room.RoomCode);

                        context.AddEvent(new VotingResultEventDto(
                            RoomCode: room.RoomCode,
                            IsSuccess: true,
                            CurrentGamePhase: SpyGamePhase.SpyLastChance,
                            ResultMessage: "Spy identified! Last chance to guess.",
                            IsAccusedSpy: true,
                            LastChanceEndsAt: room.GameState.SpyLastChanceEndsAt,
                            AccusedId: targetId));
                    }
                    else
                    {
                        // Wrong Target -> Spies Win
                        room.Status = RoomStatus.Ended;
                        room.GameState.WinnerTeam = SpyTeam.Spies;
                        room.GameState.GameEndReason = SpyGameEndReason.CivilianKicked;

                        logger.LogInformation("Final Voting Fail: Wrong target {TargetId} in room {RoomCode}.", targetId, room.RoomCode);

                        context.AddEvent(new VotingResultEventDto(
                            RoomCode: room.RoomCode,
                            IsSuccess: true,
                            CurrentGamePhase: SpyGamePhase.None,
                            ResultMessage: $"Wrong choice! {target?.Name} is innocent. Spies win!",
                            IsAccusedSpy: false,
                            LastChanceEndsAt: null,
                            AccusedId: targetId));

                        context.AddEvent(new SpyGameEndedEventDto(
                            RoomCode: room.RoomCode, 
                            WinnerTeam: SpyTeam.Spies, 
                            Reason: SpyGameEndReason.CivilianKicked,
                            SpiesReveal: room.GetSpyRevealDto(),
                            ReasonMessage: "Civilians voted for an innocent player"));
                    }
                }
            }
        }

        if (votingResolved)
        {
            room.GameState.ActiveVoting = null;
            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null));
        }
    }

    public static void CheckAndResolveTimerStop(
        SpyRoom room,
        SpyGameEventsContext context,
        ILogger logger)
    {
        // Only check if timer is running
        if (room.GameState.RoundTimerState.IsTimerStopped || !room.IsInGame()) return;

        var votesCount = room.Players.Count(p => p.PlayerState.VotedToStopTimer && p.IsConnected);
        var activePlayers = room.Players.Count(p => p.IsConnected);
        var requiredVotes = (int)Math.Ceiling(activePlayers / 2.0);
        if (requiredVotes < 1) requiredVotes = 1;

        // If threshold met due to player leaving/disconnecting
        if (votesCount >= requiredVotes)
        {
            room.GameState.RoundTimerState.Pause();

            logger.LogInformation("Timer stopped automatically in room {RoomCode} (Votes: {Votes}/{Req})", room.RoomCode, votesCount, requiredVotes);

            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, room.RoomCode, null));
            context.AddEvent(new PlayerVotedToStopTimerEventDto(room.RoomCode, "System", votesCount, requiredVotes));
        }
    }
}
