using Avalonia.Controls.Notifications;
using Avalonia.Controls.ApplicationLifetimes;
using FikaLauncher.Views;
using FikaLauncher.ViewModels;
using FikaLauncher.Localization;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace FikaLauncher.Services;

public static class NotificationController
{
    private static readonly HashSet<string> _activeNotifications = new();
    private static readonly object _notificationLock = new();
    private const int NotificationTimeoutMs = 5000; // 5 seconds
    private const string RateLimitKey = "github:ratelimit";

    private static bool TryAddNotification(string key, bool isPersistent = false)
    {
        lock (_notificationLock)
        {
            if (_activeNotifications.Contains(key))
                return false;

            _activeNotifications.Add(key);
            if (!isPersistent) RemoveNotificationAfterDelay(key);
            return true;
        }
    }

    private static async void RemoveNotificationAfterDelay(string key)
    {
        await Task.Delay(NotificationTimeoutMs);
        RemoveNotification(key);
    }

    public static void RemoveNotification(string key)
    {
        lock (_notificationLock)
        {
            _activeNotifications.Remove(key);
        }
    }

    public static void Show(string title, string message, NotificationType type, string? uniqueKey = null,
        bool isPersistent = false)
    {
        var key = uniqueKey ?? $"{title}:{message}";
        Console.WriteLine($"Showing notification - Title: {title}, Message: {message}, Type: {type}");

        if (!TryAddNotification(key, isPersistent))
        {
            Console.WriteLine("Notification already active");
            return;
        }

        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                Console.WriteLine("Sending to MainViewModel");
                mainViewModel.ShowNotification(title, message, type);
            }
            else
            {
                Console.WriteLine("MainViewModel not found");
            }
        }
        else
        {
            Console.WriteLine("Desktop lifetime not found");
        }
    }

    public static void ShowGitHubError(string operation, string error)
    {
        var key = $"github:{operation}";

        if (error.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            Show(
                Localizer.Get("GitHubRateLimit"),
                Localizer.Get("GitHubRateLimitMessage"),
                NotificationType.Error,
                key
            );
        else if (error.Contains("Network", StringComparison.OrdinalIgnoreCase))
            Show(
                Localizer.Get("GitHubNetworkError"),
                Localizer.Get("GitHubNetworkErrorMessage"),
                NotificationType.Error,
                key
            );
        else
            Show(
                Localizer.Get("GitHubError"),
                string.Format(Localizer.Get("GitHubErrorMessage"), operation, error),
                NotificationType.Error,
                key
            );
    }

    public static void ShowGitHubRateLimited(DateTime resetTime)
    {
        var timeUntilReset = resetTime - DateTime.UtcNow;
        var minutes = (int)Math.Ceiling(timeUntilReset.TotalMinutes);

        var message = minutes > 0
            ? $"GitHub rate limit reached. Please try again in {minutes} minutes."
            : "GitHub rate limit reached. Please try again later.";

        ShowPersistentNotification(
            Localizer.Get("RateLimit"),
            message,
            NotificationType.Error,
            RateLimitKey
        );
    }

    public static void ClearRateLimitNotification()
    {
        RemoveNotification(RateLimitKey);
    }

    public static void ShowError(string message)
    {
        Show(Localizer.Get("Error"), message, NotificationType.Error);
    }

    public static void ShowSuccess(string message)
    {
        Show(Localizer.Get("Success"), message, NotificationType.Success);
    }

    public static void ShowInfo(string message)
    {
        Show(Localizer.Get("Info"), message, NotificationType.Information);
    }

    public static void ShowWarning(string message)
    {
        Show(Localizer.Get("Warning"), message, NotificationType.Warning);
    }

    public static void ShowConnectionSuccess(bool isLocalhost)
    {
        Show(
            Localizer.Get("Success"),
            isLocalhost ? Localizer.Get("ServerStarted") : Localizer.Get("Connected"),
            NotificationType.Success
        );
    }

    public static void ShowConnectionError(bool isLocalhost)
    {
        Show(
            Localizer.Get("Error"),
            isLocalhost ? Localizer.Get("ServerStartFailed") : Localizer.Get("ConnectionFailed"),
            NotificationType.Error
        );
    }

    public static void ShowInvalidCredentials()
    {
        ShowError(Localizer.Get("InvalidCredentials"));
    }

    public static void ShowBookmarkExists()
    {
        ShowError(Localizer.Get("BookmarkAlreadyExists"));
    }

    public static void ShowBookmarkRenamed()
    {
        ShowInfo(Localizer.Get("BookmarkRenamed"));
    }

    public static void ShowBookmarkRemoved()
    {
        ShowInfo(Localizer.Get("BookmarkRemoved"));
    }

    public static void ShowServerShutdown()
    {
        Show(Localizer.Get("Success"), Localizer.Get("ServerShutdown"), NotificationType.Error);
    }

    public static void ShowDisconnected()
    {
        Show(Localizer.Get("Success"), Localizer.Get("Disconnected"), NotificationType.Warning);
    }

    public static void ShowLoggedIn()
    {
        Show(Localizer.Get("Success"), Localizer.Get("LoggedIn"), NotificationType.Success);
    }

    public static void ShowLoggedOut()
    {
        Show(Localizer.Get("Success"), Localizer.Get("LoggedOut"), NotificationType.Information);
    }

    public static void ShowBookmarkAdded()
    {
        Show(Localizer.Get("Success"), Localizer.Get("BookmarkAdded"), NotificationType.Information);
    }

    public static void ShowThemeChanged(bool isDark)
    {
        Show(
            Localizer.Get("ThemeChange"),
            $"{(isDark ? Localizer.Get("Dark") : Localizer.Get("Light"))} {Localizer.Get("ThemeApplied")}",
            NotificationType.Success
        );
    }

    public static void ShowDirectoryNotExist(string path)
    {
        ShowError($"{Localizer.Get("DirectoryNotExist")}: {path}");
    }

    public static void ShowTempCleanSuccess()
    {
        ShowSuccess(Localizer.Get("TempClean"));
    }

    public static void ShowTempCleanError()
    {
        ShowError(Localizer.Get("TempCleanError"));
    }

    public static void ShowCacheCleanSuccess()
    {
        ShowSuccess(Localizer.Get("CacheClean"));
    }

    public static void ShowCacheCleanError()
    {
        ShowError(Localizer.Get("CacheCleanError"));
    }

    public static void ShowLoginError(string message)
    {
        ShowError(message);
    }

    public static void ShowAccountCreationError(string message)
    {
        ShowError(message);
    }

    public static void ShowServerAddressEmpty()
    {
        ShowError(Localizer.Get("ServerAddressEmpty"));
    }

    public static void ShowBookmarkEditSuccess()
    {
        Show(
            Localizer.Get("Info"),
            Localizer.Get("BookmarkRenamed"),
            NotificationType.Information
        );
    }

    public static void ShowPersistentNotification(string title, string message, NotificationType type, string key)
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                mainViewModel.ShowPersistentNotification(title, message, type, key);
    }

    public static void UpdatePersistentNotification(string key, string newMessage, string? newTitle = null)
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                mainViewModel.UpdatePersistentNotification(key, newMessage, newTitle);
    }

    public static async Task ShowCountdownNotification(
        string title,
        string messageFormat,
        TimeSpan duration,
        NotificationType type,
        string key,
        int updateIntervalSeconds = 1)
    {
        ShowPersistentNotification(title, string.Format(messageFormat, duration.TotalSeconds), type, key);

        var endTime = DateTime.UtcNow.Add(duration);
        while (DateTime.UtcNow < endTime)
        {
            await Task.Delay(TimeSpan.FromSeconds(updateIntervalSeconds));
            var remaining = endTime - DateTime.UtcNow;
            UpdatePersistentNotification(key, string.Format(messageFormat, Math.Ceiling(remaining.TotalSeconds)));
        }

        RemoveNotification(key);
    }

    public static async Task ShowProgressNotification(
        string title,
        string messageFormat,
        NotificationType type,
        string key,
        IProgress<int> progress)
    {
        ShowPersistentNotification(title, string.Format(messageFormat, 0), type, key);

        var progressHandler = new Progress<int>(percentage =>
        {
            UpdatePersistentNotification(key, string.Format(messageFormat, percentage));
            if (percentage >= 100) RemoveNotification(key);
        });

        ((IProgress<int>)progressHandler).Report(0);
    }
}