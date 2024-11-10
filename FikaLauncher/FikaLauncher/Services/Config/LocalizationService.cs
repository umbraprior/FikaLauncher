using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Jeek.Avalonia.Localization;

namespace FikaLauncher.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static readonly LocalizationService _instance = new();
    public static LocalizationService Instance => _instance;

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _currentLanguage = "en-US";

    public string CurrentLanguage
    {
        get => _currentLanguage;
        private set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                Localizer.Language = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Localizer.Languages));

                Dispatcher.UIThread.Post(() =>
                {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var mainWindow = desktop.MainWindow;
                        mainWindow?.GetVisualDescendants()
                            .OfType<ComboBox>()
                            .ToList()
                            .ForEach(cb => cb.InvalidateVisual());
                    }
                });
            }
        }
    }

    private LocalizationService()
    {
        Initialize();
    }

    private void Initialize()
    {
        Localizer.SetLocalizer(new JsonLocalizer());
        CurrentLanguage = "en-US";
    }

    public static async Task ChangeLanguageAsync(string language)
    {
        if (Instance.CurrentLanguage != language)
        {
            ConfigurationService.Settings.Language = language;
            Instance.CurrentLanguage = language;

            await Task.Run(async () =>
            {
                await ConfigurationService.SaveSettingsAsync();
                await RepositoryReadmeService.PreCacheReadmeAsync(language);
            });
        }
    }

    public static void ChangeLanguage(string language)
    {
        _ = ChangeLanguageAsync(language);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}