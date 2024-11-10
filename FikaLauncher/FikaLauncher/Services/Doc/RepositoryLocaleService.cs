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

    private static string GetLocalePath(string language)
    {
        return $"Languages/{language}.json";
    }

    private static async Task<(string? commitHash, DateTime? commitDate)> GetLatestCommitInfo(string filePath)
    {
        try
        {
            return await _repository.GetLatestCommitInfo(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting commit info: {ex.Message}");
            return (null, null);
        }
    }

    private static async Task<bool> ShouldUpdateCache(string language)
    {
        try
        {
            var filePath = GetLocalePath(language);
            var (latestCommitHash, _) = await GetLatestCommitInfo(filePath);
            if (latestCommitHash == null)
                return false;

            var cacheFilePath = LocaleCacheService.GetCacheFilePath(language);
            if (!File.Exists(cacheFilePath))
                return true;

            var cachedInfo = await LocaleCacheService.ReadCacheInfo(cacheFilePath);
            if (cachedInfo == null)
                return true;

            var needsUpdate = cachedInfo.CommitHash != latestCommitHash;
            Console.WriteLine(
                $"Cache commit: {cachedInfo.CommitHash[..7]}, Latest commit: {latestCommitHash[..7]}, Needs update: {needsUpdate}");
            return needsUpdate;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking cache status: {ex.Message}");
            return false;
        }
    }

    private static async Task<string?> DownloadLocaleContent(string language)
    {
        var filePath = GetLocalePath(language);
        Console.WriteLine($"[Locale] Attempting to load locale from: {filePath}");

        try
        {
            var (commitHash, commitDate) = await GetLatestCommitInfo(filePath);
            if (commitHash == null || !commitDate.HasValue)
                return null;

            var content = await _repository.DownloadContent(filePath);
            if (content != null)
            {
                Console.WriteLine($"Successfully downloaded locale (length: {content.Length}, commit: {commitHash[..7]})");
                await LocaleCacheService.SaveToCache(content, LocaleCacheService.GetCacheFilePath(language),
                    commitHash, commitDate.Value);
                return content;
            }

            Console.WriteLine("[Locale] Download failed");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading locale: {ex.Message}");
            return null;
        }
    }

    public static async Task<Dictionary<string, string>?> GetLocaleStrings(string language)
    {
        try
        {
            var cacheFilePath = LocaleCacheService.GetCacheFilePath(language);

            Console.WriteLine($"[Locale] Getting strings for language: {language}");

            string? content;
            if (File.Exists(cacheFilePath) && !await ShouldUpdateCache(language))
            {
                Console.WriteLine("[Locale] Using existing cache");
                content = await LocaleCacheService.ReadFromCache(cacheFilePath);
                if (content != null)
                {
                    Console.WriteLine($"[Locale] Successfully loaded cached strings for {language}");
                    return JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                }
            }

            content = await DownloadLocaleContent(language);
            if (content == null)
            {
                Console.WriteLine($"[Locale] No localized version found for {language}");
                content = await GetEnglishLocale();
            }

            return JsonSerializer.Deserialize<Dictionary<string, string>>(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load locale strings: {ex.Message}");
            return null;
        }
    }

    private static async Task<string> GetEnglishLocale()
    {
        var englishCachePath = LocaleCacheService.GetCacheFilePath("en-US");
        
        if (File.Exists(englishCachePath) && !await ShouldUpdateCache("en-US"))
        {
            var content = await LocaleCacheService.ReadFromCache(englishCachePath);
            if (content != null)
                return content;
        }

        var downloadedContent = await DownloadLocaleContent("en-US");
        return downloadedContent ?? "{}";
    }
}
