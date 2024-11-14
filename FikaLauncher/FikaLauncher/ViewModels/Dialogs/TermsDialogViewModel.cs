using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FikaLauncher.Services;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using FikaLauncher.ViewModels;
using System.IO;
using System.Reflection;
using System;
using Avalonia.Controls;
using FikaLauncher.Views.Dialogs;
using Avalonia.VisualTree;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Styling;

namespace FikaLauncher.ViewModels.Dialogs;

public partial class TermsDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _termsHtml = string.Empty;

    [ObservableProperty] private bool _hasReadLauncherTerms;

    [ObservableProperty] private bool _hasAcceptedLauncherTerms;

    [ObservableProperty] private bool _hasReadFikaTerms;

    [ObservableProperty] private bool _hasAcceptedFikaTerms;

    [ObservableProperty] private bool _canContinue;

    [ObservableProperty] private bool _isLauncherTerms = true;

    [ObservableProperty] private bool _hasReadTerms;


    [ObservableProperty] private string _continueButtonText;

    [ObservableProperty] private bool _forceReload;

    [ObservableProperty] private Material.Icons.MaterialIconKind _themeIcon;

    [ObservableProperty] private string _themeTooltip;

    [ObservableProperty] private bool _hasScrolledToEnd;

    [ObservableProperty] private bool _hasAcceptedCurrentTerms;

    [ObservableProperty] private string _language = LocalizationService.Instance.CurrentLanguage;

    public TermsDialogViewModel()
    {
        ApplicationStateService.LoadState();
        var state = ApplicationStateService.GetCurrentState();
        Console.WriteLine(
            $"Initial state - Launcher: {state.HasAcceptedLauncherTerms}, Fika: {state.HasAcceptedFikaTerms}");

        HasAcceptedLauncherTerms = state.HasAcceptedLauncherTerms;
        HasAcceptedFikaTerms = state.HasAcceptedFikaTerms;

        if (!HasAcceptedLauncherTerms)
            IsLauncherTerms = true;
        else if (!HasAcceptedFikaTerms)
            IsLauncherTerms = false;

        Dispatcher.UIThread.Post(async () => await LoadTerms());
        UpdateThemeIcon();
        UpdateContinueButtonText();
        LocalizationService.Instance.PropertyChanged += OnLanguageServiceChanged;
    }

    private async Task LoadTerms(bool resetScroll = true, bool preserveReadState = false)
    {
        try
        {
            var isDark = false;
            if (App.Current != null) isDark = App.Current.RequestedThemeVariant == ThemeVariant.Dark;

            if (!preserveReadState)
                await RepositoryTermsService.PreCacheTermsAsync(LocalizationService.Instance.CurrentLanguage,
                    IsLauncherTerms);

            var terms = await RepositoryTermsService.GetProcessedTerms(isDark, IsLauncherTerms, preserveReadState);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var previousReadState = HasReadTerms;
                var previousAcceptedState = HasAcceptedCurrentTerms;

                TermsHtml = terms;
                ForceReload = !ForceReload;

                if (!preserveReadState)
                {
                    HasReadTerms = false;
                    HasAcceptedCurrentTerms = false;
                }
                else
                {
                    HasReadTerms = previousReadState;
                    HasAcceptedCurrentTerms = previousAcceptedState;
                }

                if (resetScroll && App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    var dialog = mainWindow?.GetVisualDescendants()
                        .OfType<TermsDialogView>()
                        .FirstOrDefault();
                    dialog?.ResetScroll();
                }

                UpdateCanContinue();
                UpdateContinueButtonText();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading terms: {ex.Message}");
            TermsHtml = "Error loading terms of use.";
        }
    }

    private void UpdateContinueButtonText()
    {
        if (IsLauncherTerms && !HasAcceptedFikaTerms)
            ContinueButtonText = "Next";
        else
            ContinueButtonText = "Get Started";
    }

    partial void OnHasScrolledToEndChanged(bool value)
    {
        if (value)
        {
            if (IsLauncherTerms)
            {
                HasReadLauncherTerms = true;
                HasReadTerms = true;
            }
            else
            {
                HasReadFikaTerms = true;
                HasReadTerms = true;
            }
        }
    }

    private void UpdateCanContinue()
    {
        CanContinue = HasReadTerms && HasAcceptedCurrentTerms;
    }

    private void UpdateThemeIcon()
    {
        var isDark = App.Current?.RequestedThemeVariant == ThemeVariant.Dark;
        ThemeIcon = isDark
            ? Material.Icons.MaterialIconKind.WeatherNight
            : Material.Icons.MaterialIconKind.WeatherSunny;
        ThemeTooltip = isDark ? "Light" : "Dark";
    }

    [RelayCommand]
    private async Task ToggleTheme()
    {
        var app = Application.Current;
        if (app == null) return;

        var isDark = app.ActualThemeVariant == ThemeVariant.Dark;

        ConfigurationService.Settings.IsDarkTheme = !isDark;
        App.ChangeTheme(!isDark);

        UpdateThemeIcon();

        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                mainViewModel.UpdateLogo(!isDark);

        await LoadTerms(false, true);

        await Task.Run(async () =>
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

        NotificationController.ShowThemeChanged(!isDark);
    }

    [RelayCommand]
    private async void GetStarted()
    {
        if (!CanContinue) return;

        var state = ApplicationStateService.GetCurrentState();
        Console.WriteLine(
            $"Before save - Launcher: {state.HasAcceptedLauncherTerms}, Fika: {state.HasAcceptedFikaTerms}");

        if (IsLauncherTerms)
        {
            state.HasAcceptedLauncherTerms = true;
            ApplicationStateService.SaveState();
            Console.WriteLine("Saved launcher terms acceptance");

            if (!HasAcceptedFikaTerms)
            {
                IsLauncherTerms = false;
                await Dispatcher.UIThread.InvokeAsync(async () => await LoadTerms());
                return;
            }
        }
        else
        {
            state.HasAcceptedFikaTerms = true;
            ApplicationStateService.SaveState();
            Console.WriteLine("Saved Fika terms acceptance");
        }

        DialogService.CloseDialog(true);
    }

    partial void OnHasAcceptedLauncherTermsChanged(bool value)
    {
        UpdateCanContinue();
    }

    partial void OnHasAcceptedFikaTermsChanged(bool value)
    {
        UpdateCanContinue();
    }

    partial void OnHasAcceptedCurrentTermsChanged(bool value)
    {
        if (IsLauncherTerms)
            HasAcceptedLauncherTerms = value;
        else
            HasAcceptedFikaTerms = value;
        UpdateCanContinue();
    }

    private void OnLanguageServiceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.CurrentLanguage))
        {
            Language = LocalizationService.Instance.CurrentLanguage;
            Dispatcher.UIThread.Post(async () => await LoadTerms());
        }
    }

    partial void OnLanguageChanged(string value)
    {
        _ = LocalizationService.ChangeLanguageAsync(value);
    }

    public override void Dispose()
    {
        LocalizationService.Instance.PropertyChanged -= OnLanguageServiceChanged;
        base.Dispose();
    }
}