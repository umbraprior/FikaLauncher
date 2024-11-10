using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Styling;
using Jeek.Avalonia.Localization;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Controls;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using FikaLauncher.Services;
using FikaLauncher.ViewModels.Dialogs;
using FikaLauncher.Views.Dialogs;
using Avalonia.Platform.Storage;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia;
using System.ComponentModel;

namespace FikaLauncher.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private IImage? _logoImage;

    [ObservableProperty] private bool _isTheme;

    private WindowNotificationManager? _manager;

    [ObservableProperty] private string _language = LocalizationService.Instance.CurrentLanguage;

    [ObservableProperty] private bool _rememberLogin;

    [ObservableProperty] private bool _keepMeLoggedIn;

    [ObservableProperty] private string _eftLiveInstallPath;

    [ObservableProperty] private string _tempDirectoryPath;

    [ObservableProperty] private string _spTarkovInstallPath;

    [ObservableProperty] private int _closeWindowBehavior;

    [ObservableProperty] private int _launchGameBehavior;

    [ObservableProperty] private string _cacheDirectoryPath;

    public bool HasEftPath => !string.IsNullOrEmpty(EftLiveInstallPath);
    public bool HasTempPath => !string.IsNullOrEmpty(TempDirectoryPath);
    public bool HasSptPath => !string.IsNullOrEmpty(SpTarkovInstallPath);
    public bool HasCachePath => !string.IsNullOrEmpty(CacheDirectoryPath);

    public string EftPathDisplay => HasEftPath ? EftLiveInstallPath : Localizer.Get("NoFolderSelected");
    public string TempPathDisplay => HasTempPath ? TempDirectoryPath : Localizer.Get("NoFolderSelected");
    public string SptPathDisplay => HasSptPath ? SpTarkovInstallPath : Localizer.Get("NoFolderSelected");
    public string CachePathDisplay => HasCachePath ? CacheDirectoryPath : Localizer.Get("NoFolderSelected");

    private string GetLocalizedOption(string key)
    {
        return Localizer.Get(key);
    }

    private string[] _closeWindowOptions = Array.Empty<string>();
    private string[] _launchGameOptions = Array.Empty<string>();

    public string[] CloseWindowOptions
    {
        get => _closeWindowOptions;
        private set
        {
            _closeWindowOptions = value;
            OnPropertyChanged();
            SelectedCloseWindowOption = CloseWindowBehavior;
        }
    }

    public string[] LaunchGameOptions
    {
        get => _launchGameOptions;
        private set
        {
            _launchGameOptions = value;
            OnPropertyChanged();
            SelectedLaunchGameOption = LaunchGameBehavior;
        }
    }

    private void UpdateLocalizedOptions()
    {
        var currentCloseOption = SelectedCloseWindowOption;
        var currentLaunchOption = SelectedLaunchGameOption;

        CloseWindowOptions = new[]
        {
            GetLocalizedOption("SystemTray"),
            GetLocalizedOption("ExitLauncher")
        };

        LaunchGameOptions = new[]
        {
            GetLocalizedOption("KeepWindow"),
            GetLocalizedOption("MinimizeWindow")
        };

        SelectedCloseWindowOption = currentCloseOption;
        SelectedLaunchGameOption = currentLaunchOption;
    }

    private void OnLanguageServiceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.CurrentLanguage))
        {
            Language = LocalizationService.Instance.CurrentLanguage;
            UpdateLocalizedOptions();
        }
    }

    [ObservableProperty] private int _selectedCloseWindowOption;

    [ObservableProperty] private int _selectedLaunchGameOption;

    partial void OnSelectedCloseWindowOptionChanged(int value)
    {
        if (value >= 0)
        {
            ConfigurationService.Settings.CloseWindowBehavior = value;
            Task.Run(ConfigurationService.SaveSettingsAsync);
        }
    }

    partial void OnSelectedLaunchGameOptionChanged(int value)
    {
        if (value >= 0)
        {
            ConfigurationService.Settings.LaunchGameBehavior = value;
            Task.Run(ConfigurationService.SaveSettingsAsync);
        }
    }

    partial void OnLanguageChanged(string value)
    {
        if (ConfigurationService.Settings.Language != value)
        {
            ConfigurationService.Settings.Language = value;
            LocalizationService.ChangeLanguage(value);
            Task.Run(async () =>
            {
                await ConfigurationService.SaveSettingsAsync();
                await RepositoryReadmeService.PreCacheReadmeAsync(value);
            });
        }
    }

    private void OnLanguageServiceChanged(object? sender, EventArgs e)
    {
        Language = LocalizationService.Instance.CurrentLanguage;
    }

    public SettingsViewModel()
    {
        LocalizationService.Instance.PropertyChanged += OnLanguageServiceChanged;
        UpdateLocalizedOptions();
        if (Application.Current != null) Application.Current.ActualThemeVariantChanged += OnThemeChanged;

        _isTheme = ConfigurationService.Settings.IsDarkTheme;
        UpdateLogo(Application.Current?.ActualThemeVariant == ThemeVariant.Dark);
        _rememberLogin = ConfigurationService.Settings.RememberLogin;
        _keepMeLoggedIn = ConfigurationService.Settings.KeepLauncherOpen;
        _eftLiveInstallPath = ConfigurationService.Settings.EftInstallPath;
        _tempDirectoryPath = FileSystemService.TempDirectory;
        _spTarkovInstallPath = ConfigurationService.Settings.SptInstallPath;
        _closeWindowBehavior = ConfigurationService.Settings.CloseWindowBehavior;
        _launchGameBehavior = ConfigurationService.Settings.LaunchGameBehavior;
        _cacheDirectoryPath = FileSystemService.CacheDirectory;

        _selectedCloseWindowOption = ConfigurationService.Settings.CloseWindowBehavior;
        _selectedLaunchGameOption = ConfigurationService.Settings.LaunchGameBehavior;
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (Application.Current != null) UpdateLogo(Application.Current.ActualThemeVariant == ThemeVariant.Dark);
    }

    public override void Dispose()
    {
        LocalizationService.Instance.PropertyChanged -= OnLanguageServiceChanged;
        if (Application.Current != null) Application.Current.ActualThemeVariantChanged -= OnThemeChanged;
        base.Dispose();
    }

    partial void OnIsThemeChanged(bool value)
    {
        ConfigurationService.Settings.IsDarkTheme = value;
        App.ChangeTheme(value);

        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                mainViewModel.UpdateLogo(value);

        Task.Run(async () =>
        {
            try
            {
                await ConfigurationService.SaveSettingsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        });

        NotificationController.ShowThemeChanged(value);
    }

    partial void OnRememberLoginChanged(bool value)
    {
        ConfigurationService.Settings.RememberLogin = value;
        if (!value)
        {
            KeepMeLoggedIn = false;
            ConfigurationService.Settings.KeepLauncherOpen = false;
        }

        Task.Run(ConfigurationService.SaveSettingsAsync);
    }

    partial void OnKeepMeLoggedInChanged(bool value)
    {
        if (value && !RememberLogin) RememberLogin = true;

        ConfigurationService.Settings.KeepLauncherOpen = value;

        if (AuthService.IsLoggedIn)
        {
            var currentState = ApplicationStateService.GetCurrentState();
            ApplicationStateService.SaveLoginState(
                AuthService.CurrentUsername,
                true,
                currentState.SecurityToken);
        }

        Task.Run(ConfigurationService.SaveSettingsAsync);
    }

    partial void OnEftLiveInstallPathChanged(string value)
    {
        ConfigurationService.Settings.EftInstallPath = value;
        OnPropertyChanged(nameof(HasEftPath));
        Task.Run(ConfigurationService.SaveSettingsAsync);
    }

    partial void OnSpTarkovInstallPathChanged(string value)
    {
        ConfigurationService.Settings.SptInstallPath = value;
        OnPropertyChanged(nameof(HasSptPath));
        Task.Run(ConfigurationService.SaveSettingsAsync);
    }

    partial void OnCloseWindowBehaviorChanged(int value)
    {
        ConfigurationService.Settings.CloseWindowBehavior = value;
        Task.Run(ConfigurationService.SaveSettingsAsync);
    }

    partial void OnLaunchGameBehaviorChanged(int value)
    {
        ConfigurationService.Settings.LaunchGameBehavior = value;
        Task.Run(ConfigurationService.SaveSettingsAsync);
    }


    private void ShowNotification(string title, string message, NotificationType type)
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                mainViewModel.ShowNotification(title, message, type);
    }

    [RelayCommand]
    private async Task SetEftLiveFolder()
    {
        var folderPath = await SelectFolder();
        if (!string.IsNullOrEmpty(folderPath)) EftLiveInstallPath = folderPath;
    }

    [RelayCommand]
    private async Task SetSpTarkovFolder()
    {
        var folderPath = await SelectFolder();
        if (!string.IsNullOrEmpty(folderPath)) SpTarkovInstallPath = folderPath;
    }

    private async Task<string?> SelectFolder()
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var topLevel = desktop.MainWindow;
            if (topLevel is null) return null;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder",
                AllowMultiple = false
            });

            return folders.Count > 0 ? folders[0].Path.LocalPath : null;
        }

        return null;
    }

    [RelayCommand]
    private void OpenEftLiveFolder()
    {
        OpenFolder(EftLiveInstallPath);
    }

    [RelayCommand]
    private void OpenSpTarkovFolder()
    {
        OpenFolder(SpTarkovInstallPath);
    }

    private void OpenFolder(string path)
    {
        if (Directory.Exists(path))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        else
            NotificationController.ShowDirectoryNotExist(path);
    }

    [RelayCommand]
    private async Task CleanTempFolder()
    {
        var dialogResult =
            await DialogService.ShowDialog<CleanTempFilesDialogView>(new CleanTempFilesDialogViewModel());

        if (dialogResult is bool result && result)
            try
            {
                FileSystemService.CleanTempDirectory();
                NotificationController.ShowTempCleanSuccess();
            }
            catch (Exception)
            {
                NotificationController.ShowTempCleanError();
            }
    }

    [RelayCommand]
    private async Task CleanCacheFolder()
    {
        var dialogResult =
            await DialogService.ShowDialog<CleanTempFilesDialogView>(new CleanTempFilesDialogViewModel());

        if (dialogResult is bool result && result)
            try
            {
                FileSystemService.CleanCacheDirectory();
                NotificationController.ShowCacheCleanSuccess();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning cache directory: {ex.Message}");
                NotificationController.ShowCacheCleanError();
            }
    }

    public void UpdateLogo(bool isDarkTheme)
    {
        var uri = new Uri(isDarkTheme
            ? "avares://FikaLauncher/Assets/fika-logo-light.png"
            : "avares://FikaLauncher/Assets/fika-logo-dark.png");

        try
        {
            using var stream = AssetLoader.Open(uri);
            LogoImage = new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load logo: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenCacheFolder()
    {
        OpenFolder(FileSystemService.CacheDirectory);
    }

    [RelayCommand]
    private void OpenTempFolder()
    {
        OpenFolder(FileSystemService.TempDirectory);
    }
}