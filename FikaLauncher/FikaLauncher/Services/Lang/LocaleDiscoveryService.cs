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
    private static readonly HashSet<string> _availableLocales = new() { "en-US" };
    private const string LocaleDirectory = "Languages";
    private const int CommitGracePeriodMinutes = 10;

    public static IReadOnlyCollection<string> AvailableLocales => _availableLocales;

    static LocaleDiscoveryService()
    {
        var repoInfo = RepositoryConfiguration.GetRepository("FikaLauncherTranslations");
        _repository = RepositoryServiceFactory.Create("https://api.github.com", repoInfo);
    }

    public static async Task DiscoverAvailableLocales()
    {
        try
        {
            var directories = await _repository.GetDirectoryContents(LocaleDirectory);
            if (directories == null) return;

            foreach (var dir in directories)
            {
                var locale = Path.GetFileName(dir);
                if (locale != "en-US") // Skip English as it's embedded
                {
                    var hasRequiredFiles = await ValidateLocaleDirectory(locale);
                    if (hasRequiredFiles)
                    {
                        _availableLocales.Add(locale);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error discovering locales: {ex.Message}");
        }
    }

    private static async Task<bool> ValidateLocaleDirectory(string locale)
    {
        var requiredFiles = new[]
        {
            $"{LocaleDirectory}/{locale}/strings.json",
            $"{LocaleDirectory}/{locale}/launcher-terms.md",
            $"{LocaleDirectory}/{locale}/fika-terms.md"
        };

        foreach (var file in requiredFiles)
        {
            if (!await _repository.DoesFileExist(file))
                return false;
        }

        return true;
    }

    public static async Task PreCacheLocaleAsync(string language)
    {
        if (language == "en-US") return; // Skip precaching for English
        
        try
        {
            var filePath = $"{LocaleDirectory}/{language}/strings.json";
            var (latestCommitHash, commitDate) = await _repository.GetLatestCommitInfo(filePath);
            if (latestCommitHash == null || commitDate == null)
                return;

            if (DateTime.UtcNow - commitDate.Value < TimeSpan.FromMinutes(CommitGracePeriodMinutes))
            {
                Console.WriteLine($"Skipping cache update for {language} - commit is too recent ({commitDate.Value:HH:mm:ss UTC})");
                return;
            }

            var (cachedContent, cacheInfo) = await LocaleCacheService.GetCachedLocale(language, latestCommitHash);
            if (cachedContent == null)
            {
                Console.WriteLine($"Cache needs updating for {language}, downloading new content");
                await RepositoryLocaleService.GetLocaleStrings(language);
            }
            else
            {
                Console.WriteLine($"Locale cache is up to date for {language} (commit: {cacheInfo?.CommitHash[..7]})");
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
            var filePath = $"{LocaleDirectory}/{language}/strings.json";
            var (latestCommitHash, _) = await _repository.GetLatestCommitInfo(filePath);
            if (latestCommitHash == null)
                return false;

            var (_, cacheInfo) = await LocaleCacheService.GetCachedLocale(language, latestCommitHash);
            return cacheInfo == null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking locale cache status: {ex.Message}");
            return false;
        }
    }
}