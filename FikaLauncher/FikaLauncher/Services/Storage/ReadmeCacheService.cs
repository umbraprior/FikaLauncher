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

    private const string CacheFileName = "readme-{0}-{1}.md";

    public static string GetCacheFilePath(string language, string commitHash)
    {
        return FileSystemService.GetCacheFilePath(string.Format(CacheFileName, language, commitHash[..7]));
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

    public static async Task SaveToCache(string content, string language, string commitHash, DateTime commitDate)
    {
        var path = GetCacheFilePath(language, commitHash);
        var info = new ReadmeInfo
        {
            CommitHash = commitHash,
            CommitDate = commitDate
        };

        await SaveToCache(content, path, info);
        await CleanupOldReadmeFiles(language, commitHash);
    }

    private static async Task CleanupOldReadmeFiles(string language, string currentCommitHash)
    {
        try
        {
            var cacheDir = FileSystemService.CacheDirectory;
            var readmePrefix = $"readme-{language}-";
            var currentCacheFile = GetCacheFilePath(language, currentCommitHash);

            foreach (var file in Directory.GetFiles(cacheDir, $"{readmePrefix}*.md"))
                if (file != currentCacheFile)
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);

                        var infoFile = file + ".info";
                        if (File.Exists(infoFile))
                        {
                            File.SetAttributes(infoFile, FileAttributes.Normal);
                            File.Delete(infoFile);
                        }

                        Console.WriteLine($"Cleaned up old readme file: {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error cleaning up old readme file {file}: {ex.Message}");
                    }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during readme cleanup: {ex.Message}");
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

    public static async Task<(string? content, ReadmeInfo? info)> GetCachedReadme(string language, string commitHash)
    {
        var path = GetCacheFilePath(language, commitHash);

        var content = await ReadFromCache(path);
        if (content != null)
        {
            var info = await ReadCacheInfo(path);
            if (info != null && info.CommitHash == commitHash) return (content, info);
        }

        return (null, null);
    }

    private static async Task SaveToCache(string content, string path, ReadmeInfo info)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (directory != null)
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(path, content);
            await File.WriteAllTextAsync(path + ".info", JsonSerializer.Serialize(info));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving to cache: {ex.Message}");
        }
    }
}