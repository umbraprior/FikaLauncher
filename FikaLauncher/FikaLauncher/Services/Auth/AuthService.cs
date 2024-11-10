using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FikaLauncher.Localization;

namespace FikaLauncher.Services;

public static class AuthService
{
    public static event EventHandler<bool>? AuthStateChanged;
    public static event EventHandler<string?>? CurrentUsernameChanged;
    private static readonly Random _random = new();

    public static bool IsLoggedIn { get; private set; }
    private static string? _currentUsername;

    public static string? CurrentUsername
    {
        get => _currentUsername;
        set
        {
            if (_currentUsername != value)
            {
                _currentUsername = value;
                CurrentUsernameChanged?.Invoke(null, value);
            }
        }
    }

    public static (string Prefix, string Suffix) CurrentGreeting { get; private set; }

    public static void Initialize()
    {
        ApplicationStateService.LoadLoginState();
    }

    public static async Task Login(string username)
    {
        var currentState = ApplicationStateService.GetCurrentState();
        string? token = null;

        if (currentState.Username == username && !string.IsNullOrEmpty(currentState.SecurityToken))
        {
            token = currentState.SecurityToken;
        }
        else
        {
            var (success, newToken) = await DatabaseService.GenerateSecurityToken(username);
            if (!success || newToken == null) throw new InvalidOperationException("Failed to generate security token");
            token = newToken;
        }

        CurrentUsername = username;
        CurrentGreeting = ApplicationStateService.GetOrUpdateGreeting(username);

        if (ConfigurationService.Settings.KeepLauncherOpen)
        {
            ConfigurationService.Settings.IsLoggedIn = true;
            ConfigurationService.Settings.CurrentUsername = username;
            await ConfigurationService.SaveSettingsAsync();
        }

        ApplicationStateService.SaveLoginState(username, true, token);

        IsLoggedIn = true;
        AuthStateChanged?.Invoke(null, true);
    }

    public static async Task<bool> ValidateAndRestoreLogin(string username, string? token)
    {
        var (success, newToken) = await DatabaseService.ValidateOrRefreshSecurityToken(username, token);
        if (success && newToken != null)
        {
            CurrentUsername = username;
            CurrentGreeting = ApplicationStateService.GetOrUpdateGreeting(username);
            ApplicationStateService.SaveLoginState(username, true, newToken);
            IsLoggedIn = true;
            AuthStateChanged?.Invoke(null, true);
            return true;
        }

        return false;
    }

    public static async Task Logout()
    {
        if (!string.IsNullOrEmpty(CurrentUsername)) await DatabaseService.InvalidateSecurityToken(CurrentUsername);

        IsLoggedIn = false;
        CurrentUsername = string.Empty;
        CurrentGreeting = (string.Empty, string.Empty);

        ApplicationStateService.ClearLoginState();

        if (ConfigurationService.Settings.KeepLauncherOpen)
        {
            ConfigurationService.Settings.IsLoggedIn = false;
            ConfigurationService.Settings.CurrentUsername = null;
            await ConfigurationService.SaveSettingsAsync();
        }

        AuthStateChanged?.Invoke(null, false);
    }
}