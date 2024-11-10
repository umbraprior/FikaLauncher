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

    private const string CacheFileName = "terms-{0}-{1}-{2}.md";

    public static string GetCacheFilePath(string language, string commitHash, bool isLauncherTerms)
    {
        var type = isLauncherTerms ? "launcher" : "fika";
        return FileSystemService.GetCacheFilePath(string.Format(CacheFileName, type, language, commitHash[..7]));
    }

    public static async Task<(string? content, TermsInfo? info)> GetCachedTerms(string language, string commitHash, bool isLauncherTerms)
    {
        var path = GetCacheFilePath(language, commitHash, isLauncherTerms);
        
        var content = await ReadFromCache(path);
        if (content != null)
        {
            var info = await ReadCacheInfo(path);
            if (info != null && info.CommitHash == commitHash)
            {
                return (content, info);
            }
        }

        return (null, null);
    }

    public static async Task SaveToCache(string content, string language, string commitHash, DateTime commitDate, bool isLauncherTerms)
    {
        var path = GetCacheFilePath(language, commitHash, isLauncherTerms);
        var info = new TermsInfo
        {
            CommitHash = commitHash,
            CommitDate = commitDate
        };

        await SaveToCache(content, path, info);
        await CleanupOldTermsFiles(language, commitHash, isLauncherTerms);
    }

    private static async Task CleanupOldTermsFiles(string language, string currentCommitHash, bool isLauncherTerms)
    {
        try
        {
            var cacheDir = FileSystemService.CacheDirectory;
            var type = isLauncherTerms ? "launcher" : "fika";
            var termsPrefix = $"terms-{type}-{language}-";
            var currentCacheFile = GetCacheFilePath(language, currentCommitHash, isLauncherTerms);

            foreach (var file in Directory.GetFiles(cacheDir, $"{termsPrefix}*.md"))
            {
                if (file != currentCacheFile)
                {
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

                        Console.WriteLine($"Cleaned up old terms file: {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error cleaning up old terms file {file}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during terms cleanup: {ex.Message}");
        }
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

    public static async Task SaveToCache(string content, string cacheFilePath, TermsInfo info)
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

            if (File.Exists(infoPath))
            {
                var infoFile = new FileInfo(infoPath);
                if ((infoFile.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    infoFile.Attributes &= ~FileAttributes.ReadOnly;
            }

            var json = JsonSerializer.Serialize(info);
            await File.WriteAllTextAsync(infoPath, json);
            File.SetAttributes(infoPath, File.GetAttributes(infoPath) | FileAttributes.ReadOnly);

            Console.WriteLine($"Successfully saved terms to cache (commit: {info.CommitHash[..7]})");
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