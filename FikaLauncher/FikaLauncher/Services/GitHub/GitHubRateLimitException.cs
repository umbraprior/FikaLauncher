using System;

namespace FikaLauncher.Services.GitHub;

public class GitHubRateLimitException : Exception
{
    public GitHubRateLimitException(string message) : base(message)
    {
    }

    public GitHubRateLimitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}