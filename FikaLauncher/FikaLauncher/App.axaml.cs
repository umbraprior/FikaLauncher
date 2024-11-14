using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FikaLauncher.ViewModels;
using FikaLauncher.Views;
using FikaLauncher.Services;
using Avalonia.Styling;
using System;
using System.Threading.Tasks;
using FikaLauncher.ViewModels.Dialogs;
using FikaLauncher.Views.Dialogs;
using FikaLauncher.Services.GitHub;

namespace FikaLauncher;

public partial class App : Application
{
    public new static App? Current => Application.Current as App;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            DatabaseService.Initialize();
            AuthService.Initialize();
            _ = FileSystemService.AppDataDirectory;
            _ = ConfigurationService.Settings;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                GitHubRateLimitService.Instance.Initialize();
                var mainViewModel = new MainViewModel();
                desktop.MainWindow = new MainWindow(mainViewModel);

                ApplicationStateService.LoadState();
                var state = ApplicationStateService.GetCurrentState();
                if (!state.HasAcceptedLauncherTerms || !state.HasAcceptedFikaTerms)
                {
                    await Task.Delay(500);
                    await DialogService.ShowDialog<TermsDialogView>(new TermsDialogViewModel());
                }
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical error during application initialization: {ex}");
            throw;
        }
    }

    public static void ChangeTheme(bool isDark)
    {
        if (Current != null) Current.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}