using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using FikaLauncher.Services.Doc;
using System.Collections.Generic;
using System.Reflection;
using FikaLauncher.Services.GitHub;
using FikaLauncher.Services.Lang;

namespace FikaLauncher.Services;

public static class RepositoryLocaleService
{
    private static readonly IRepositoryService _repository;
    private const string BaseUrl = "https://raw.githubusercontent.com";
    private const int CommitGracePeriodMinutes = 10;
    private static Dictionary<string, string>? _cachedEnglishStrings;

    static RepositoryLocaleService()
    {
        var repoInfo = RepositoryConfiguration.GetRepository("FikaLauncherTranslations");
        _repository = RepositoryServiceFactory.Create("https://api.github.com", repoInfo);
    }

    public static string GetGitHubPath(string relativePath)
    {
        return $"{BaseUrl}/{relativePath}";
    }

    public static async Task<(Dictionary<string, string>? strings, LocaleCacheService.LocaleInfo? info)>
        GetLocaleStringsWithInfo(string language)
    {
        try
        {
            if (language == "en-US" && IsResourceEmbedded(language, "strings"))
            {
                if (_cachedEnglishStrings != null)
                    return (_cachedEnglishStrings, new LocaleCacheService.LocaleInfo
                    {
                        CommitHash = "embedded",
                        CommitDate = DateTime.UtcNow
                    });

                var embeddedStrings = await LoadEmbeddedStrings();
                if (embeddedStrings != null)
                {
                    _cachedEnglishStrings = embeddedStrings;
                    return (embeddedStrings, new LocaleCacheService.LocaleInfo
                    {
                        CommitHash = "embedded",
                        CommitDate = DateTime.UtcNow
                    });
                }
            }

            var filePath = $"Languages/{language}/strings.json";
            var endpoint = $"locale/{filePath}";

            var commitInfo = await _repository.GetLatestCommitInfo(filePath).WithRateLimiting(endpoint);
            var (latestCommitHash, commitDate) = commitInfo;

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
        try
        {
            if (!GitHubRateLimitService.Instance.IsRateLimited)
            {
                var content = await TryGetGitHubLocale(language);
                if (content != null) return content;
            }

            var fallbackContent = await LanguageFallbackController.GetWithFallback(
                language,
                async (lang) => await GetLatestCachedLocale(lang),
                async (lang) => await GetEmbeddedLocale(lang),
                "locale"
            );

            return fallbackContent;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting locale content: {ex.Message}");
            if (LanguageFallbackController.ShouldUseFallback(ex))
            {
                var fallbackContent = await LanguageFallbackController.GetWithFallback(
                    language,
                    async (lang) => await GetLatestCachedLocale(lang),
                    async (lang) => await GetEmbeddedLocale(lang),
                    "locale"
                );

                return fallbackContent;
            }

            return null;
        }
    }

    private static async Task<Dictionary<string, string>?> GetLatestCachedLocale(string language)
    {
        try
        {
            var cacheDir = FileSystemService.CacheDirectory;
            var localePrefix = $"locale-{language}-";
            var files = Directory.GetFiles(cacheDir, $"{localePrefix}*.json");

            var latestDate = DateTime.MinValue;
            Dictionary<string, string>? latestContent = null;

            foreach (var file in files)
            {
                var info = await LocaleCacheService.ReadCacheInfo(file);
                if (info != null && info.CommitDate > latestDate)
                {
                    var content = await LocaleCacheService.ReadFromCache(file);
                    if (content != null)
                    {
                        latestDate = info.CommitDate;
                        latestContent = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                    }
                }
            }

            return latestContent;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting latest cached locale: {ex.Message}");
            return null;
        }
    }

    private static async Task<Dictionary<string, string>?> GetEmbeddedLocale(string language)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "FikaLauncher.Languages.en-US.strings.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return null;

            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading embedded strings: {ex.Message}");
            return null;
        }
    }

    private static bool IsResourceEmbedded(string language, string resourceType)
    {
        if (language != "en-US") return false;

        var assembly = Assembly.GetExecutingAssembly();
        var resourceLanguage = language.Replace("-", "_");

        switch (resourceType.ToLower())
        {
            case "strings":
                return assembly.GetManifestResourceStream($"FikaLauncher.Languages.{resourceLanguage}.strings.json") !=
                       null;
            case "terms":
                var launcherTerms =
                    assembly.GetManifestResourceStream(
                        $"FikaLauncher.Languages.{resourceLanguage}.launcher-terms.md") != null;
                var fikaTerms =
                    assembly.GetManifestResourceStream($"FikaLauncher.Languages.{resourceLanguage}.fika-terms.md") !=
                    null;
                return launcherTerms || fikaTerms;
            case "readme":
                return assembly.GetManifestResourceStream($"FikaLauncher.Languages.{resourceLanguage}.README.md") !=
                       null;
            default:
                return false;
        }
    }

    private static async Task<Dictionary<string, string>?> LoadEmbeddedStrings()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "FikaLauncher.Languages.en-US.strings.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return null;

            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading embedded strings: {ex.Message}");
            return null;
        }
    }

    private static async Task<Dictionary<string, string>?> TryGetGitHubLocale(string language)
    {
        try
        {
            var filePath = $"Languages/{language}/strings.json";
            var endpoint = $"locale/{filePath}";

            var (latestCommitHash, commitDate) = await _repository.GetLatestCommitInfo(filePath)
                .WithRateLimiting(endpoint);

            if (latestCommitHash == null || !commitDate.HasValue) return null;

            var (cachedContent, cacheInfo) = await LocaleCacheService.GetCachedLocale(language, latestCommitHash);
            if (cachedContent != null && cacheInfo != null)
                return JsonSerializer.Deserialize<Dictionary<string, string>>(cachedContent);

            var content = await _repository.DownloadContent(filePath);
            if (content == null) return null;

            await LocaleCacheService.SaveLocaleToCache(content, language, latestCommitHash, commitDate.Value);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting GitHub locale: {ex.Message}");
            return null;
        }
    }
}