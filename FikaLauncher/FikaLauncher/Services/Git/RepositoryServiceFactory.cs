using System;
using FikaLauncher.Services.Doc;

namespace FikaLauncher.Services.Doc;

public static class RepositoryServiceFactory
{
    public static IRepositoryService Create(string url, RepositoryInfo repoInfo)
    {
        if (url.EndsWith("github.com", StringComparison.OrdinalIgnoreCase))
            return new GitHubRepositoryService(repoInfo.Owner, repoInfo.Repository, repoInfo.Branch);
        else
            return new GiteaRepositoryService(url, repoInfo.Owner, repoInfo.Repository, repoInfo.Branch);
    }
}