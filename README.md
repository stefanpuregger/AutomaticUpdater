# AutomaticUpdater

A lightweight Windows system tray application that automatically runs `winget upgrade --all` on a configurable schedule.

## Features

- **System tray** — runs silently in the background, no taskbar clutter
- **Scheduled updates** — daily or weekly at a time you choose
- **Run Now** — trigger an update check on demand
- **Live log viewer** — dark-themed console window showing winget output in real time
- **Start with Windows** — optional autostart via the registry
- **Single instance** — only one copy runs at a time

## Screenshots

| Tray Menu | Settings | Log Viewer |
|-----------|----------|------------|
| Right-click the tray icon to access all features | Configure schedule and autostart | Live winget output in a dark Consolas terminal |

## Requirements

- Windows 10 / 11
- [.NET 8 Runtime](https://aka.ms/dotnet-download) (Windows Desktop)
- [winget](https://aka.ms/winget) (included with Windows 11; available via the Microsoft Store on Windows 10)

## Installation

### Option A — Build from source

```bash
git clone https://github.com/stefanpuregger/AutomaticUpdater.git
cd AutomaticUpdater
dotnet build -c Release
```

The executable will be at `bin\Release\net8.0-windows\AutomaticUpdater.exe`.

### Option B — Download release

Download the latest `AutomaticUpdater.exe` from the [Releases](https://github.com/stefanpuregger/AutomaticUpdater/releases) page and run it.

## Usage

Launch `AutomaticUpdater.exe` — it goes straight to the system tray (no window).

**Right-click the tray icon** to access:

| Menu Item | Description |
|-----------|-------------|
| Next run: … | When the next scheduled update will run |
| Last run: … | When the last update ran |
| **Run Now** | Run `winget upgrade --all` immediately |
| **Settings…** | Configure schedule and autostart |
| **View Log…** | Open the live log window |
| **Start with Windows** | Toggle autostart via the registry |
| **Exit** | Quit the application |

Double-clicking the tray icon also opens the log viewer.

## Configuration

Settings are saved to `%AppData%\AutomaticUpdater\settings.json`.

```json
{
  "AutostartEnabled": true,
  "Schedule": {
    "Enabled": true,
    "Frequency": "Daily",
    "Hour": 3,
    "Minute": 0,
    "DayOfWeek": "Sunday"
  },
  "LastRunUtc": null,
  "LogMaxLines": 1000
}
```

The update log is stored at `%AppData%\AutomaticUpdater\update.log` and is automatically trimmed to 1000 lines.

## How It Works

- Uses `System.Threading.Timer` for scheduling — wakes up at the next scheduled time, runs the update, then recalculates and reschedules
- Runs `winget upgrade --all --silent --accept-source-agreements --accept-package-agreements`
- Captures stdout and stderr in real time and streams them to the log file and log window
- Autostart is implemented via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

## Project Structure

```
AutomaticUpdater/
├── App.xaml / App.xaml.cs         Single-instance guard, app entry point
├── TrayApplication.cs             NotifyIcon, context menu, balloon tips
├── Models/
│   └── AppSettings.cs             Settings model
├── Services/
│   ├── SettingsService.cs         JSON persistence
│   ├── AutostartService.cs        Registry autostart
│   ├── UpdaterService.cs          winget process management
│   └── SchedulerService.cs        Timer-based scheduling
└── Windows/
    ├── SettingsWindow.xaml/.cs    Schedule configuration UI
    └── LogWindow.xaml/.cs         Live log viewer
```

## License

MIT
