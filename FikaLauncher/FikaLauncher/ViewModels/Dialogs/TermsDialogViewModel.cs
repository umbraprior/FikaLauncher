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

namespace FikaLauncher.ViewModels.Dialogs;

public partial class TermsDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _termsHtml = string.Empty;

    [ObservableProperty]
    private bool _hasReadLauncherTerms;

    [ObservableProperty]
    private bool _hasAcceptedLauncherTerms;

    [ObservableProperty]
    private bool _hasReadFikaTerms;

    [ObservableProperty]
    private bool _hasAcceptedFikaTerms;

    [ObservableProperty]
    private bool _canContinue;

    [ObservableProperty]
    private bool _isLauncherTerms = true;

    [ObservableProperty]
    private bool _hasReadTerms;


    [ObservableProperty]
    private string _continueButtonText;

    [ObservableProperty]
    private bool _forceReload;

    [ObservableProperty]
    private Material.Icons.MaterialIconKind _themeIcon;

    [ObservableProperty]
    private string _themeTooltip;

    [ObservableProperty]
    private bool _hasScrolledToEnd;

    [ObservableProperty]
    private bool _hasAcceptedCurrentTerms;

    [ObservableProperty]
    private string _language = LocalizationService.Instance.CurrentLanguage;

    public TermsDialogViewModel()
    {
        ApplicationStateService.LoadState();
        var state = ApplicationStateService.GetCurrentState();
        Console.WriteLine($"Initial state - Launcher: {state.HasAcceptedLauncherTerms}, Fika: {state.HasAcceptedFikaTerms}");
        
        HasAcceptedLauncherTerms = state.HasAcceptedLauncherTerms;
        HasAcceptedFikaTerms = state.HasAcceptedFikaTerms;
        
        if (!HasAcceptedLauncherTerms)
        {
            IsLauncherTerms = true;
        }
        else if (!HasAcceptedFikaTerms)
        {
            IsLauncherTerms = false;
        }
        
        LoadTerms();
        UpdateThemeIcon();
        UpdateContinueButtonText();
        LocalizationService.Instance.PropertyChanged += OnLanguageServiceChanged;
    }

    private void LoadTerms()
    {
        var isDark = App.Current?.RequestedThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
        TermsHtml = TermsService.GetProcessedTerms(isDark, IsLauncherTerms);
        ForceReload = !ForceReload;
        
        HasReadTerms = false;
        HasAcceptedCurrentTerms = false;
        
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            var dialog = mainWindow?.GetVisualDescendants()
                .OfType<TermsDialogView>()
                .FirstOrDefault();
            dialog?.ResetScroll();
        }
        
        UpdateCanContinue();
        UpdateContinueButtonText();
    }

    private void UpdateContinueButtonText()
    {
        if (IsLauncherTerms && !HasAcceptedFikaTerms)
        {
            ContinueButtonText = "Next";
        }
        else
        {
            ContinueButtonText = "Get Started";
        }
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
        var isDark = App.Current?.RequestedThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
        ThemeIcon = isDark ? Material.Icons.MaterialIconKind.WeatherNight : Material.Icons.MaterialIconKind.WeatherSunny;
        ThemeTooltip = isDark ? "Light" : "Dark";
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        var isDark = App.Current?.RequestedThemeVariant != Avalonia.Styling.ThemeVariant.Dark;
        ConfigurationService.Settings.IsDarkTheme = isDark;
        ConfigurationService.SaveSettings();
        App.ChangeTheme(isDark);
        
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.UpdateLogo(isDark);
            }
        }
        
        LoadTerms();
        UpdateThemeIcon();
    }

    [RelayCommand]
    private void GetStarted()
    {
        if (!CanContinue) return;

        var state = ApplicationStateService.GetCurrentState();
        Console.WriteLine($"Before save - Launcher: {state.HasAcceptedLauncherTerms}, Fika: {state.HasAcceptedFikaTerms}");
        
        if (IsLauncherTerms)
        {
            state.HasAcceptedLauncherTerms = true;
            ApplicationStateService.SaveState();
            Console.WriteLine("Saved launcher terms acceptance");
            
            if (!HasAcceptedFikaTerms)
            {
                IsLauncherTerms = false;
                LoadTerms();
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
        {
            HasAcceptedLauncherTerms = value;
        }
        else
        {
            HasAcceptedFikaTerms = value;
        }
        UpdateCanContinue();
    }

    private void OnLanguageServiceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.CurrentLanguage))
        {
            Language = LocalizationService.Instance.CurrentLanguage;
            LoadTerms();
        }
    }

    partial void OnLanguageChanged(string value)
    {
        LocalizationService.ChangeLanguage(value);
    }

    public override void Dispose()
    {
        LocalizationService.Instance.PropertyChanged -= OnLanguageServiceChanged;
        base.Dispose();
    }
}
