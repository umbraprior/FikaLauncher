using Avalonia.Threading;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using FikaLauncher.Localization;
using FikaLauncher.Services;

namespace FikaLauncher.Services.GitHub;

public class GitHubRateLimitService
{
    private static readonly TimeSpan RateLimitBuffer = TimeSpan.FromSeconds(30);
    private static GitHubRateLimitService? _instance;
    private static readonly object _lock = new();

    public static GitHubRateLimitService Instance
    {
        get
        {
            if (_instance == null)
                lock (_lock)
                {
                    _instance ??= new GitHubRateLimitService();
                }

            return _instance;
        }
    }

    private GitHubRateLimitService()
    {
    }

    public void Initialize()
    {
    }

    public DateTime GetResetTime()
    {
        return ApplicationStateService.GetCurrentState().RateLimitResetTime;
    }

    public bool IsRateLimited
    {
        get
        {
            var resetTime = GetResetTime();
            var currentTime = DateTime.UtcNow;
            return resetTime > currentTime;
        }
    }

    public void HandleRateLimit()
    {
        var resetTime = GetResetTime();
        if (resetTime <= DateTime.UtcNow)
        {
            resetTime = DateTime.UtcNow.AddHours(1);
            var state = ApplicationStateService.GetCurrentState();
            state.RateLimitResetTime = resetTime;
            ApplicationStateService.SaveState();
        }

        var localTime = resetTime.ToLocalTime();
        NotificationController.Show(
            Localizer.Get("RateLimit"),
            string.Format(Localizer.Get("GitHubRateLimitUntil"), localTime.ToString("h:mm tt")),
            NotificationType.Error
        );
    }

    public void HandleRateLimit(DateTime resetTimeUtc)
    {
        var utcResetTime = resetTimeUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(resetTimeUtc, DateTimeKind.Utc)
            : resetTimeUtc.ToUniversalTime();

        utcResetTime = utcResetTime.Add(RateLimitBuffer);

        var state = ApplicationStateService.GetCurrentState();
        state.RateLimitResetTime = utcResetTime;
        ApplicationStateService.SaveState();

        var localTime = utcResetTime.ToLocalTime();
        NotificationController.Show(
            Localizer.Get("RateLimit"),
            string.Format(Localizer.Get("GitHubRateLimitUntil"), localTime.ToString("h:mm tt")),
            NotificationType.Error
        );
    }

    public void ClearRateLimit()
    {
        var state = ApplicationStateService.GetCurrentState();
        state.RateLimitResetTime = DateTime.MinValue;
        ApplicationStateService.SaveState();
    }

    public async Task<bool> CanMakeRequest(string endpoint)
    {
        if (IsRateLimited)
        {
            HandleRateLimit();
            return false;
        }

        return true;
    }
}