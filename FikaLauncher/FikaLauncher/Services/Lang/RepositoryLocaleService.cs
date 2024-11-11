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
    private const string BaseUrl = "https://raw.githubusercontent.com";
    private const string RepoPath = "umbraprior/FikaLauncher";
    private const string Branch = "refs/heads/main";
    private const int CommitGracePeriodMinutes = 10;

    static RepositoryLocaleService()
    {
        var repoInfo = RepositoryConfiguration.GetRepository("FikaLauncher");
        _repository = RepositoryServiceFactory.Create(BaseUrl, repoInfo);
    }

    public static string GetGitHubPath(string relativePath)
    {
        return $"{BaseUrl}/{RepoPath}/{Branch}/{relativePath}";
    }

    public static async Task<(Dictionary<string, string>? strings, LocaleCacheService.LocaleInfo? info)>
        GetLocaleStringsWithInfo(string language)
    {
        try
        {
            var filePath = $"Languages/{language}/strings.json";
            var fullPath = GetGitHubPath(filePath);
            var (latestCommitHash, commitDate) = await _repository.GetLatestCommitInfo(filePath);

            if (latestCommitHash == null || commitDate == null)
                return (null, null);
            
            if (DateTime.UtcNow - commitDate.Value < TimeSpan.FromMinutes(CommitGracePeriodMinutes))
            {
                Console.WriteLine(
                    $"Skipping download for {language} - commit is too recent ({commitDate.Value:HH:mm:ss UTC})");
                return (null, null);
            }
            
            var (cachedContent, cacheInfo) = await LocaleCacheService.GetCachedLocale(language, latestCommitHash);
            if (cachedContent != null && cacheInfo != null)
                return (JsonSerializer.Deserialize<Dictionary<string, string>>(cachedContent), cacheInfo);
            
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