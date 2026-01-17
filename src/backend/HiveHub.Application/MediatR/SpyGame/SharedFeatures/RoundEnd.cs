using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;

public static class RoundEnd
{
    public static void BeginFinalVoting(SpyRoom room, SpyGameEventsContext context)
    {
        room.GameState.RoundTimerState.Stop();
        room.GameState.CurrentPhase = SpyGamePhase.FinalVote;
        room.GameState.CaughtSpyId = null;

        context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, room.RoomCode, null));
        context.AddEvent(new CancelTaskEvent(TaskType.SpyGameLastChanceTimeUp, room.RoomCode, null));

        context.AddEvent(new SpyGameRoundTimerStateChangedEventDto(
            RoomCode: room.RoomCode,
            TimerStatus: room.GameState.RoundTimerState.Status,
            RemainingSeconds: 0));

        var votingDuration = TimeSpan.FromSeconds(ProjectConstants.SpyGame.FinalVoteDurationSeconds);

        room.GameState.ActiveVoting = new GeneralVotingState
        {
            VotingStartedAt = DateTime.UtcNow,
            VotingEndsAt = DateTime.UtcNow.Add(votingDuration),
            Votes = new Dictionary<string, string?>()
        };

        context.AddEvent(new VotingStartedEventDto(
            RoomCode: room.RoomCode,
            InitiatorId: "System",
            TargetId: null,
            TargetName: null,
            VotingType: SpyVotingType.Final,
            CurrentGamePhase: SpyGamePhase.FinalVote,
            EndsAt: room.GameState.ActiveVoting.VotingEndsAt));

        context.AddEvent(new ScheduleTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null, votingDuration));
    }

    public static void EndGame(
        SpyRoom room,
        SpyTeam winner,
        SpyGameEndReason reason,
        string message,
        SpyGameEventsContext context)
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

        context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, room.RoomCode, null));
        context.AddEvent(new CancelTaskEvent(TaskType.SpyGameVotingTimeUp, room.RoomCode, null));
        context.AddEvent(new CancelTaskEvent(TaskType.SpyGameLastChanceTimeUp, room.RoomCode, null));
    }

    public static void CheckGameEndConditions(SpyRoom room, SpyGameEventsContext context)
    {
        if (!room.IsInGame())
        {
            return;
        }

        var connectedPlayers = room.Players.Count(p => p.IsConnected);
        var alivePlayers = room.Players.Count(p => !p.PlayerState.IsDead && p.IsConnected);
        var aliveSpies = room.Players.Count(p => p.PlayerState.IsSpy && !p.PlayerState.IsDead && p.IsConnected);
        var aliveCivilians = room.Players.Count(p => !p.PlayerState.IsSpy && !p.PlayerState.IsDead && p.IsConnected);

        // Not enough players to continue
        if (connectedPlayers < 2 || alivePlayers < 2)
        {
            EndGame(room, SpyTeam.Spies, SpyGameEndReason.InsufficientPlayers, "Not enough players to continue.", context);
            return;
        }

        // 1v1 scenario with spy
        if (alivePlayers == 2 && aliveSpies >= 1)
        {
            EndGame(room, SpyTeam.Spies, SpyGameEndReason.RoundTimeExpired, "Spies control the room (1v1).", context);
            return;
        }

        // All spies eliminated
        if (aliveSpies == 0 && room.GameState.SpiesCountSnapshot > 0)
        {
            EndGame(room, SpyTeam.Civilians, SpyGameEndReason.AllSpiesEliminated, "All spies eliminated.", context);
            return;
        }
    }
}