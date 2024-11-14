using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FikaLauncher.Services.Doc;
using System.Reflection;
using FikaLauncher.Services.GitHub;
using FikaLauncher.Services.Lang;

namespace FikaLauncher.Services;

public static class RepositoryReadmeService
{
    private static readonly IRepositoryService _repository;
    private const int CommitGracePeriodMinutes = 10;
    private static readonly RepositoryInfo _repoInfo;

    static RepositoryReadmeService()
    {
        _repoInfo = RepositoryConfiguration.GetRepository("FikaDocumentation");
        _repository = RepositoryServiceFactory.Create("https://api.github.com", _repoInfo);
    }

    private static string GetGitHubFilePath(string language)
    {
        return language == "en-US"
            ? "README.md"
            : $"{language.Replace("-", "_")}-README.md";
    }

    private static async Task<(string? commitHash, DateTime? commitDate)> GetLatestCommitInfo(string filePath)
    {
        var endpoint = $"readme/{filePath}";

        try
        {
            return await _repository.GetLatestCommitInfo(filePath).WithRateLimiting(endpoint);
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

            if (DateTime.UtcNow - commitDate.Value < TimeSpan.FromMinutes(CommitGracePeriodMinutes))
            {
                Console.WriteLine(
                    $"Skipping download for {language} - commit is too recent ({commitDate.Value:HH:mm:ss UTC})");
                return null;
            }

            var (cachedContent, cacheInfo) = await ReadmeCacheService.GetCachedReadme(language, commitHash);
            if (cachedContent != null && cacheInfo != null) return cachedContent;

            var content = await _repository.DownloadContent(filePath);
            if (content != null)
            {
                Console.WriteLine(
                    $"Successfully downloaded content (length: {content.Length}, commit: {commitHash[..7]})");
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

    private static string? GetEmbeddedReadme(string language)
    {
        try
        {
            var assembly = typeof(RepositoryReadmeService).Assembly;
            var resourceLanguage = language.Replace("-", "_");
            var resourcePath = $"FikaLauncher.Languages.{resourceLanguage}.README.md";

            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                Console.WriteLine($"No embedded readme found for {language}");
                return null;
            }

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            Console.WriteLine($"Successfully loaded embedded readme for {language}");
            return content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading embedded readme: {ex.Message}");
            return null;
        }
    }

    public static async Task<string> GetReadmeAsync(string language)
    {
        try
        {
            if (!GitHubRateLimitService.Instance.IsRateLimited)
            {
                var content = await TryGetGitHubReadme(language);
                if (content != null) return content;
            }

            var fallbackContent = await LanguageFallbackController.GetWithFallback(
                language,
                async (lang) => await GetLatestCachedReadme(lang),
                async (lang) => GetEmbeddedReadme(lang),
                "readme"
            );

            return fallbackContent ?? "# Error\nFailed to load documentation.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting readme content: {ex.Message}");
            if (LanguageFallbackController.ShouldUseFallback(ex))
            {
                var fallbackContent = await LanguageFallbackController.GetWithFallback(
                    language,
                    async (lang) => await GetLatestCachedReadme(lang),
                    async (lang) => GetEmbeddedReadme(lang),
                    "readme"
                );

                return fallbackContent ?? "# Error\nFailed to load documentation.";
            }

            return "# Error\nFailed to load documentation.";
        }
    }

    private static async Task<string?> GetLatestCachedReadme(string language)
    {
        try
        {
            var cacheDir = FileSystemService.CacheDirectory;
            var readmePrefix = $"readme-{language}-";
            var files = Directory.GetFiles(cacheDir, $"{readmePrefix}*.md");

            var latestDate = DateTime.MinValue;
            string? latestContent = null;

            foreach (var file in files)
            {
                var info = await ReadmeCacheService.ReadCacheInfo(file);
                if (info != null && info.CommitDate > latestDate)
                {
                    var content = await ReadmeCacheService.ReadFromCache(file);
                    if (content != null)
                    {
                        latestDate = info.CommitDate;
                        latestContent = content;
                    }
                }
            }

            return latestContent;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting latest cached readme: {ex.Message}");
            return null;
        }
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

    private static async Task<string?> TryGetGitHubReadme(string language)
    {
        try
        {
            var filePath = GetGitHubFilePath(language);
            var endpoint = $"readme/{filePath}";

            var (latestCommitHash, commitDate) = await _repository.GetLatestCommitInfo(filePath)
                .WithRateLimiting(endpoint);

            if (latestCommitHash == null || !commitDate.HasValue) return null;

            var (cachedContent, cacheInfo) = await ReadmeCacheService.GetCachedReadme(language, latestCommitHash);
            if (cachedContent != null && cacheInfo != null) return cachedContent;

            var content = await _repository.DownloadContent(filePath);
            if (content == null) return null;

            await ReadmeCacheService.SaveToCache(content, language, latestCommitHash, commitDate.Value);
            return content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting GitHub readme: {ex.Message}");
            return null;
        }
    }
}