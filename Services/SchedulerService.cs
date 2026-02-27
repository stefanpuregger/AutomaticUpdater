using System.Threading;
using AutomaticUpdater.Models;

namespace AutomaticUpdater.Services;

public class SchedulerService : IDisposable
{
    private readonly UpdaterService _updaterService;
    private readonly CancellationTokenSource _cts;
    private AppSettings _settings;
    private System.Threading.Timer? _timer;
    private bool _disposed;

    public event EventHandler? ScheduleChanged;

    public DateTime? NextRunTime { get; private set; }

    public SchedulerService(UpdaterService updaterService, AppSettings settings, CancellationTokenSource cts)
    {
        _updaterService = updaterService;
        _settings = settings;
        _cts = cts;
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
    }

    public void Start()
    {
        Reschedule();
    }

    public void Reschedule()
    {
        _timer?.Dispose();
        _timer = null;

        if (!_settings.Schedule.Enabled)
        {
            NextRunTime = null;
            ScheduleChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        NextRunTime = CalculateNextRun(_settings.Schedule);
        ScheduleChanged?.Invoke(this, EventArgs.Empty);

        if (NextRunTime is null) return;

        TimeSpan delay = NextRunTime.Value - DateTime.Now;
        if (delay < TimeSpan.Zero)
            delay = TimeSpan.Zero;

        _timer = new System.Threading.Timer(OnTimerFired, null, delay, Timeout.InfiniteTimeSpan);
    }

    private async void OnTimerFired(object? state)
    {
        try
        {
            await _updaterService.RunUpdateAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Scheduler] Unhandled exception in timer: {ex.Message}");
        }
        finally
        {
            if (!_cts.IsCancellationRequested)
                Reschedule();
        }
    }

    private static DateTime? CalculateNextRun(UpdateSchedule schedule)
    {
        var now = DateTime.Now;
        var candidate = new DateTime(now.Year, now.Month, now.Day, schedule.Hour, schedule.Minute, 0);

        if (schedule.Frequency == ScheduleFrequency.Daily)
        {
            if (candidate <= now)
                candidate = candidate.AddDays(1);
            return candidate;
        }
        else // Weekly
        {
            // Find next occurrence of the target day-of-week
            int daysUntil = ((int)schedule.DayOfWeek - (int)candidate.DayOfWeek + 7) % 7;
            candidate = candidate.AddDays(daysUntil);

            if (candidate <= now)
                candidate = candidate.AddDays(7);

            return candidate;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _timer?.Dispose();
            _disposed = true;
        }
    }
}
