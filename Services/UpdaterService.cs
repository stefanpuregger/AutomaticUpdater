using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using AutomaticUpdater.Models;

namespace AutomaticUpdater.Services;

public class UpdaterService
{
    private readonly SettingsService _settingsService;
    private AppSettings _settings;
    private int _isRunning;

    public event EventHandler? UpdateStarted;
    public event EventHandler? UpdateCompleted;
    public event EventHandler<string>? LogLineAdded;

    public bool IsRunning => _isRunning == 1;

    // Matches spinner frames and progress bar lines winget emits during downloads/installs
    private static readonly Regex _noisePattern = new(
        @"^\s*([-\\|/]|[█░▒▓]+.*|.*[█░▒▓]+.*%.*)\s*$",
        RegexOptions.Compiled);

    public UpdaterService(SettingsService settingsService, AppSettings settings)
    {
        _settingsService = settingsService;
        _settings = settings;
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task RunUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0) return;

        UpdateStarted?.Invoke(this, EventArgs.Empty);
        AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === Starting winget upgrade ===");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "upgrade --all --silent --accept-source-agreements --accept-package-agreements",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    AppendLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    AppendLine($"[ERR] {e.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === winget exited with code {process.ExitCode} ===");
        }
        catch (OperationCanceledException)
        {
            AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === Update cancelled ===");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 2) // ERROR_FILE_NOT_FOUND
        {
            AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] winget not found. Install App Installer from the Microsoft Store.");
        }
        catch (Exception ex)
        {
            AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {ex.Message}");
        }
        finally
        {
            _settings.LastRunUtc = DateTime.UtcNow;
            _settingsService.Save(_settings);
            _settingsService.TrimLog(_settings.LogMaxLines);
            Interlocked.Exchange(ref _isRunning, 0);
            UpdateCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void AppendLine(string line)
    {
        if (IsNoiseLine(line)) return;
        _settingsService.AppendLogLine(line);
        LogLineAdded?.Invoke(this, line);
    }

    private static bool IsNoiseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return true;
        return _noisePattern.IsMatch(line);
    }
}
