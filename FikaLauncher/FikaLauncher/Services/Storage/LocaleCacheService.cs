using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FikaLauncher.Services;

public static class LocaleCacheService
{
    public class LocaleInfo
    {
        public string CommitHash { get; set; } = string.Empty;
        public DateTime CommitDate { get; set; }
    }

    private const string CacheFileName = "locale-{0}-{1}.json";

    public static string GetCacheFilePath(string language, string commitHash)
    {
        return FileSystemService.GetCacheFilePath(string.Format(CacheFileName, language, commitHash[..7]));
    }

    public static async Task<(string? content, LocaleInfo? info)> GetCachedLocale(string language, string commitHash)
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

    public static async Task SaveLocaleToCache(string content, string language, string commitHash, DateTime commitDate)
    {
        var path = GetCacheFilePath(language, commitHash);
        var info = new LocaleInfo
        {
            CommitHash = commitHash,
            CommitDate = commitDate
        };

        await SaveToCache(content, path, info);
        await CleanupOldLocaleFiles(language, commitHash);
    }

    private static async Task SaveToCache(string content, string path, LocaleInfo info)
    {
        try
        {
            Console.WriteLine($"Saving locale to cache: {path}");

            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                if ((fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    fileInfo.Attributes &= ~FileAttributes.ReadOnly;
            }

            await File.WriteAllTextAsync(path, content);
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);

            var infoPath = path + ".info";
            if (File.Exists(infoPath))
            {
                var infoFile = new FileInfo(infoPath);
                if ((infoFile.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    infoFile.Attributes &= ~FileAttributes.ReadOnly;
            }

            var json = JsonSerializer.Serialize(info);
            await File.WriteAllTextAsync(infoPath, json);
            File.SetAttributes(infoPath, File.GetAttributes(infoPath) | FileAttributes.ReadOnly);

            Console.WriteLine($"Successfully saved locale to cache (commit: {info.CommitHash[..7]})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving to cache: {ex.Message}");
        }
    }

    public static async Task<LocaleInfo?> ReadCacheInfo(string cacheFilePath)
    {
        try
        {
            var infoPath = cacheFilePath + ".info";
            if (!File.Exists(infoPath))
                return null;

            var json = await File.ReadAllTextAsync(infoPath);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<LocaleInfo>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading cache info: {ex.Message}");
            return null;
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

    public static async Task CleanupOldLocaleFiles(string language, string currentCommitHash)
    {
        try
        {
            var cacheDir = FileSystemService.CacheDirectory;
            var localePrefix = $"locale-{language}-";
            var currentCacheFile = GetCacheFilePath(language, currentCommitHash);

            foreach (var file in Directory.GetFiles(cacheDir, $"{localePrefix}*.json"))
                if (file != currentCacheFile)
                    try
                    {
                        // Remove read-only attribute if present
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);

                        // Also delete the corresponding .info file
                        var infoFile = file + ".info";
                        if (File.Exists(infoFile))
                        {
                            File.SetAttributes(infoFile, FileAttributes.Normal);
                            File.Delete(infoFile);
                        }

                        Console.WriteLine($"Cleaned up old locale file: {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error cleaning up old locale file {file}: {ex.Message}");
                    }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during locale cleanup: {ex.Message}");
        }
    }
}