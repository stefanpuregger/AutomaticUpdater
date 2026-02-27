using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AutomaticUpdater.Models;
using AutomaticUpdater.Services;
using AutomaticUpdater.Windows;
using WinFormsApp = System.Windows.Forms.Application;

namespace AutomaticUpdater;

public class TrayApplication : IDisposable
{
    public static TrayApplication Instance { get; private set; } = null!;

    public AppSettings Settings { get; private set; } = null!;
    public SettingsService SettingsService { get; private set; } = null!;
    public AutostartService AutostartService { get; private set; } = null!;
    public UpdaterService UpdaterService { get; private set; } = null!;
    public SchedulerService SchedulerService { get; private set; } = null!;

    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private ToolStripMenuItem? _nextRunItem;
    private ToolStripMenuItem? _lastRunItem;
    private ToolStripMenuItem? _runNowItem;
    private ToolStripMenuItem? _autostartItem;
    private Icon? _trayIcon;

    private SettingsWindow? _settingsWindow;
    private LogWindow? _logWindow;
    private bool _disposed;

    public void Initialize()
    {
        Instance = this;

        SettingsService = new SettingsService();
        Settings = SettingsService.Load();

        AutostartService = new AutostartService();
        UpdaterService = new UpdaterService(SettingsService, Settings);
        SchedulerService = new SchedulerService(UpdaterService, Settings);

        UpdaterService.UpdateStarted += OnUpdateStarted;
        UpdaterService.UpdateCompleted += OnUpdateCompleted;
        SchedulerService.ScheduleChanged += OnScheduleChanged;

        BuildTrayIcon();

        SchedulerService.Start();
    }

    private void BuildTrayIcon()
    {
        _trayIcon = CreateProgrammaticIcon();

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Renderer = new ToolStripProfessionalRenderer();

        // Header
        var headerItem = new ToolStripMenuItem("AutomaticUpdater")
        {
            Enabled = false,
            Font = new Font(SystemFonts.MenuFont ?? SystemFonts.DefaultFont, FontStyle.Bold)
        };

        // Info labels
        _nextRunItem = new ToolStripMenuItem(GetNextRunText()) { Enabled = false };
        _lastRunItem = new ToolStripMenuItem(GetLastRunText()) { Enabled = false };

        var sep1 = new ToolStripSeparator();

        // Run Now
        _runNowItem = new ToolStripMenuItem("Run Now");
        _runNowItem.Click += async (_, _) => await UpdaterService.RunUpdateAsync();

        // Settings
        var settingsItem = new ToolStripMenuItem("Settings…");
        settingsItem.Click += OnSettingsClicked;

        // View Log
        var logItem = new ToolStripMenuItem("View Log…");
        logItem.Click += OnLogClicked;

        var sep2 = new ToolStripSeparator();

        // Start with Windows
        _autostartItem = new ToolStripMenuItem("Start with Windows")
        {
            Checked = AutostartService.IsEnabled,
            CheckOnClick = true
        };
        _autostartItem.CheckedChanged += OnAutostartCheckedChanged;

        var sep3 = new ToolStripSeparator();

        // Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => System.Windows.Application.Current.Shutdown();

        _contextMenu.Items.AddRange(new ToolStripItem[]
        {
            headerItem,
            _nextRunItem,
            _lastRunItem,
            sep1,
            _runNowItem,
            settingsItem,
            logItem,
            sep2,
            _autostartItem,
            sep3,
            exitItem
        });

        _notifyIcon = new NotifyIcon
        {
            Icon = _trayIcon,
            Text = "AutomaticUpdater",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };

        _notifyIcon.DoubleClick += OnLogClicked;
    }

    private static Icon CreateProgrammaticIcon()
    {
        var bitmap = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Blue filled circle
        var blue = Color.FromArgb(0, 120, 215);
        using var brush = new SolidBrush(blue);
        g.FillEllipse(brush, 1, 1, 30, 30);

        // White up-arrow
        using var arrowBrush = new SolidBrush(Color.White);
        var arrowPoints = new Point[]
        {
            new(16, 6),   // tip top
            new(9, 16),   // left wing
            new(13, 16),  // left inner
            new(13, 26),  // bottom left
            new(19, 26),  // bottom right
            new(19, 16),  // right inner
            new(23, 16),  // right wing
        };
        g.FillPolygon(arrowBrush, arrowPoints);

        IntPtr hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    private void OnUpdateStarted(object? sender, EventArgs e)
    {
        if (_runNowItem is not null)
        {
            InvokeOnUiThread(() =>
            {
                _runNowItem.Enabled = false;
                _lastRunItem!.Text = "Running now…";
            });
        }

        _notifyIcon?.ShowBalloonTip(
            3000,
            "AutomaticUpdater",
            "Running winget upgrade…",
            ToolTipIcon.Info);
    }

    private void OnUpdateCompleted(object? sender, EventArgs e)
    {
        InvokeOnUiThread(() =>
        {
            if (_runNowItem is not null)
                _runNowItem.Enabled = true;
            _lastRunItem!.Text = GetLastRunText();
            _nextRunItem!.Text = GetNextRunText();
        });

        _notifyIcon?.ShowBalloonTip(
            5000,
            "AutomaticUpdater",
            "Update check complete.",
            ToolTipIcon.Info);
    }

    private void OnScheduleChanged(object? sender, EventArgs e)
    {
        InvokeOnUiThread(() =>
        {
            _nextRunItem!.Text = GetNextRunText();
        });
    }

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        if (_settingsWindow is null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
        }
        else
        {
            _settingsWindow.Activate();
        }
    }

    private void OnLogClicked(object? sender, EventArgs e)
    {
        if (_logWindow is null || !_logWindow.IsLoaded)
        {
            _logWindow = new LogWindow();
            _logWindow.Closed += (_, _) => _logWindow = null;
            _logWindow.Show();
        }
        else
        {
            _logWindow.Activate();
        }
    }

    private void OnAutostartCheckedChanged(object? sender, EventArgs e)
    {
        bool enabled = _autostartItem!.Checked;
        AutostartService.SetEnabled(enabled);
        Settings.AutostartEnabled = enabled;
        SettingsService.Save(Settings);
    }

    public void RefreshMenuLabels()
    {
        InvokeOnUiThread(() =>
        {
            _nextRunItem!.Text = GetNextRunText();
            _lastRunItem!.Text = GetLastRunText();
            _autostartItem!.Checked = AutostartService.IsEnabled;
        });
    }

    private string GetNextRunText()
    {
        if (!Settings.Schedule.Enabled)
            return "Next run: (scheduler disabled)";

        if (SchedulerService?.NextRunTime is { } next)
            return $"Next run: {next:ddd MMM d, HH:mm}";

        return "Next run: calculating…";
    }

    private string GetLastRunText()
    {
        if (Settings.LastRunUtc is { } last)
            return $"Last run: {last.ToLocalTime():ddd MMM d, HH:mm}";

        return "Last run: never";
    }

    private static void InvokeOnUiThread(Action action)
    {
        if (System.Windows.Application.Current?.Dispatcher is { } dispatcher)
            dispatcher.InvokeAsync(action);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            SchedulerService?.Dispose();
            _notifyIcon?.Dispose();
            _trayIcon?.Dispose();
            _contextMenu?.Dispose();
            _disposed = true;
        }
    }
}
