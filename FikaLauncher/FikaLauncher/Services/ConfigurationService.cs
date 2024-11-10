using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FikaLauncher.Services;

public static class ConfigurationService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static readonly object _saveLock = new();
    private static bool _isSaving = false;

    public static AppSettings Settings { get; private set; } = new();

    static ConfigurationService()
    {
        Console.WriteLine("Initializing ConfigurationService...");

        try
        {
            LoadSettings();
            EnsureDefaultFiles();
            ApplySettings();
            Console.WriteLine("ConfigurationService initialized successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing ConfigurationService: {ex.Message}");
            Settings = new AppSettings();
            SaveSettings();
            ApplySettings();
        }
    }

    private static void LoadSettings()
    {
        var settingsPath = Path.Combine(FileSystemService.SettingsDirectory, "settings.json");

        if (File.Exists(settingsPath))
        {
            try
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                if (settings != null)
                {
                    Settings = settings;
                    Console.WriteLine("Settings loaded successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings, using defaults: {ex.Message}");
                Settings = new AppSettings();
                SaveSettings();
            }
        }
        else
        {
            Console.WriteLine("No settings file found, creating with defaults");
            Settings = new AppSettings();
            SaveSettings();
        }
    }

    public static void SaveSettings()
    {
        if (_isSaving) return;

        lock (_saveLock)
        {
            if (_isSaving) return;
            _isSaving = true;

            try
            {
                var settingsPath = Path.Combine(FileSystemService.SettingsDirectory, "settings.json");
                var json = JsonSerializer.Serialize(Settings, _jsonOptions);
                File.WriteAllText(settingsPath, json);
                Console.WriteLine("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
                throw;
            }
            finally
            {
                _isSaving = false;
            }
        }
    }

    public static async Task SaveSettingsAsync()
    {
        if (_isSaving) return;

        await Task.Run(() => { SaveSettings(); });
    }

    private static void EnsureDefaultFiles()
    {
        var logPath = Path.Combine(FileSystemService.LogsDirectory, "app.log");
        if (!File.Exists(logPath)) File.WriteAllText(logPath, $"Log file created: {DateTime.Now}\n");
    }

    private static async void ApplySettings()
    {
        LocalizationService.ChangeLanguage(Settings.Language);

        App.ChangeTheme(Settings.IsDarkTheme);

        Console.WriteLine(
            $"Applied settings - Language: {Settings.Language}, Theme: {(Settings.IsDarkTheme ? "Dark" : "Light")}");

        await GitHubReadmeService.PreCacheReadmeAsync(Settings.Language);
    }
}

public class AppSettings
{
    public string Language { get; set; } = "en-US";
    public bool IsDarkTheme { get; set; } = true;
    public bool RememberLogin { get; set; } = false;
    public bool KeepLauncherOpen { get; set; } = false;
    public string? LastServer { get; set; }
    public string? LastUsername { get; set; }
    public string? CurrentUsername { get; set; }
    public bool IsLoggedIn { get; set; } = false;
    public string EftInstallPath { get; set; } = string.Empty;
    public string SptInstallPath { get; set; } = string.Empty;
    public int CloseWindowBehavior { get; set; } = 0;
    public int LaunchGameBehavior { get; set; } = 0;
}