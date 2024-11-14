using Markdig;
using Markdig.Parsers;
using System;
using System.Threading.Tasks;
using FikaLauncher.Services.Doc;
using System.IO;
using FikaLauncher.Services.GitHub;
using FikaLauncher.Services.Lang;

namespace FikaLauncher.Services;

public static class RepositoryTermsService
{
    private static readonly IRepositoryService _repository;
    private const int CommitGracePeriodMinutes = 10;

    static RepositoryTermsService()
    {
        var repoInfo = RepositoryConfiguration.GetRepository("FikaLauncherTranslations");
        _repository = RepositoryServiceFactory.Create("https://api.github.com", repoInfo);
    }

    private static string GetTermsPath(string language, bool isLauncherTerms)
    {
        var fileName = isLauncherTerms ? "launcher-terms.md" : "fika-terms.md";
        return $"Languages/{language}/{fileName}";
    }

    private static async Task<(string? commitHash, DateTime? commitDate)> GetLatestCommitInfo(string filePath)
    {
        var endpoint = $"terms/{filePath}";

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

    private static async Task<bool> ShouldUpdateCache(string language, bool isLauncherTerms)
    {
        try
        {
            var filePath = GetTermsPath(language, isLauncherTerms);
            var (latestCommitHash, _) = await GetLatestCommitInfo(filePath);
            if (latestCommitHash == null)
                return false;

            var (_, cacheInfo) = await TermsCacheService.GetCachedTerms(language, latestCommitHash, isLauncherTerms);
            return cacheInfo == null;
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

            if (DateTime.UtcNow - commitDate.Value < TimeSpan.FromMinutes(CommitGracePeriodMinutes))
            {
                Console.WriteLine(
                    $"Skipping download for {language} - commit is too recent ({commitDate.Value:HH:mm:ss UTC})");
                return null;
            }

            var (cachedContent, cacheInfo) =
                await TermsCacheService.GetCachedTerms(language, commitHash, isLauncherTerms);
            if (cachedContent != null && cacheInfo != null) return cachedContent;

            var content = await _repository.DownloadContent(filePath);
            if (content != null)
            {
                Console.WriteLine(
                    $"Successfully downloaded terms (length: {content.Length}, commit: {commitHash[..7]})");
                await TermsCacheService.SaveToCache(content, language, commitHash, commitDate.Value, isLauncherTerms);
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

    public static async Task<string> GetTermsAsync(string language, bool isLauncherTerms, bool isDark = false)
    {
        try
        {
            if (!GitHubRateLimitService.Instance.IsRateLimited)
            {
                var content = await TryGetGitHubTerms(language, isLauncherTerms);
                if (content != null) return await ProcessTermsContent(content, isDark);
            }

            var fallbackContent = await LanguageFallbackController.GetWithFallback(
                language,
                async (lang) => await GetLatestCachedTerms(lang, isLauncherTerms),
                async (lang) => GetEmbeddedTerms(lang, isLauncherTerms),
                isLauncherTerms ? "launcher-terms" : "fika-terms"
            );

            return fallbackContent != null
                ? await ProcessTermsContent(fallbackContent, isDark)
                : "Error loading terms of use.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting terms content: {ex.Message}");
            if (LanguageFallbackController.ShouldUseFallback(ex))
            {
                var fallbackContent = await LanguageFallbackController.GetWithFallback(
                    language,
                    async (lang) => await GetLatestCachedTerms(lang, isLauncherTerms),
                    async (lang) => GetEmbeddedTerms(lang, isLauncherTerms),
                    isLauncherTerms ? "launcher-terms" : "fika-terms"
                );

                return fallbackContent != null
                    ? await ProcessTermsContent(fallbackContent, isDark)
                    : "Error loading terms of use.";
            }

            return "Error loading terms of use.";
        }
    }

    private static async Task<string?> GetLatestCachedTerms(string language, bool isLauncherTerms)
    {
        try
        {
            var cacheDir = FileSystemService.CacheDirectory;
            var termsPrefix = $"terms-{language}-{(isLauncherTerms ? "launcher" : "fika")}-";
            var files = Directory.GetFiles(cacheDir, $"{termsPrefix}*.md");

            var latestDate = DateTime.MinValue;
            string? latestContent = null;

            foreach (var file in files)
            {
                var info = await TermsCacheService.ReadCacheInfo(file);
                if (info != null && info.CommitDate > latestDate)
                {
                    var content = await TermsCacheService.ReadFromCache(file);
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
            Console.WriteLine($"Error getting latest cached terms: {ex.Message}");
            return null;
        }
    }

    private static async Task<string> TryGetGitHubTerms(string language, bool isLauncherTerms)
    {
        var filePath = GetTermsPath(language, isLauncherTerms);
        var (latestCommitHash, _) = await GetLatestCommitInfo(filePath);
        if (latestCommitHash == null)
            return "Error loading terms of use.";

        var (cachedContent, _) = await TermsCacheService.GetCachedTerms(language, latestCommitHash, isLauncherTerms);
        if (cachedContent != null) return cachedContent;

        var downloadedContent = await DownloadTermsContent(language, isLauncherTerms);
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
            if (language != "en-US" && !await DoesLanguageTermsExist(language, isLauncherTerms))
            {
                Console.WriteLine($"No terms exist for {language}, using English");
                language = "en-US";
            }

            var filePath = GetTermsPath(language, isLauncherTerms);
            var (latestCommitHash, _) = await GetLatestCommitInfo(filePath);
            if (latestCommitHash == null)
                return;

            var (_, cacheInfo) = await TermsCacheService.GetCachedTerms(language, latestCommitHash, isLauncherTerms);
            if (cacheInfo == null)
            {
                Console.WriteLine($"Cache needs updating for {language}, downloading new content");
                await DownloadTermsContent(language, isLauncherTerms);
            }
            else
            {
                Console.WriteLine($"Cache is up to date for {language} (commit: {cacheInfo.CommitHash[..7]})");
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

    public static async Task<string> GetProcessedTerms(bool isDark, bool isLauncherTerms, bool skipCacheCheck = false)
    {
        try
        {
            var currentLanguage = LocalizationService.Instance.CurrentLanguage;

            if (!skipCacheCheck)
            {
                var filePath = GetTermsPath(currentLanguage, isLauncherTerms);
                var (latestCommitHash, commitDate) = await GetLatestCommitInfo(filePath);
                if (latestCommitHash != null)
                {
                    var (cachedContent, cacheInfo) =
                        await TermsCacheService.GetCachedTerms(currentLanguage, latestCommitHash, isLauncherTerms);
                    if (cachedContent != null) return await ProcessTermsContent(cachedContent, isDark);

                    var downloadedContent = await DownloadTermsContent(currentLanguage, isLauncherTerms);
                    if (downloadedContent != null) return await ProcessTermsContent(downloadedContent, isDark);
                }
            }
            else
            {
                var embeddedContent = GetEmbeddedTerms(currentLanguage, isLauncherTerms);
                if (embeddedContent != null) return await ProcessTermsContent(embeddedContent, isDark);
            }

            var fallbackContent = GetEmbeddedTerms("en-US", isLauncherTerms);
            return fallbackContent != null
                ? await ProcessTermsContent(fallbackContent, isDark)
                : "Error loading terms of use.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get processed terms: {ex.Message}");
            return "Error loading terms of use.";
        }
    }

    private static string? GetEmbeddedTerms(string language, bool isLauncherTerms)
    {
        try
        {
            var assembly = typeof(RepositoryTermsService).Assembly;
            var fileName = isLauncherTerms ? "launcher-terms.md" : "fika-terms.md";

            var resourceLanguage = language.Replace("-", "_");
            var resourcePath = $"FikaLauncher.Languages.{resourceLanguage}.{fileName}";

            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                Console.WriteLine($"No embedded terms found for {language}");
                return null;
            }

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            Console.WriteLine($"Successfully loaded embedded terms for {language}");
            return content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading embedded terms: {ex.Message}");
            return null;
        }
    }
}