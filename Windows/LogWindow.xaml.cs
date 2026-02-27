using System.Text;
using System.Windows;
using AutomaticUpdater.Services;

namespace AutomaticUpdater.Windows;

public partial class LogWindow : Window
{
    private readonly UpdaterService _updater;

    public LogWindow()
    {
        InitializeComponent();

        _updater = TrayApplication.Instance.UpdaterService;
        _updater.LogLineAdded += OnLogLineAdded;

        Closed += OnWindowClosed;

        LoadLog();
    }

    private void LoadLog()
    {
        var lines = TrayApplication.Instance.SettingsService.GetLogLines();
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            sb.AppendLine(line);
        }
        TxtLog.Text = sb.ToString();
        ScrollToBottom();
        UpdateStatus();
    }

    private void OnLogLineAdded(object? sender, string line)
    {
        Dispatcher.InvokeAsync(() =>
        {
            TxtLog.AppendText(line + Environment.NewLine);
            ScrollToBottom();
            UpdateStatus();
        });
    }

    private void ScrollToBottom()
    {
        TxtLog.ScrollToEnd();
    }

    private void UpdateStatus()
    {
        int lineCount = TxtLog.LineCount;
        TxtStatus.Text = $"Lines: {lineCount}  |  Last updated: {DateTime.Now:HH:mm:ss}";
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        LoadLog();
    }

    private void OnClearLog(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Clear the update log? This cannot be undone.",
            "Clear Log",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            TrayApplication.Instance.SettingsService.ClearLog();
            TxtLog.Clear();
            UpdateStatus();
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _updater.LogLineAdded -= OnLogLineAdded;
    }
}
