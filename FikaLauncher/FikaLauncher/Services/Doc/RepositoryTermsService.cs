using Markdig;
using Markdig.Parsers;
using System;
using System.Threading.Tasks;
using FikaLauncher.Services.Doc;
using System.IO;

namespace FikaLauncher.Services;

public static class RepositoryTermsService
{
    private static readonly IRepositoryService _repository;

    static RepositoryTermsService()
    {
        var repoInfo = RepositoryConfiguration.GetRepository("FikaLauncher");
        _repository = RepositoryServiceFactory.Create("https://github.com", repoInfo);
    }

    private static string GetTermsPath(string language, bool isLauncherTerms)
    {
        var folder = isLauncherTerms ? "Launcher" : "Fika";
        return $"Languages/Terms/{folder}/{language}.md";
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

    private static async Task<bool> ShouldUpdateCache(string language, bool isLauncherTerms)
    {
        try
        {
            var filePath = GetTermsPath(language, isLauncherTerms);
            var (latestCommitHash, _) = await GetLatestCommitInfo(filePath);
            if (latestCommitHash == null)
                return false;

            var cacheFilePath = TermsCacheService.GetCacheFilePath(language, isLauncherTerms);
            if (!File.Exists(cacheFilePath))
                return true;

            var cachedInfo = await TermsCacheService.ReadCacheInfo(cacheFilePath);
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

    private static async Task<string?> DownloadTermsContent(string language, bool isLauncherTerms)
    {
        var filePath = GetTermsPath(language, isLauncherTerms);
        Console.WriteLine($"[Terms] Attempting to load terms from: {filePath}");

        try
        {
            var (commitHash, commitDate) = await GetLatestCommitInfo(filePath);
            if (commitHash == null || !commitDate.HasValue)
                return null;

            var content = await _repository.DownloadContent(filePath);
            if (content != null)
            {
                Console.WriteLine($"Successfully downloaded terms (length: {content.Length}, commit: {commitHash[..7]})");
                await TermsCacheService.SaveToCache(content, TermsCacheService.GetCacheFilePath(language, isLauncherTerms),
                    commitHash, commitDate.Value);
                return content;
            }

            Console.WriteLine("[Terms] Download failed");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading terms: {ex.Message}");
            return null;
        }
    }

    public static async Task<string> GetProcessedTerms(bool isDark, bool isLauncherTerms = true)
    {
        try
        {
            var currentLanguage = LocalizationService.Instance.CurrentLanguage;
            var cacheFilePath = TermsCacheService.GetCacheFilePath(currentLanguage, isLauncherTerms);

            Console.WriteLine($"[Terms] Getting terms for language: {currentLanguage}");

            string? content;
            if (File.Exists(cacheFilePath) && !await ShouldUpdateCache(currentLanguage, isLauncherTerms))
            {
                Console.WriteLine("[Terms] Using existing cache");
                content = await TermsCacheService.ReadFromCache(cacheFilePath);
                if (content != null)
                {
                    Console.WriteLine($"[Terms] Successfully loaded cached terms for {currentLanguage}");
                    return await ProcessTermsContent(content, isDark);
                }
            }

            content = await DownloadTermsContent(currentLanguage, isLauncherTerms);
            if (content == null)
            {
                Console.WriteLine($"[Terms] No localized version found for {currentLanguage}");
                content = await GetEnglishTerms(isLauncherTerms);
            }

            return await ProcessTermsContent(content, isDark);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load terms: {ex.Message}");
            return "Error loading terms of use.";
        }
    }

    private static async Task<string> GetEnglishTerms(bool isLauncherTerms)
    {
        var englishCachePath = TermsCacheService.GetCacheFilePath("en-US", isLauncherTerms);
        
        if (File.Exists(englishCachePath) && !await ShouldUpdateCache("en-US", isLauncherTerms))
        {
            var content = await TermsCacheService.ReadFromCache(englishCachePath);
            if (content != null)
                return content;
        }

        var downloadedContent = await DownloadTermsContent("en-US", isLauncherTerms);
        return downloadedContent ?? "Error loading terms of use.";
    }

    private static Task<string> ProcessTermsContent(string content, bool isDark)
    {
        var (backgroundColor, textColor, headingColor, linkColor) = isDark
            ? ("#242424", "#d4d4d4", "#ffffff", "#569cd6")
            : ("#ebeef0", "#24292f", "#000000", "#0366d6");

        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var html = Markdig.Markdown.ToHtml(content, pipeline);
        html = ProcessNoteBlocks(html, isDark, isDark ? "#1C1C1C" : "#F6F8FA");

        return Task.FromResult($@"<!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{
                        background-color: {backgroundColor};
                        color: {textColor};
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif;
                        font-size: 16px;
                        line-height: 1.5;
                        padding: 0 16px 40px 16px;
                        margin: 0;
                    }}

                    h1, h2, h3, h4, h5, h6 {{
                        color: {headingColor};
                        margin-top: 24px;
                        margin-bottom: 16px;
                        font-weight: 600;
                        line-height: 1.25;
                    }}

                    a {{
                        color: {linkColor};
                        text-decoration: none;
                    }}

                    a:hover {{
                        text-decoration: underline;
                    }}

                    .note-block {{
                        margin: 16px 0;
                        padding: 10px 16px;
                        border-left: 4px solid;
                        border-radius: 6px;
                    }}

                    .note-block-title {{
                        font-weight: 600;
                        margin-bottom: 8px;
                        display: block;
                    }}

                    pre {{
                        margin: 16px 0 !important;
                        border-radius: 8px !important;
                        background-color: {(isDark ? "#2D2D2D" : "#F0F0F0")} !important;
                        padding: 12px !important;
                        overflow-x: auto !important;
                        border: 1px solid {(isDark ? "#404040" : "#E0E0E0")} !important;
                        display: block !important;
                    }}

                    pre > code {{
                        padding: 0 !important;
                        margin: 0 !important;
                        display: block !important;
                        white-space: pre !important;
                        font-family: 'Consolas', 'Monaco', 'Courier New', monospace !important;
                        line-height: 1.4 !important;
                        min-height: 0 !important;
                    }}
                </style>
            </head>
            <body>
                {html}
            </body>
            </html>");
    }

    private static string ProcessNoteBlocks(string html, bool isDark, string bgColor)
    {
        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<div class=""markdown-alert markdown-alert-note"">\s*<p class=""markdown-alert-title""[^>]*>.*?Note</p>\s*<p>(.*?)</p>\s*</div>",
            match =>
            {
                var content = match.Groups[1].Value;
                var noteColor = "#58a6ff";
                return
                    $@"<div class=""note-block note"" style=""background-color: {bgColor}; border-left-color: {noteColor} !important;"">
                           <span class=""note-block-title"" style=""color: {noteColor} !important;"">ℹ️ Note</span>
                           <div class=""note-block-content"" style=""font-weight: 600"">{content}</div></div>";
            },
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<div class=""markdown-alert markdown-alert-tip"">\s*<p class=""markdown-alert-title""[^>]*>.*?Tip</p>\s*<p>(.*?)</p>\s*</div>",
            match =>
            {
                var content = match.Groups[1].Value;
                var tipColor = "#3fb950";
                return
                    $@"<div class=""note-block tip"" style=""background-color: {bgColor}; border-left-color: {tipColor} !important;"">
                           <span class=""note-block-title"" style=""color: {tipColor} !important;"">✅ Tip</span>
                           <div class=""note-block-content"" style=""font-weight: 600"">{content}</div></div>";
            },
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<div class=""markdown-alert markdown-alert-important"">\s*<p class=""markdown-alert-title""[^>]*>.*?Important</p>\s*<p>(.*?)</p>\s*</div>",
            match =>
            {
                var content = match.Groups[1].Value;
                var importantColor = "#8957e5";
                return
                    $@"<div class=""note-block important"" style=""background-color: {bgColor}; border-left-color: {importantColor} !important;"">
                           <span class=""note-block-title"" style=""color: {importantColor} !important;"">☑️ Important</span>
                           <div class=""note-block-content"" style=""font-weight: 600"">{content}</div></div>";
            },
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<div class=""markdown-alert markdown-alert-warning"">\s*<p class=""markdown-alert-title""[^>]*>.*?Warning</p>\s*<p>(.*?)</p>\s*</div>",
            match =>
            {
                var content = match.Groups[1].Value;
                var warningColor = "#d29922";
                return
                    $@"<div class=""note-block warning"" style=""background-color: {bgColor}; border-left-color: {warningColor} !important;"">
                           <span class=""note-block-title"" style=""color: {warningColor} !important;"">⚠️ Warning</span>
                           <div class=""note-block-content"" style=""font-weight: 600"">{content}</div></div>";
            },
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<div class=""markdown-alert markdown-alert-caution"">\s*<p class=""markdown-alert-title""[^>]*>.*?Caution</p>\s*<p>(.*?)</p>\s*</div>",
            match =>
            {
                var content = match.Groups[1].Value;
                var cautionColor = "#f85149";
                return
                    $@"<div class=""note-block caution"" style=""background-color: {bgColor}; border-left-color: {cautionColor} !important;"">
                           <span class=""note-block-title"" style=""color: {cautionColor} !important;"">⛔ Caution</span>
                           <div class=""note-block-content"" style=""font-weight: 600"">{content}</div></div>";
            },
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        return html;
    }

    public static async Task PreCacheTermsAsync(string language, bool isLauncherTerms)
    {
        try
        {
            var cacheFilePath = TermsCacheService.GetCacheFilePath(language, isLauncherTerms);

            if (language != "en-US" && !await DoesLanguageTermsExist(language, isLauncherTerms))
            {
                Console.WriteLine($"No terms exist for {language}, using English");
                language = "en-US";
                cacheFilePath = TermsCacheService.GetCacheFilePath("en-US", isLauncherTerms);
            }

            if (await ShouldUpdateCache(language, isLauncherTerms))
            {
                Console.WriteLine($"Cache needs updating for {language}, downloading new content");
                await DownloadTermsContent(language, isLauncherTerms);
            }
            else
            {
                var cachedInfo = await TermsCacheService.ReadCacheInfo(cacheFilePath);
                Console.WriteLine($"Cache is up to date for {language} (commit: {cachedInfo?.CommitHash[..7]})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PreCacheTermsAsync: {ex.Message}");
        }
    }

    private static async Task<bool> DoesLanguageTermsExist(string language, bool isLauncherTerms)
    {
        var filePath = GetTermsPath(language, isLauncherTerms);
        try
        {
            return await _repository.DoesFileExist(filePath);
        }
        catch
        {
            return false;
        }
    }
}