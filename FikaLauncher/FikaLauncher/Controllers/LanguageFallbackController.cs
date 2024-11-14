using System;
using System.Threading.Tasks;
using FikaLauncher.Services.GitHub;

namespace FikaLauncher.Services.Lang;

public static class LanguageFallbackController
{
    private const string DefaultLanguage = "en-US";

    public static async Task<T?> GetWithFallback<T>(
        string language,
        Func<string, Task<T?>> getCachedContent,
        Func<string, Task<T?>> getEmbeddedContent,
        string resourceType)
    {
        var cached = await getCachedContent(language);
        if (cached != null)
        {
            Console.WriteLine($"Using cached {resourceType} for {language}");
            return cached;
        }

        var embedded = await getEmbeddedContent(language);
        if (embedded != null)
        {
            Console.WriteLine($"Using embedded {resourceType} for {language}");
            return embedded;
        }

        if (language != DefaultLanguage)
        {
            var englishCached = await getCachedContent(DefaultLanguage);
            if (englishCached != null)
            {
                Console.WriteLine($"Using cached English {resourceType} as fallback");
                return englishCached;
            }
        }

        if (language != DefaultLanguage)
        {
            var englishEmbedded = await getEmbeddedContent(DefaultLanguage);
            if (englishEmbedded != null)
            {
                Console.WriteLine($"Using embedded English {resourceType} as fallback");
                return englishEmbedded;
            }
        }

        Console.WriteLine($"No fallback content available for {resourceType}");
        return default;
    }

    public static bool ShouldUseFallback(Exception? error = null)
    {
        if (error == null) return true;

        if (GitHubRateLimitService.Instance.IsRateLimited)
        {
            Console.WriteLine("Using fallback due to rate limit");
            return true;
        }

        if (error.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Using fallback due to rate limit error");
            return true;
        }

        if (error.Message.Contains("network", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Using fallback due to network error");
            return true;
        }

        return true;
    }
}