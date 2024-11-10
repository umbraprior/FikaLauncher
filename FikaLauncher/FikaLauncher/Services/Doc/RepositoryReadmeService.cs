using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FikaLauncher.Services.Doc;

namespace FikaLauncher.Services;

public static class RepositoryReadmeService
{
    private static readonly IRepositoryService _repository;

    static RepositoryReadmeService()
    {
        var repoInfo = RepositoryConfiguration.GetRepository("FikaDocumentation");
        _repository = RepositoryServiceFactory.Create("https://github.com", repoInfo);
    }

    private static string GetGitHubFilePath(string language)
    {
        return language == "en-US"
            ? "README.md"
            : $"{language.Replace("-", "_")}-README.md";
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
            var filePath = GetGitHubFilePath(language);
            var (latestCommitHash, _) = await GetLatestCommitInfo(filePath);
            if (latestCommitHash == null)
                return false;

            var cacheFilePath = ReadmeCacheService.GetCacheFilePath(language);
            if (!File.Exists(cacheFilePath))
                return true;

            var cachedInfo = await ReadmeCacheService.ReadCacheInfo(cacheFilePath);
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

    private static async Task<string?> DownloadReadmeContent(string language)
    {
        var filePath = GetGitHubFilePath(language);
        Console.WriteLine($"Downloading readme: {filePath}");

        try
        {
            var (commitHash, commitDate) = await GetLatestCommitInfo(filePath);
            if (commitHash == null || !commitDate.HasValue)
                return null;

            var content = await _repository.DownloadContent(filePath);
            if (content != null)
            {
                Console.WriteLine($"Successfully downloaded content (length: {content.Length}, commit: {commitHash[..7]})");
                await ReadmeCacheService.SaveToCache(content, ReadmeCacheService.GetCacheFilePath(language), commitHash,
                    commitDate.Value);
                return content;
            }

            Console.WriteLine("Download failed");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading content: {ex.Message}");
            return null;
        }
    }

    private static async Task<bool> DoesLanguageReadmeExist(string language)
    {
        var filePath = GetGitHubFilePath(language);
        try
        {
            return await _repository.DoesFileExist(filePath);
        }
        catch
        {
            return false;
        }
    }

    public static async Task<string> GetReadmeContentAsync()
    {
        var currentLanguage = LocalizationService.Instance.CurrentLanguage;
        var cacheFilePath = ReadmeCacheService.GetCacheFilePath(currentLanguage);

        Console.WriteLine($"Getting readme for language: {currentLanguage}");

        try
        {
            if (currentLanguage != "en-US" && !await DoesLanguageReadmeExist(currentLanguage))
            {
                Console.WriteLine($"No {currentLanguage} readme exists, using English");
                return await GetEnglishContent(ReadmeCacheService.GetCacheFilePath("en-US"));
            }

            if (File.Exists(cacheFilePath) && !await ShouldUpdateCache(currentLanguage))
            {
                Console.WriteLine("Using existing cache");
                var content = await ReadmeCacheService.ReadFromCache(cacheFilePath);
                if (content != null)
                    return content;
            }

            var downloadedContent = await DownloadReadmeContent(currentLanguage);
            return downloadedContent ?? await GetEnglishContent(ReadmeCacheService.GetCacheFilePath("en-US"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting readme content: {ex.Message}");
            return await GetEnglishContent(ReadmeCacheService.GetCacheFilePath("en-US"));
        }
    }

    private static async Task<string> GetEnglishContent(string englishCachePath)
    {
        if (File.Exists(englishCachePath) && !await ShouldUpdateCache("en-US"))
        {
            var content = await ReadmeCacheService.ReadFromCache(englishCachePath);
            if (content != null)
                return content;
        }

        var downloadedContent = await DownloadReadmeContent("en-US");
        return downloadedContent ?? "# Error\nFailed to load documentation from GitHub.";
    }

    public static async Task PreCacheReadmeAsync(string language)
    {
        try
        {
            var cacheFilePath = ReadmeCacheService.GetCacheFilePath(language);

            if (language != "en-US" && !await DoesLanguageReadmeExist(language))
            {
                Console.WriteLine($"No readme exists for {language}, using English");
                language = "en-US";
                cacheFilePath = ReadmeCacheService.GetCacheFilePath("en-US");
            }

            if (await ShouldUpdateCache(language))
            {
                Console.WriteLine($"Cache needs updating for {language}, downloading new content");
                await DownloadReadmeContent(language);
            }
            else
            {
                var cachedInfo = await ReadmeCacheService.ReadCacheInfo(cacheFilePath);
                Console.WriteLine($"Cache is up to date for {language} (commit: {cachedInfo?.CommitHash[..7]})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PreCacheReadmeAsync: {ex.Message}");
        }
    }
}