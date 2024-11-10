using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FikaLauncher.Localization;

namespace FikaLauncher.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static readonly LocalizationService _instance = new();
    public static LocalizationService Instance => _instance;

    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyCollection<string> AvailableLanguages => Localizer.Languages;

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
                OnPropertyChanged(nameof(AvailableLanguages));

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
        CurrentLanguage = ConfigurationService.Settings.Language ?? "en-US";
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

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}