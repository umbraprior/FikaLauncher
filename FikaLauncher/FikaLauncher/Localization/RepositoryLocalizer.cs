using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using FikaLauncher.Services;

namespace FikaLauncher.Localization;

public class RepositoryLocalizer : BaseLocalizer
{
    private Dictionary<string, string>? _languageStrings;
    private readonly Dictionary<string, bool> _loadingLanguages = [];
    private readonly object _loadLock = new();
    private readonly JsonLocalizer _fallbackLocalizer = new();

    public RepositoryLocalizer()
    {
        Task.Run(async () =>
        {
            await LocaleDiscoveryService.DiscoverAvailableLocales();
            _languages.Clear();
            _languages.AddRange(LocaleDiscoveryService.AvailableLocales);
            await LoadLanguageStrings(_language);
            _hasLoaded = true;
            RefreshUI();
        });
    }

    public override async void Reload()
    {
        _languageStrings = null;
        _languages.Clear();
        _languages.AddRange(LocaleDiscoveryService.AvailableLocales);

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
            try
            {
                var (strings, cacheInfo) = await RepositoryLocaleService.GetLocaleStringsWithInfo(language);
                if (strings != null && cacheInfo != null)
                {
                    _languageStrings = strings;
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load from repository: {ex.Message}");
            }

            try
            {
                var cacheFiles = Directory.GetFiles(
                    FileSystemService.CacheDirectory,
                    $"locale-{language}-*.json"
                );

                if (cacheFiles.Length > 0)
                {
                    var latestCache = cacheFiles
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.LastWriteTimeUtc)
                        .First();

                    var cachedContent = await LocaleCacheService.ReadFromCache(latestCache.FullName);
                    if (cachedContent != null)
                    {
                        _languageStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(cachedContent);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load from cache: {ex.Message}");
            }

            try
            {
                if (!_fallbackLocalizer.Languages.Contains(language))
                {
                    if (language != DefaultLanguage)
                    {
                        _language = DefaultLanguage;
                        await LoadLanguageStrings(DefaultLanguage);
                    }

                    return;
                }

                _fallbackLocalizer.Language = language;
                _languageStrings = GetFallbackStrings(language);
                Console.WriteLine($"Successfully loaded fallback strings for language: {language}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load fallback strings: {ex.Message}");
                if (language != DefaultLanguage)
                {
                    _language = DefaultLanguage;
                    await LoadLanguageStrings(DefaultLanguage);
                }
            }
        }
        finally
        {
            lock (_loadLock)
            {
                _loadingLanguages[language] = false;
            }
        }
    }

    private Dictionary<string, string> GetFallbackStrings(string language)
    {
        var result = new Dictionary<string, string>();
        foreach (var key in _fallbackLocalizer.GetAllKeys()) result[key] = _fallbackLocalizer.Get(key);
        return result;
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

    public override IEnumerable<string> GetAllKeys()
    {
        if (!_hasLoaded || _languageStrings == null)
            return _fallbackLocalizer.GetAllKeys();

        return _languageStrings.Keys;
    }
}