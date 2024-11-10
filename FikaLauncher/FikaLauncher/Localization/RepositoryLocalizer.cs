using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FikaLauncher.Localization;

public class RepositoryLocalizer : BaseLocalizer
{
    private Dictionary<string, string>? _languageStrings;
    private readonly Dictionary<string, bool> _loadingLanguages = [];
    private readonly object _loadLock = new();

    public RepositoryLocalizer()
    {
        Task.Run(async () =>
        {
            await Services.LocaleDiscoveryService.DiscoverAvailableLocales();
            _languages.Clear();
            _languages.AddRange(Services.LocaleDiscoveryService.AvailableLocales);
            await LoadLanguageStrings(_language);
            _hasLoaded = true;
            RefreshUI();
        });
    }

    public override async void Reload()
    {
        _languageStrings = null;
        _languages.Clear();
        _languages.AddRange(Services.LocaleDiscoveryService.AvailableLocales);

        await LoadLanguageStrings(_language);
        _hasLoaded = true;
    }

    protected override async void SetLanguage(string language)
    {
        var oldLanguage = _language;
        _language = language;

        try
        {
            await LoadLanguageStrings(language);
            RefreshUI();
        }
        catch
        {
            _language = oldLanguage;
            throw;
        }
    }

    private async Task LoadLanguageStrings(string language)
    {
        lock (_loadLock)
        {
            if (_loadingLanguages.TryGetValue(language, out var isLoading) && isLoading)
                return;

            _loadingLanguages[language] = true;
        }

        try
        {
            var newStrings = await Services.RepositoryLocaleService.GetLocaleStrings(language);

            if (newStrings == null)
            {
                if (language != DefaultLanguage)
                {
                    _language = DefaultLanguage;
                    await LoadLanguageStrings(DefaultLanguage);
                }

                return;
            }

            var oldStrings = _languageStrings;
            _languageStrings = newStrings;

            if (oldStrings != null)
                oldStrings.Clear();
        }
        finally
        {
            lock (_loadLock)
            {
                _loadingLanguages[language] = false;
            }
        }
    }

    public override string Get(string key)
    {
        if (!_hasLoaded)
            return $"{Language}:{key}";

        if (_languageStrings == null)
            return $"{Language}:{key}";

        if (_languageStrings.TryGetValue(key, out var langStr))
            return langStr.Replace("\\n", "\n");

        return $"{Language}:{key}";
    }
}