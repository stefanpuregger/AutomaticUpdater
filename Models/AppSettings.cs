namespace AutomaticUpdater.Models;

public enum ScheduleFrequency
{
    Daily,
    Weekly
}

public class UpdateSchedule
{
    public bool Enabled { get; set; } = true;
    public ScheduleFrequency Frequency { get; set; } = ScheduleFrequency.Daily;
    public int Hour { get; set; } = 3;
    public int Minute { get; set; } = 0;
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Sunday;
}

public class AppSettings
{
    public bool AutostartEnabled { get; set; } = true;
    public UpdateSchedule Schedule { get; set; } = new UpdateSchedule();
    public DateTime? LastRunUtc { get; set; } = null;
    public int LogMaxLines { get; set; } = 1000;
}
