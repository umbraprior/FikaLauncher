using System;
using System.Threading.Tasks;

namespace FikaLauncher.Services.GitHub;

public static class GitHubRequestExtensions
{
    public static async Task<T> WithRateLimiting<T>(this Task<T> request, string endpoint)
    {
        var service = GitHubRateLimitService.Instance;

        if (!await service.CanMakeRequest(endpoint)) throw new GitHubRateLimitException("Rate limit reached");

        try
        {
            return await request;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            {
                service.HandleRateLimit();
                throw new GitHubRateLimitException("Rate limit reached", ex);
            }

            throw;
        }
    }
}