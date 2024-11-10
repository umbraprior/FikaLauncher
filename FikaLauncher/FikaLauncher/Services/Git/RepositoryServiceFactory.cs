using FikaLauncher.Services.Doc;

namespace FikaLauncher.Services.Doc;

public static class RepositoryServiceFactory
{
    public static IRepositoryService Create(string url, RepositoryInfo repoInfo)
    {
        if (url.Contains("github.com"))
            return new GitHubRepositoryService(repoInfo.Owner, repoInfo.Repository, repoInfo.Branch);
        else
            // Assume Gitea for other URLs
            return new GiteaRepositoryService(url, repoInfo.Owner, repoInfo.Repository, repoInfo.Branch);
    }
}