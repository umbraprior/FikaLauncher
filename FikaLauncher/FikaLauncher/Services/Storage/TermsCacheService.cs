using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FikaLauncher.Services;

public static class TermsCacheService
{
    public class TermsInfo
    {
        public string CommitHash { get; set; } = string.Empty;
        public DateTime CommitDate { get; set; }
    }

    private const string CacheFileName = "terms-{0}-{1}.md";

    public static string GetCacheFilePath(string language, bool isLauncherTerms)
    {
        var type = isLauncherTerms ? "launcher" : "fika";
        return FileSystemService.GetCacheFilePath(string.Format(CacheFileName, type, language));
    }

    public static async Task<TermsInfo?> ReadCacheInfo(string cacheFilePath)
    {
        try
        {
            if (!File.Exists(cacheFilePath))
                return null;

            var infoPath = cacheFilePath + ".info";
            if (!File.Exists(infoPath))
                return null;

            var json = await File.ReadAllTextAsync(infoPath);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<TermsInfo>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading cache info: {ex.Message}");
            return null;
        }
    }

    public static async Task SaveToCache(string content, string cacheFilePath, string commitHash, DateTime commitDate)
    {
        try
        {
            Console.WriteLine($"Saving terms to cache: {cacheFilePath}");

            if (File.Exists(cacheFilePath))
            {
                var fileInfo = new FileInfo(cacheFilePath);
                if ((fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    fileInfo.Attributes &= ~FileAttributes.ReadOnly;
            }

            await File.WriteAllTextAsync(cacheFilePath, content);
            File.SetAttributes(cacheFilePath, File.GetAttributes(cacheFilePath) | FileAttributes.ReadOnly);

            var infoPath = cacheFilePath + ".info";
            var termsInfo = new TermsInfo
            {
                CommitHash = commitHash,
                CommitDate = commitDate
            };

            if (File.Exists(infoPath))
            {
                var infoFile = new FileInfo(infoPath);
                if ((infoFile.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    infoFile.Attributes &= ~FileAttributes.ReadOnly;
            }

            var json = JsonSerializer.Serialize(termsInfo);
            await File.WriteAllTextAsync(infoPath, json);
            File.SetAttributes(infoPath, File.GetAttributes(infoPath) | FileAttributes.ReadOnly);

            Console.WriteLine($"Successfully saved terms to cache (commit: {commitHash[..7]})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving to cache: {ex.Message}");
        }
    }

    public static async Task<string?> ReadFromCache(string cacheFilePath)
    {
        try
        {
            if (!File.Exists(cacheFilePath))
                return null;

            return await File.ReadAllTextAsync(cacheFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading from cache: {ex.Message}");
            return null;
        }
    }
} 