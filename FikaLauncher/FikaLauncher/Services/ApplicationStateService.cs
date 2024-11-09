using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FikaLauncher.Services;
using Jeek.Avalonia.Localization;

public static class ApplicationStateService
{
    private static readonly string StateFilePath = Path.Combine(FileSystemService.CacheDirectory, "appstate.json");
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };
    
    private static AppState _currentState = new();

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
    }

    public static (string prefix, string suffix) GetOrUpdateGreeting(string username)
    {
        LoadState();
        
        var hour = DateTime.Now.Hour;
        string greeting;
        
        if (Random.Shared.NextDouble() < 0.3)
        {
            // Time-based greeting (30% chance)
            if (hour >= 5 && hour < 12)
                greeting = "GoodMorning";
            else if (hour >= 12 && hour < 17)
                greeting = "GoodAfternoon";
            else
                greeting = "GoodEvening";
        }
        else
        {
            // Get all available greetings from the Localizer
            var validGreetings = new List<string>();
            
            // We know we have at least 18 greetings from the language file
            for (int i = 1; i <= 18; i++)
            {
                string key = $"RandomGreeting{i}";
                string value = Localizer.Get(key);
                
                // Only add if we got a real translation (not the key itself)
                if (value != key)
                {
                    validGreetings.Add(key);
                }
            }

            if (validGreetings.Count > 0)
            {
                // Pick a random greeting from the valid ones
                greeting = validGreetings[Random.Shared.Next(validGreetings.Count)];
            }
            else
            {
                // Fallback to time-based greeting if no random greetings found
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

    private static void LoadState()
    {
        try
        {
            // Ensure cache directory exists
            Directory.CreateDirectory(FileSystemService.CacheDirectory);
            
            if (File.Exists(StateFilePath))
            {
                var json = File.ReadAllText(StateFilePath);
                _currentState = JsonSerializer.Deserialize<AppState>(json, _jsonOptions) ?? new AppState();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load application state: {ex.Message}");
            _currentState = new AppState();
        }
    }

    public static void SaveState()
    {
        try
        {
            // Ensure cache directory exists
            Directory.CreateDirectory(FileSystemService.CacheDirectory);
            
            var json = JsonSerializer.Serialize(_currentState, _jsonOptions);
            File.WriteAllText(StateFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save application state: {ex.Message}");
        }
    }

    public static void ClearCache()
    {
        _currentState = new AppState();
        try
        {
            if (File.Exists(StateFilePath))
            {
                File.Delete(StateFilePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear application state: {ex.Message}");
        }
    }

    public static async Task LoadLoginState()
    {
        LoadState();
        
        if (_currentState.KeepLoggedIn && 
            _currentState.IsLoggedIn && 
            !string.IsNullOrEmpty(_currentState.Username) && 
            ConfigurationService.Settings.KeepLauncherOpen)
        {
            if (await AuthService.ValidateAndRestoreLogin(_currentState.Username, _currentState.SecurityToken))
            {
                return;
            }
        }
        
        // If validation fails, clear the login state
        var lastUsername = _currentState.LastLoggedInUsername;
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

    public static void ClearLoginState()
    {
        // Preserve the LastLoggedInUsername when clearing login state
        var lastUsername = _currentState.LastLoggedInUsername;
        _currentState = new AppState
        {
            LastLoggedInUsername = lastUsername
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
        return _currentState;
    }
}
