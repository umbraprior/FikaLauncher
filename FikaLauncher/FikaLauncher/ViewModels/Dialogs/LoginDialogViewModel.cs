using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FikaLauncher.Services;
using Avalonia.Media;
using Avalonia.Platform;
using System;
using Avalonia.Styling;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Jeek.Avalonia.Localization;
using Avalonia.VisualTree;
using System.Linq;
using Material.Icons;
using System.ComponentModel;

namespace FikaLauncher.ViewModels.Dialogs;

public partial class LoginDialogViewModel : ViewModelBase
{
    [ObservableProperty] private IImage? _logoImage;
    [ObservableProperty] private double _logoWidth = 60;
    [ObservableProperty] private double _logoHeight = 60;
    [ObservableProperty] private string _language = LocalizationService.Instance.CurrentLanguage;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _canSubmit;
    [ObservableProperty] private bool _rememberLogin;
    [ObservableProperty] private bool _canLogin;
    [ObservableProperty] private bool _canCreateAccount;
    [ObservableProperty] private bool _keepMeLoggedIn;
    [ObservableProperty] private bool _isPasswordVisible;
    [ObservableProperty] private MaterialIconKind _isPasswordVisibleIcon = MaterialIconKind.Eye;

    public LoginDialogViewModel()
    {
        UpdateLogo(App.Current!.RequestedThemeVariant == ThemeVariant.Dark);
        LocalizationService.Instance.PropertyChanged += OnLanguageServiceChanged;
        _rememberLogin = ConfigurationService.Settings.RememberLogin;
        _keepMeLoggedIn = ConfigurationService.Settings.KeepLauncherOpen;

        if (ConfigurationService.Settings.RememberLogin)
        {
            var lastUsername = ApplicationStateService.GetLastLoggedInUsername();
            if (!string.IsNullOrEmpty(lastUsername)) Username = lastUsername;
        }
    }

    private void OnLanguageServiceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.CurrentLanguage))
            Language = LocalizationService.Instance.CurrentLanguage;
    }

    public override void Dispose()
    {
        LocalizationService.Instance.PropertyChanged -= OnLanguageServiceChanged;
        base.Dispose();
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

    partial void OnLanguageChanged(string value)
    {
        LocalizationService.ChangeLanguage(value);
        _ = RepositoryReadmeService.PreCacheReadmeAsync(value);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow?.InvalidateVisual();
    }

    partial void OnUsernameChanged(string value)
    {
        UpdateCanSubmit();
    }

    partial void OnPasswordChanged(string value)
    {
        UpdateCanSubmit();
    }

    private void UpdateCanSubmit()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            CanLogin = false;
            CanCreateAccount = false;
            return;
        }

        var userExists = UserService.UserExists(Username);
        CanLogin = userExists;
        CanCreateAccount = !userExists;
    }

    private async Task HandleSuccessfulLogin()
    {
        try
        {
            await AuthService.Login(Username);

            ConfigurationService.Settings.RememberLogin = RememberLogin;
            ConfigurationService.Settings.KeepLauncherOpen = KeepMeLoggedIn;
            await ConfigurationService.SaveSettingsAsync();

            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                var playView = mainWindow?.GetVisualDescendants()
                    .OfType<UserControl>()
                    .FirstOrDefault(x => x.DataContext is PlayViewModel);

                if (playView?.DataContext is PlayViewModel playViewModel)
                {
                    playViewModel.UpdateLoginState();
                    await Task.Delay(50);
                    playView.InvalidateVisual();
                    mainWindow?.InvalidateVisual();
                }
            }

            DialogService.CloseDialog(true);
        }
        catch (Exception ex)
        {
            NotificationController.ShowLoginError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task CreateAccount()
    {
        if (!CanCreateAccount) return;

        var (success, token, error) = await UserService.CreateUser(Username, Password);
        if (!success)
        {
            NotificationController.ShowAccountCreationError(error);
            return;
        }

        ApplicationStateService.SaveLoginState(Username, true, token);
        await HandleSuccessfulLogin();
    }

    [RelayCommand]
    private async Task Login()
    {
        if (!CanLogin) return;

        var isValid = await UserService.ValidateUser(Username, Password);
        if (isValid)
            await HandleSuccessfulLogin();
        else
            NotificationController.ShowInvalidCredentials();
    }

    partial void OnRememberLoginChanged(bool value)
    {
        if (!value)
        {
            KeepMeLoggedIn = false;
            Username = string.Empty;
        }

        ConfigurationService.Settings.RememberLogin = value;
        ConfigurationService.SaveSettings();
    }

    partial void OnKeepMeLoggedInChanged(bool value)
    {
        if (value && !RememberLogin) RememberLogin = true;
        ConfigurationService.Settings.KeepLauncherOpen = value;
        ConfigurationService.SaveSettings();
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
        IsPasswordVisibleIcon = IsPasswordVisible
            ? MaterialIconKind.EyeOff
            : MaterialIconKind.Eye;
    }

    [RelayCommand]
    private async Task HandleEnterKey()
    {
        if (CanLogin)
            await Login();
        else if (CanCreateAccount) await CreateAccount();
    }
}