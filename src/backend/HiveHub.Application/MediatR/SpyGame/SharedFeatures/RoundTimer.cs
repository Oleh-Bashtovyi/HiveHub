using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Domain.Models.SpyGame;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public static class RoundTimer
{
    public static void CheckAndResolveTimerStop(SpyRoom room, SpyGameEventsContext context)
    {
        if (room.GameState.RoundTimerState.IsStopped || !room.IsInGame())
        {
            return;
        }

        var votesCount = room.CountVotedForTimerStop();
        var requiredVotes = room.GetMajorityRequiredVotes();

        if (votesCount >= requiredVotes)
        {
            RoundEnd.BeginFinalVoting(room, context);
        }
    }

    public static void ResumeGameTimer(SpyRoom room, SpyGameEventsContext context)
    {
        if (room.GameState.RoundTimerState.IsPaused)
        {
            room.GameState.RoundTimerState.Resume();
            var remaining = room.GameState.RoundTimerState.GetRemainingSeconds();

            context.AddEvent(new ScheduleTaskEvent(
                Type: TaskType.SpyGameRoundTimeUp,
                RoomCode: room.RoomCode,
                TargetId: null,
                Delay: TimeSpan.FromSeconds(remaining)));

            context.AddEvent(new SpyGameRoundTimerStateChangedEventDto(
                RoomCode: room.RoomCode,
                Status: room.GameState.RoundTimerState.Status,
                RemainingSeconds: remaining,
                Reason: TimerChangeReason.Resumed));
        }
    }

    public static void HandleRoundTimeUp(SpyRoom room, SpyGameEventsContext context)
    {
        if (!room.IsInGame() || room.GameState.CurrentPhase != SpyGamePhase.Search)
        {
            return;
        }

        if (room.IsParanoyaMode())
        {
            RoundEnd.EndGame(
                room,
                SpyTeam.Civilians,
                SpyGameEndReason.ParanoiaSurvived,
                "Civilians survived in Paranoia mode!",
                context);
            return;
        }

        RoundEnd.BeginFinalVoting(room, context);
    }

    public static void HandleLastChanceTimeUp(SpyRoom room, SpyGameEventsContext context, string spyId)
    {
        if (!room.IsInGame() || room.GameState.CurrentPhase != SpyGamePhase.SpyLastChance)
        {
            return;
        }

        if (room.GameState.CaughtSpyId != spyId)
        {
            return;
        }

        MakeGuess.HandleSpyGuess(room, context, spyId, isGuessCorrect: false);
    }
}