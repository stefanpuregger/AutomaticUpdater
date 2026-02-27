using System.Windows;
using System.Windows.Controls;
using AutomaticUpdater.Models;

namespace AutomaticUpdater.Windows;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        PopulateComboBoxes();
        LoadSettings();
    }

    private void PopulateComboBoxes()
    {
        // Days of week
        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            CbDayOfWeek.Items.Add(day.ToString());

        // Hours 00–23
        for (int h = 0; h < 24; h++)
            CbHour.Items.Add(h.ToString("D2"));

        // Minutes 00, 05, 10, …, 55
        for (int m = 0; m < 60; m += 5)
            CbMinute.Items.Add(m.ToString("D2"));
    }

    private void LoadSettings()
    {
        var settings = TrayApplication.Instance.Settings;
        var schedule = settings.Schedule;

        ChkAutostart.IsChecked = TrayApplication.Instance.AutostartService.IsEnabled;
        ChkScheduleEnabled.IsChecked = schedule.Enabled;

        RbDaily.IsChecked = schedule.Frequency == ScheduleFrequency.Daily;
        RbWeekly.IsChecked = schedule.Frequency == ScheduleFrequency.Weekly;

        CbDayOfWeek.SelectedItem = schedule.DayOfWeek.ToString();
        CbHour.SelectedItem = schedule.Hour.ToString("D2");
        CbMinute.SelectedItem = (schedule.Minute / 5 * 5).ToString("D2");

        UpdateScheduleOptionsVisibility();
        UpdateDayOfWeekVisibility();
    }

    private void OnScheduleEnabledChanged(object sender, RoutedEventArgs e)
    {
        UpdateScheduleOptionsVisibility();
    }

    private void UpdateScheduleOptionsVisibility()
    {
        bool enabled = ChkScheduleEnabled.IsChecked == true;
        PnlScheduleOptions.IsEnabled = enabled;
        PnlScheduleOptions.Opacity = enabled ? 1.0 : 0.5;
    }

    private void OnFrequencyChanged(object sender, RoutedEventArgs e)
    {
        UpdateDayOfWeekVisibility();
    }

    private void UpdateDayOfWeekVisibility()
    {
        PnlDayOfWeek.Visibility = RbWeekly.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var tray = TrayApplication.Instance;
        var settings = tray.Settings;
        var schedule = settings.Schedule;

        // Autostart
        bool autostartEnabled = ChkAutostart.IsChecked == true;
        tray.AutostartService.SetEnabled(autostartEnabled);
        settings.AutostartEnabled = autostartEnabled;

        // Schedule
        schedule.Enabled = ChkScheduleEnabled.IsChecked == true;
        schedule.Frequency = RbWeekly.IsChecked == true
            ? ScheduleFrequency.Weekly
            : ScheduleFrequency.Daily;

        if (CbDayOfWeek.SelectedItem is string dayStr &&
            Enum.TryParse<DayOfWeek>(dayStr, out var day))
        {
            schedule.DayOfWeek = day;
        }

        if (CbHour.SelectedItem is string hourStr && int.TryParse(hourStr, out int hour))
            schedule.Hour = hour;

        if (CbMinute.SelectedItem is string minStr && int.TryParse(minStr, out int minute))
            schedule.Minute = minute;

        tray.SettingsService.Save(settings);
        tray.UpdaterService.UpdateSettings(settings);
        tray.SchedulerService.UpdateSettings(settings);
        tray.SchedulerService.Reschedule();
        tray.RefreshMenuLabels();

        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
