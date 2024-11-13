using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using FikaLauncher.Services.Doc;
using System.Collections.Generic;
using System.Reflection;

namespace FikaLauncher.Services;

public static class RepositoryLocaleService
{
    private static readonly IRepositoryService _repository;
    private const string BaseUrl = "https://raw.githubusercontent.com";
    private const int CommitGracePeriodMinutes = 10;
    
    static RepositoryLocaleService()
    {
        var repoInfo = RepositoryConfiguration.GetRepository("FikaLauncherTranslations");
        _repository = RepositoryServiceFactory.Create(BaseUrl, repoInfo);
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
            if (language == "en-US")
            {
                var embeddedStrings = await LoadEmbeddedStrings();
                if (embeddedStrings != null)
                {
                    return (embeddedStrings, new LocaleCacheService.LocaleInfo 
                    { 
                        CommitHash = "embedded",
                        CommitDate = DateTime.UtcNow
                    });
                }
            }

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
}