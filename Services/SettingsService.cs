using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutomaticUpdater.Models;

namespace AutomaticUpdater.Services;

public class SettingsService
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AutomaticUpdater");

    private static readonly string SettingsFilePath = Path.Combine(AppDataDir, "settings.json");

    public string LogFilePath { get; } = Path.Combine(AppDataDir, "update.log");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppSettings Load()
    {
        EnsureDirectoryExists();

        if (!File.Exists(SettingsFilePath))
            return new AppSettings();

        try
        {
            string json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        EnsureDirectoryExists();
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }

    public string[] GetLogLines()
    {
        if (!File.Exists(LogFilePath))
            return Array.Empty<string>();

        try
        {
            return File.ReadAllLines(LogFilePath);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public void AppendLogLine(string line)
    {
        EnsureDirectoryExists();

        try
        {
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }
        catch
        {
            // Best-effort logging; ignore failures
        }
    }

    public void TrimLog(int maxLines)
    {
        try
        {
            if (!File.Exists(LogFilePath)) return;
            var lines = File.ReadAllLines(LogFilePath);
            if (lines.Length > maxLines)
            {
                var trimmed = lines.Skip(lines.Length - maxLines).ToArray();
                File.WriteAllLines(LogFilePath, trimmed);
            }
        }
        catch
        {
            // Best-effort
        }
    }

    public void ClearLog()
    {
        if (File.Exists(LogFilePath))
            File.Delete(LogFilePath);
    }

    private static void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(AppDataDir);
    }
}
