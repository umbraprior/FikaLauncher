using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FikaLauncher.Services.Doc;

namespace FikaLauncher.Services;

public static class LocaleDiscoveryService
{
    private static readonly IRepositoryService _repository;
    private static readonly HashSet<string> _availableLocales = new();
    private const string LocaleDirectory = "Languages";

    static LocaleDiscoveryService()
    {
        var repoInfo = RepositoryConfiguration.GetRepository("FikaLauncher");
        _repository = RepositoryServiceFactory.Create("https://github.com", repoInfo);
        _availableLocales.Add("en-US"); // Always available
    }

    public static IReadOnlyCollection<string> AvailableLocales => _availableLocales;

    public static async Task DiscoverAvailableLocales()
    {
        try
        {
            var files = await _repository.GetDirectoryContents(LocaleDirectory);
            if (files == null) return;

            foreach (var file in files.Where(f => f.EndsWith(".json")))
            {
                var locale = file.Replace(".json", "");
                _availableLocales.Add(locale);
                Console.WriteLine($"Discovered locale: {locale}");
            }

            // Pre-cache all discovered locales
            foreach (var locale in _availableLocales) await PreCacheLocaleAsync(locale);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error discovering locales: {ex.Message}");
        }
    }

    private static async Task PreCacheLocaleAsync(string language)
    {
        try
        {
            var cacheFilePath = LocaleCacheService.GetCacheFilePath(language);

            if (await ShouldUpdateLocaleCache(language))
            {
                Console.WriteLine($"Cache needs updating for {language}, downloading new content");
                await RepositoryLocaleService.GetLocaleStrings(language);
            }
            else
            {
                var cachedInfo = await LocaleCacheService.ReadCacheInfo(cacheFilePath);
                Console.WriteLine($"Locale cache is up to date for {language} (commit: {cachedInfo?.CommitHash[..7]})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PreCacheLocaleAsync: {ex.Message}");
        }
    }

    private static async Task<bool> ShouldUpdateLocaleCache(string language)
    {
        try
        {
            var filePath = $"{LocaleDirectory}/{language}.json";
            var (latestCommitHash, _) = await _repository.GetLatestCommitInfo(filePath);
            if (latestCommitHash == null)
                return false;

            var cacheFilePath = LocaleCacheService.GetCacheFilePath(language);
            if (!File.Exists(cacheFilePath))
                return true;

            var cachedInfo = await LocaleCacheService.ReadCacheInfo(cacheFilePath);
            if (cachedInfo == null)
                return true;

            return cachedInfo.CommitHash != latestCommitHash;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking locale cache status: {ex.Message}");
            return false;
        }
    }
}