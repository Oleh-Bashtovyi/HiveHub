using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public static class SpyGameLogicHelper
{
    public static async Task CheckAndResolveVoting(
        SpyRoom room,
        ISpyGamePublisher publisher,
        ITaskScheduler scheduler,
        ISpyGameRepository repository,
        ILogger logger)
    {
        if (room.ActiveVoting == null) return;

        var activePlayers = room.Players.Where(p => p.IsConnected).ToList();
        if (activePlayers.Count == 0) return;

        // Recalculate required votes based on currently connected players
        int requiredVotes = (int)Math.Floor(activePlayers.Count / 2.0) + 1;

        VotingResultEventDto? resultDto = null;
        SpyGameEndedEventDto? gameEndedDto = null;
        ScheduledTask? timerResumeTask = null;
        TimeSpan? timerResumeDelay = null;
        bool votingResolved = false;

        // Handle Accusation Logic
        if (room.CurrentPhase == SpyGamePhase.Accusation && room.ActiveVoting is AccusationVotingState accState)
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
                    room.CurrentPhase = SpyGamePhase.SpyLastChance;
                    room.CaughtSpyId = accused.IdInRoom;

                    logger.LogInformation("Voting Passed: Spy {SpyId} caught in room {RoomCode}", accused.IdInRoom, room.RoomCode);

                    resultDto = new VotingResultEventDto(
                        RoomCode: room.RoomCode,
                        IsSuccess: true,
                        CurrentGamePhase: SpyGamePhase.SpyLastChance,
                        ResultMessage: $"Spy {accused.Name} caught! They have a last chance to guess the location.",
                        AccusedId: accused.IdInRoom);
                }
                else
                {
                    // Civilian Kicked -> Spies Win
                    room.Status = RoomStatus.Ended;
                    room.WinnerTeam = Team.Spies;
                    room.GameEndReason = GameEndReason.CivilianKicked;

                    logger.LogInformation("Voting Passed: Innocent {PlayerId} kicked in room {RoomCode}. Spies win.", accState.TargetId, room.RoomCode);

                    resultDto = new VotingResultEventDto(
                        RoomCode: room.RoomCode,
                        IsSuccess: true,
                        CurrentGamePhase: SpyGamePhase.None,
                        ResultMessage: $"Player {accused?.Name} was NOT a spy. Spies win!",
                        AccusedId: accState.TargetId);

                    gameEndedDto = new SpyGameEndedEventDto(room.RoomCode, Team.Spies, GameEndReason.CivilianKicked, "Innocent player kicked");
                }
            }
            // Resolution: Failure (Everyone voted, but not enough Yes, OR strictly impossible to reach majority)
            else if (totalVotes >= activePlayers.Count)
            {
                votingResolved = true;
                room.CurrentPhase = SpyGamePhase.Search;

                logger.LogInformation("Voting Failed: Not enough votes in room {RoomCode}. Resuming game.", room.RoomCode);

                resultDto = new VotingResultEventDto(
                    RoomCode: room.RoomCode,
                    IsSuccess: false,
                    CurrentGamePhase: SpyGamePhase.Search,
                    ResultMessage: "Not enough votes. Game resumes.",
                    AccusedId: null);

                // Resume Timer Logic
                if (room.TimerState.TimerStoppedAt.HasValue && room.TimerState.PlannedGameEndTime.HasValue)
                {
                    var timeSpentPaused = DateTime.UtcNow - room.TimerState.TimerStoppedAt.Value;
                    room.TimerState.PlannedGameEndTime = room.TimerState.PlannedGameEndTime.Value.Add(timeSpentPaused);
                    room.TimerState.IsTimerStopped = false;
                    room.TimerState.TimerStoppedAt = null;

                    var remaining = room.TimerState.PlannedGameEndTime.Value - DateTime.UtcNow;
                    timerResumeDelay = remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
                    timerResumeTask = new ScheduledTask(TaskType.SpyGameEndTimeUp, room.RoomCode, null);
                }
            }
        }
        // Final Vote Logic
        else if (room.CurrentPhase == SpyGamePhase.FinalVote && room.ActiveVoting is GeneralVotingState finalState)
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
                    room.WinnerTeam = Team.Spies;
                    room.GameEndReason = GameEndReason.FinalVotingFailed;

                    logger.LogInformation("Final Voting Failed: Consensus not reached in room {RoomCode}.", room.RoomCode);

                    resultDto = new VotingResultEventDto(
                        RoomCode: room.RoomCode,
                        IsSuccess: false,
                        CurrentGamePhase: SpyGamePhase.None,
                        ResultMessage: "Consensus not reached. Spies win!",
                        AccusedId: null);

                    gameEndedDto = new SpyGameEndedEventDto(room.RoomCode, Team.Spies, GameEndReason.FinalVotingFailed, "Civilians failed to agree on a suspect");
                }
                else
                {
                    // Target Selected
                    var targetId = groupedVotes.First().Key;
                    var target = room.Players.FirstOrDefault(p => p.IdInRoom == targetId);

                    if (target != null && target.PlayerState.IsSpy)
                    {
                        // Spy Found -> Last Chance
                        room.CurrentPhase = SpyGamePhase.SpyLastChance;
                        room.CaughtSpyId = targetId;

                        logger.LogInformation("Final Voting Success: Spy {SpyId} identified in room {RoomCode}.", targetId, room.RoomCode);

                        resultDto = new VotingResultEventDto(
                            RoomCode: room.RoomCode,
                            IsSuccess: true,
                            CurrentGamePhase: SpyGamePhase.SpyLastChance,
                            ResultMessage: "Spy identified! Last chance to guess.",
                            AccusedId: targetId);
                    }
                    else
                    {
                        // Wrong Target -> Spies Win
                        room.Status = RoomStatus.Ended;
                        room.WinnerTeam = Team.Spies;
                        room.GameEndReason = GameEndReason.CivilianKicked;

                        logger.LogInformation("Final Voting Fail: Wrong target {TargetId} in room {RoomCode}.", targetId, room.RoomCode);

                        resultDto = new VotingResultEventDto(
                            RoomCode: room.RoomCode,
                            IsSuccess: true,
                            CurrentGamePhase: SpyGamePhase.None,
                            ResultMessage: $"Wrong choice! {target?.Name} is innocent. Spies win!",
                            AccusedId: targetId);

                        gameEndedDto = new SpyGameEndedEventDto(room.RoomCode, Team.Spies, GameEndReason.CivilianKicked, "Civilians voted for an innocent player");
                    }
                }
            }
        }

        // Apply Side Effects
        if (votingResolved)
        {
            room.ActiveVoting = null;
            await scheduler.CancelAsync(new ScheduledTask(TaskType.SpyVotingTimeUp, room.RoomCode, null));

            if (resultDto != null) await publisher.PublishVotingResultAsync(resultDto);
            if (gameEndedDto != null) await publisher.PublishGameEndedAsync(gameEndedDto);

            if (timerResumeTask != null && timerResumeDelay.HasValue)
            {
                await scheduler.ScheduleAsync(timerResumeTask, timerResumeDelay.Value);
            }
        }
    }

    public static async Task CheckAndResolveTimerStop(
        SpyRoom room,
        ISpyGamePublisher publisher,
        ITaskScheduler scheduler,
        ILogger logger)
    {
        // Only check if timer is running
        if (room.TimerState.IsTimerStopped || !room.IsInGame()) return;

        var votesCount = room.Players.Count(p => p.PlayerState.VotedToStopTimer && p.IsConnected);
        var activePlayers = room.Players.Count(p => p.IsConnected);
        var requiredVotes = (int)Math.Ceiling(activePlayers / 2.0);
        if (requiredVotes < 1) requiredVotes = 1;

        // If threshold met due to player leaving/disconnecting
        if (votesCount >= requiredVotes)
        {
            room.TimerState.IsTimerStopped = true;
            room.TimerState.TimerStoppedAt = DateTime.UtcNow;

            logger.LogInformation("Timer stopped automatically in room {RoomCode} (Votes: {Votes}/{Req})", room.RoomCode, votesCount, requiredVotes);

            var timerTask = new ScheduledTask(TaskType.SpyGameEndTimeUp, room.RoomCode, null);
            await scheduler.CancelAsync(timerTask);

            // Notify everyone
            var eventDto = new PlayerVotedToStopTimerEventDto(room.RoomCode, "System", votesCount, requiredVotes);
            await publisher.PublishTimerVoteAsync(eventDto);
        }
    }
}