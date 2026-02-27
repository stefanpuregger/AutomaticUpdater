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
    private volatile bool _isRunning;

    public event EventHandler? UpdateStarted;
    public event EventHandler? UpdateCompleted;
    public event EventHandler<string>? LogLineAdded;

    public bool IsRunning => _isRunning;

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
        if (_isRunning) return;
        _isRunning = true;

        UpdateStarted?.Invoke(this, EventArgs.Empty);

        string startLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === Starting winget upgrade ===";
        AppendLine(startLine);

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

            string endLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === winget exited with code {process.ExitCode} ===";
            AppendLine(endLine);
        }
        catch (OperationCanceledException)
        {
            AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === Update cancelled ===");
        }
        catch (Exception ex)
        {
            AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {ex.Message}");
        }
        finally
        {
            _settings.LastRunUtc = DateTime.UtcNow;
            _settingsService.Save(_settings);
            _isRunning = false;
            UpdateCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void AppendLine(string line)
    {
        if (IsNoiseLine(line)) return;
        _settingsService.AppendLogLine(line, _settings.LogMaxLines);
        LogLineAdded?.Invoke(this, line);
    }

    private static bool IsNoiseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return true;
        return _noisePattern.IsMatch(line);
    }
}
