using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FikaLauncher.Services;

public static class ReadmeCacheService
{
    public class ReadmeInfo
    {
        public string CommitHash { get; set; } = string.Empty;
        public DateTime CommitDate { get; set; }
    }

    private const string CacheFileName = "readme-{0}.md";

    public static string GetCacheFilePath(string language)
    {
        return FileSystemService.GetCacheFilePath(string.Format(CacheFileName, language));
    }

    public static async Task<ReadmeInfo?> ReadCacheInfo(string cacheFilePath)
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

            return JsonSerializer.Deserialize<ReadmeInfo>(json);
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
            Console.WriteLine($"Saving content to cache: {cacheFilePath}");

            if (File.Exists(cacheFilePath))
            {
                var fileInfo = new FileInfo(cacheFilePath);
                if ((fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    fileInfo.Attributes &= ~FileAttributes.ReadOnly;
            }

            await File.WriteAllTextAsync(cacheFilePath, content);
            File.SetAttributes(cacheFilePath, File.GetAttributes(cacheFilePath) | FileAttributes.ReadOnly);

            var infoPath = cacheFilePath + ".info";
            var readmeInfo = new ReadmeInfo
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

            var json = JsonSerializer.Serialize(readmeInfo);
            await File.WriteAllTextAsync(infoPath, json);
            File.SetAttributes(infoPath, File.GetAttributes(infoPath) | FileAttributes.ReadOnly);

            Console.WriteLine($"Successfully saved to cache (commit: {commitHash[..7]})");
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