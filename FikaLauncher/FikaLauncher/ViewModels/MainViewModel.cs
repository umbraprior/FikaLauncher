using System;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.IO;
using Avalonia.Media;
using Jeek.Avalonia.Localization;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.ApplicationLifetimes;
using FikaLauncher.Views;
using FikaLauncher.Services;

namespace FikaLauncher.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isPaneOpen = false;
    [ObservableProperty] private bool _isPaneNotOpen = true;
    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private string _logoSource = "avares://FikaLauncher/Assets/fika-logo-light.png";
    [ObservableProperty] private IImage? _logoImage;
    [ObservableProperty] private double _logoWidth = 60;
    [ObservableProperty] private double _logoHeight = 60;
    [ObservableProperty]
    private int _closeAction = 0; // 0 for minimize to tray, 1 for exit

    partial void OnLogoSourceChanged(string value)
    {
        Console.WriteLine($"Logo source changed to: {value}");
    }

    partial void OnIsPaneOpenChanged(bool value)
    {
        UpdateLogoSize(value);
    }

    public MainViewModel()
    {
        CurrentPage = new PlayViewModel();
        UpdateLogo(App.Current!.RequestedThemeVariant == ThemeVariant.Dark);
        UpdateLogoSize(IsPaneOpen);
    }

    [RelayCommand]
    private void TogglePane()
    {
        UpdatePaneState(!IsPaneOpen);
    }

    public void UpdatePaneState(bool isOpen)
    {
        IsPaneOpen = isOpen;
        IsPaneNotOpen = !isOpen;
        UpdateLogoSize(isOpen);
    }

    [RelayCommand]
    private void NavigatePlay()
    {
        CurrentPage = new PlayViewModel();
    }
    
    [RelayCommand]
    private void NavigateInstall()
    {
        CurrentPage = new InstallViewModel();
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        CurrentPage = new SettingsViewModel();
    }
    
    [RelayCommand]
    private void NavigateAbout()
    {
        CurrentPage = new AboutViewModel();
    }

    public void UpdateLogo(bool isDarkTheme)
    {
        var uri = new Uri(isDarkTheme ? "avares://FikaLauncher/Assets/fika-logo-light.png" : "avares://FikaLauncher/Assets/fika-logo-dark.png");
        
        try
        {
            using var stream = AssetLoader.Open(uri);
            LogoImage = new Bitmap(stream);
            Console.WriteLine($"Logo loaded successfully: {uri}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load logo: {ex.Message}");
        }
    }

    private void UpdateLogoSize(bool isPaneOpen)
    {
        LogoWidth = isPaneOpen ? 150 : 50;
        LogoHeight = isPaneOpen ? 150 : 50;
    }

    [ObservableProperty]
    private string _language = LocalizationService.Instance.CurrentLanguage;

    partial void OnLanguageChanged(string value)
    {
        LocalizationService.ChangeLanguage(value);
        _ = GitHubReadmeService.PreCacheReadmeAsync(value);
    }
    
    public void ShowNotification(string title, string message, NotificationType type)
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is MainWindow mainWindow)
            {
                mainWindow.NotificationManager?.Show(new Notification(title, message, type));
            }
        }
    }
}
