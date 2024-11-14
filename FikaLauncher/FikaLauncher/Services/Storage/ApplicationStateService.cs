using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FikaLauncher.Services;
using FikaLauncher.Localization;
using System.Text.Json.Serialization;
using System.Globalization;

public static class ApplicationStateService
{
    private static readonly string StateFilePath = Path.Combine(FileSystemService.CacheDirectory, "appstate.json");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringDateTimeConverter()
        }
    };

    private static AppState? _currentState;

    public class AppState
    {
        public string CurrentGreeting { get; set; } = string.Empty;
        public bool IsLoggedIn { get; set; }
        public string Username { get; set; } = string.Empty;
        public string LastLoggedInUsername { get; set; } = string.Empty;
        public DateTime LastLoginTime { get; set; }
        public bool KeepLoggedIn { get; set; }
        public string? SecurityToken { get; set; }
        public string LastServerAddress { get; set; } = "127.0.0.1";
        public string LastServerPort { get; set; } = "6969";
        public bool HasAcceptedLauncherTerms { get; set; }
        public bool HasAcceptedFikaTerms { get; set; }
        public DateTime RateLimitResetTime { get; set; } = DateTime.MinValue;
    }

    public static (string prefix, string suffix) GetOrUpdateGreeting(string username)
    {
        LoadState();

        var hour = DateTime.Now.Hour;
        string greeting;

        if (Random.Shared.NextDouble() < 0.3)
        {
            if (hour >= 5 && hour < 12)
                greeting = "GoodMorning";
            else if (hour >= 12 && hour < 17)
                greeting = "GoodAfternoon";
            else
                greeting = "GoodEvening";
        }
        else
        {
            var validGreetings = new List<string>();

            for (var i = 1; i <= 20; i++)
            {
                var key = $"RandomGreeting{i}";
                var value = Localizer.Get(key);

                if (value != key) validGreetings.Add(key);
            }

            if (validGreetings.Count > 0)
            {
                greeting = validGreetings[Random.Shared.Next(validGreetings.Count)];
            }
            else
            {
                if (hour >= 5 && hour < 12)
                    greeting = "GoodMorning";
                else if (hour >= 12 && hour < 17)
                    greeting = "GoodAfternoon";
                else
                    greeting = "GoodEvening";
            }
        }

        var localizedGreeting = Localizer.Get(greeting);
        var parts = localizedGreeting.Split(new[] { "{0}" }, StringSplitOptions.None);

        _currentState.Username = username;
        _currentState.IsLoggedIn = true;
        SaveState();

        return (parts[0], parts.Length > 1 ? parts[1] : string.Empty);
    }

    public static void LoadState()
    {
        try
        {
            if (File.Exists(StateFilePath))
            {
                var rawJson = File.ReadAllText(StateFilePath);
                var loadedState = JsonSerializer.Deserialize<AppState>(rawJson, _jsonOptions);
                if (loadedState != null) _currentState = loadedState;
            }
            else
            {
                _currentState = new AppState();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in LoadState: {ex}");
            _currentState = new AppState();
        }
    }

    public static void SaveState()
    {
        try
        {
            if (_currentState == null) _currentState = new AppState();

            var json = JsonSerializer.Serialize(_currentState, _jsonOptions);
            File.WriteAllText(StateFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving state: {ex}");
        }
    }

    public static void ClearCache()
    {
        _currentState = new AppState();
        try
        {
            if (File.Exists(StateFilePath)) File.Delete(StateFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear application state: {ex.Message}");
        }
    }

    public static async Task LoadLoginState()
    {
        LoadState();
        var state = GetCurrentState();
        var savedResetTime = state.RateLimitResetTime;

        if (!state.KeepLoggedIn || string.IsNullOrEmpty(state.SecurityToken))
        {
            ClearLoginState(true);
            return;
        }

        if (await AuthService.ValidateAndRestoreLogin(state.Username, state.SecurityToken))
            return;

        var lastUsername = state.LastLoggedInUsername;
        ClearLoginState();
        _currentState.LastLoggedInUsername = lastUsername;
        SaveState();
    }

    public static void SaveLoginState(string username, bool isLoggedIn, string? securityToken)
    {
        _currentState.Username = username;
        _currentState.IsLoggedIn = isLoggedIn;
        _currentState.LastLoggedInUsername = username;
        _currentState.LastLoginTime = DateTime.UtcNow;
        _currentState.KeepLoggedIn = ConfigurationService.Settings.KeepLauncherOpen;
        _currentState.SecurityToken = securityToken;
        SaveState();
    }

    public static void ClearLoginState(bool preserveRateLimit = false)
    {
        var oldState = _currentState ?? new AppState();

        _currentState = new AppState
        {
            RateLimitResetTime = oldState.RateLimitResetTime,
            HasAcceptedLauncherTerms = oldState.HasAcceptedLauncherTerms,
            HasAcceptedFikaTerms = oldState.HasAcceptedFikaTerms,
            LastServerAddress = oldState.LastServerAddress,
            LastServerPort = oldState.LastServerPort
        };

        SaveState();
    }

    public static string GetLastLoggedInUsername()
    {
        LoadState();
        return _currentState.LastLoggedInUsername;
    }

    public static AppState GetCurrentState()
    {
        if (_currentState == null) LoadState();
        return _currentState ?? new AppState();
    }

    public static void Initialize()
    {
        if (!File.Exists(StateFilePath))
            SaveState();
        else
            LoadState();
    }
}

public class JsonStringDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        return dateString != null ? DateTime.Parse(dateString, null, DateTimeStyles.RoundtripKind) : DateTime.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("O"));
    }
}