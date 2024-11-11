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
    private static readonly HashSet<string> _availableLocales = new() { "en-US" }; // Always include English
    private const string BaseUrl = "https://raw.githubusercontent.com";
    private const string RepoPath = "umbraprior/FikaLauncher";
    private const string Branch = "refs/heads/main";
    private const string LocaleDirectory = "Languages";
    private const int CommitGracePeriodMinutes = 10;

    public static IReadOnlyCollection<string> AvailableLocales => _availableLocales;

    static LocaleDiscoveryService()
    {
        var repoInfo = RepositoryConfiguration.GetRepository("FikaLauncher");
        _repository = RepositoryServiceFactory.Create(BaseUrl, repoInfo);
    }

    private static string GetGitHubPath(string relativePath)
    {
        return $"{BaseUrl}/{RepoPath}/{Branch}/{relativePath}";
    }

    public static async Task DiscoverAvailableLocales()
    {
        try
        {
            var directories = await _repository.GetDirectoryContents(LocaleDirectory);
            if (directories == null) return;

            foreach (var dir in directories)
            {
                if (dir.EndsWith("/"))
                {
                    var locale = dir.TrimEnd('/');
                    var stringsPath = GetGitHubPath($"{LocaleDirectory}/{locale}/strings.json");
                    if (await _repository.DoesFileExist(stringsPath))
                    {
                        _availableLocales.Add(locale);
                        Console.WriteLine($"Discovered locale: {locale}");
                    }
                }
            }

            // Only pre-cache English and current system language if different
            var currentLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
            await PreCacheLocaleAsync("en-US");

            if (currentLanguage != "en-US" && _availableLocales.Contains(currentLanguage))
                await PreCacheLocaleAsync(currentLanguage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error discovering locales: {ex.Message}");
        }
    }

    public static async Task PreCacheLocaleAsync(string language)
    {
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