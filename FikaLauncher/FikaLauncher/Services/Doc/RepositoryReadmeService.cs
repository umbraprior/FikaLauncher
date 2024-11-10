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
            var result = await _repository.GetLatestCommitInfo(filePath);
            if (result.commitHash == null && result.commitDate == null)
            {
                NotificationController.ShowGitHubRateLimited();
            }
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting commit info: {ex.Message}");
            if (ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            {
                NotificationController.ShowGitHubRateLimited();
            }
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

            var (_, cacheInfo) = await ReadmeCacheService.GetCachedReadme(language, latestCommitHash);
            return cacheInfo == null;
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

            if (DateTime.UtcNow - commitDate.Value < TimeSpan.FromMinutes(15))
            {
                Console.WriteLine($"Skipping download for {language} - commit is too recent ({commitDate.Value:HH:mm:ss UTC})");
                return null;
            }

            var (cachedContent, cacheInfo) = await ReadmeCacheService.GetCachedReadme(language, commitHash);
            if (cachedContent != null && cacheInfo != null)
            {
                return cachedContent;
            }

            var content = await _repository.DownloadContent(filePath);
            if (content != null)
            {
                Console.WriteLine($"Successfully downloaded content (length: {content.Length}, commit: {commitHash[..7]})");
                await ReadmeCacheService.SaveToCache(content, language, commitHash, commitDate.Value);
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
        Console.WriteLine($"Getting readme for language: {currentLanguage}");

        try
        {
            if (currentLanguage != "en-US" && !await DoesLanguageReadmeExist(currentLanguage))
            {
                Console.WriteLine($"No {currentLanguage} readme exists, using English");
                return await GetEnglishContent();
            }

            var filePath = GetGitHubFilePath(currentLanguage);
            var (latestCommitHash, _) = await GetLatestCommitInfo(filePath);
            if (latestCommitHash == null)
                return await GetEnglishContent();

            var (cachedContent, cacheInfo) = await ReadmeCacheService.GetCachedReadme(currentLanguage, latestCommitHash);
            if (cachedContent != null)
                return cachedContent;

            var downloadedContent = await DownloadReadmeContent(currentLanguage);
            return downloadedContent ?? await GetEnglishContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting readme content: {ex.Message}");
            return await GetEnglishContent();
        }
    }

    private static async Task<string> GetEnglishContent()
    {
        var (latestCommitHash, _) = await GetLatestCommitInfo("README.md");
        if (latestCommitHash == null)
            return "# Error\nFailed to load documentation from GitHub.";

        var (cachedContent, _) = await ReadmeCacheService.GetCachedReadme("en-US", latestCommitHash);
        if (cachedContent != null)
            return cachedContent;

        var downloadedContent = await DownloadReadmeContent("en-US");
        return downloadedContent ?? "# Error\nFailed to load documentation from GitHub.";
    }

    public static async Task PreCacheReadmeAsync(string language)
    {
        try
        {
            if (language != "en-US" && !await DoesLanguageReadmeExist(language))
            {
                Console.WriteLine($"No readme exists for {language}, using English");
                language = "en-US";
            }

            var filePath = GetGitHubFilePath(language);
            var (latestCommitHash, _) = await GetLatestCommitInfo(filePath);
            if (latestCommitHash == null)
                return;

            var (_, cacheInfo) = await ReadmeCacheService.GetCachedReadme(language, latestCommitHash);
            if (cacheInfo == null)
            {
                Console.WriteLine($"Cache needs updating for {language}, downloading new content");
                await DownloadReadmeContent(language);
            }
            else
            {
                Console.WriteLine($"Cache is up to date for {language} (commit: {cacheInfo.CommitHash[..7]})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PreCacheReadmeAsync: {ex.Message}");
        }
    }
}