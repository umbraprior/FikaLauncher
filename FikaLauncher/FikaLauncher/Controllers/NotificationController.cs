using Avalonia.Controls.Notifications;
using Avalonia.Controls.ApplicationLifetimes;
using FikaLauncher.Views;
using FikaLauncher.ViewModels;
using Jeek.Avalonia.Localization;

namespace FikaLauncher.Services;

public static class NotificationController
{
    public static void Show(string title, string message, NotificationType type)
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                mainViewModel.ShowNotification(title, message, type);
    }

    // Predefined notification methods
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

    // Common notification messages
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

    // Add these methods to the NotificationController class
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
}