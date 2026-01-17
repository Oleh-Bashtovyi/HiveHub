namespace HiveHub.Domain.Models.Shared;

public enum TimerStatus
{
    Stopped,
    Running,
    Paused
}

public sealed class TimerState
{
    public TimerStatus Status { get; private set; } = TimerStatus.Stopped;
    public DateTime? TimerStartedAt { get; private set; }
    public DateTime? TimerWillStopAt { get; private set; }
    public DateTime? TimerPausedAt { get; private set; }

    public bool IsTimeUp => GetRemainingSeconds() <= 0.001;
    public bool IsFinished => Status == TimerStatus.Stopped || IsTimeUp;
    public bool IsRunning => Status == TimerStatus.Running;
    public bool IsPaused => Status == TimerStatus.Paused;
    public bool IsStopped => Status == TimerStatus.Stopped;

    public void Start(TimeSpan duration, DateTime? now = null)
    {
        var currentTime = now ?? DateTime.UtcNow;

        Status = TimerStatus.Running;
        TimerStartedAt = currentTime;
        TimerPausedAt = null;
        TimerWillStopAt = currentTime.Add(duration);
    }

    public void Stop()
    {
        Status = TimerStatus.Stopped;
        TimerStartedAt = null;
        TimerWillStopAt = null;
        TimerPausedAt = null;
    }

    public void Pause(DateTime? now = null)
    {
        if (Status != TimerStatus.Running) 
            return;

        var currentTime = now ?? DateTime.UtcNow;

        Status = TimerStatus.Paused;
        TimerPausedAt = currentTime;
    }

    public void Resume(DateTime? now = null)
    {
        if (Status != TimerStatus.Paused || !TimerPausedAt.HasValue || !TimerWillStopAt.HasValue) 
            return;

        var currentTime = now ?? DateTime.UtcNow;
        var timeSpentPaused = currentTime - TimerPausedAt.Value;

        TimerWillStopAt = TimerWillStopAt.Value.Add(timeSpentPaused);

        Status = TimerStatus.Running;
        TimerPausedAt = null;
    }

    public double GetRemainingSeconds(DateTime? now = null)
    {
        if (Status == TimerStatus.Stopped || !TimerWillStopAt.HasValue)
        {
            return 0;
        }

        var currentTime = now ?? DateTime.UtcNow;
        TimeSpan remaining;

        if (Status == TimerStatus.Paused && TimerPausedAt.HasValue)
        {
            remaining = TimerWillStopAt.Value - TimerPausedAt.Value;
        }
        else
        {
            remaining = TimerWillStopAt.Value - currentTime;
        }

        return Math.Max(0, remaining.TotalSeconds);
    }
}