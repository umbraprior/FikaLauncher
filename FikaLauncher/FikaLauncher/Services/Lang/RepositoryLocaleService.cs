using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using FikaLauncher.Services.Doc;
using System.Collections.Generic;

namespace FikaLauncher.Services;

public static class RepositoryLocaleService
{
    private static readonly IRepositoryService _repository;

    static RepositoryLocaleService()
    {
        var repoInfo = RepositoryConfiguration.GetRepository("FikaLauncher");
        _repository = RepositoryServiceFactory.Create("https://github.com", repoInfo);
    }

    public static async Task<(Dictionary<string, string>? strings, LocaleCacheService.LocaleInfo? info)>
        GetLocaleStringsWithInfo(string language)
    {
        try
        {
            var filePath = $"Languages/{language}.json";
            var (latestCommitHash, commitDate) = await _repository.GetLatestCommitInfo(filePath);

            if (latestCommitHash == null || commitDate == null)
                return (null, null);

            // Add 15-minute delay check for recent commits
            if (DateTime.UtcNow - commitDate.Value < TimeSpan.FromMinutes(15))
            {
                Console.WriteLine(
                    $"Skipping download for {language} - commit is too recent ({commitDate.Value:HH:mm:ss UTC})");
                return (null, null);
            }

            // Check cache first
            var (cachedContent, cacheInfo) = await LocaleCacheService.GetCachedLocale(language, latestCommitHash);
            if (cachedContent != null && cacheInfo != null)
                return (JsonSerializer.Deserialize<Dictionary<string, string>>(cachedContent), cacheInfo);

            // Download if not in cache
            var content = await _repository.DownloadContent(filePath);
            if (content == null)
            {
                if (language == "en-US")
                    return (null, null);

                return await GetLocaleStringsWithInfo("en-US");
            }

            await LocaleCacheService.SaveLocaleToCache(content, language, latestCommitHash,
                commitDate ?? DateTime.UtcNow);
            return (JsonSerializer.Deserialize<Dictionary<string, string>>(content),
                new LocaleCacheService.LocaleInfo
                    { CommitHash = latestCommitHash, CommitDate = commitDate ?? DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load locale strings: {ex.Message}");
            return (null, null);
        }
    }

    public static async Task<Dictionary<string, string>?> GetLocaleStrings(string language)
    {
        var (strings, _) = await GetLocaleStringsWithInfo(language);
        return strings;
    }
}