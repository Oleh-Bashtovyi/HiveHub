namespace HiveHub.Domain.Models.Shared;

public sealed class TimerState
{
    public bool IsTimerStopped { get; private set; } = true;
    public DateTime? TimerStartedAt { get; private set; }
    public DateTime? TimerWillStopAt { get; private set; }
    public DateTime? TimerPausedAt { get; private set; }

    public bool IsRunning() => !IsTimerStopped;
    public bool IsPaused() => IsTimerStopped && TimerPausedAt.HasValue;
    public bool IsStopped() => IsTimerStopped && !TimerPausedAt.HasValue;


    public void Start(TimeSpan duration, DateTime? now = null)
    {
        now = now ?? DateTime.UtcNow;
        IsTimerStopped = false;
        TimerPausedAt = null;
        TimerStartedAt = now;
        TimerWillStopAt = now.Value.Add(duration);
    }

    public void Clear()
    {
        IsTimerStopped = true;
        TimerPausedAt = null;
        TimerStartedAt = null;
        TimerWillStopAt = null;
    }

    public void Pause()
    {
        if (IsTimerStopped) return;

        IsTimerStopped = true;
        TimerPausedAt = DateTime.UtcNow;
    }

    public void Resume()
    {
        if (!IsTimerStopped || !TimerPausedAt.HasValue || !TimerWillStopAt.HasValue) return;

        var now = DateTime.UtcNow;

        var timeSpentPaused = now - TimerPausedAt.Value;

        TimerWillStopAt = TimerWillStopAt.Value.Add(timeSpentPaused);

        IsTimerStopped = false;
        TimerPausedAt = null;
    }

    public double GetRemainingSeconds()
    {
        if (!TimerWillStopAt.HasValue)
        {
            return 0;
        }

        TimeSpan remaining;

        if (IsTimerStopped && TimerPausedAt.HasValue)
        {
            remaining = TimerWillStopAt.Value - TimerPausedAt.Value;
        }
        else
        {
            remaining = TimerWillStopAt.Value - DateTime.UtcNow;
        }

        return remaining.TotalSeconds > 0 ? remaining.TotalSeconds : 0;
    }
}