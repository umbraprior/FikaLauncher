using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FikaLauncher.Services.Doc;

public class GitHubRepositoryService : BaseRepositoryService
{
    public override string BaseApiUrl => "https://api.github.com/";
    public override string RawContentUrl => "https://raw.githubusercontent.com/";

    public GitHubRepositoryService(string owner, string repo, string branch) 
        : base(owner, repo, branch)
    {
    }

    protected override void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FikaLauncher", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    public override async Task<(string? commitHash, DateTime? commitDate)> GetLatestCommitInfo(string filePath)
    {
        try
        {
            var url = $"repos/{_owner}/{_repo}/commits?path={filePath}&sha={_branch}&per_page=1";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (null, null);

            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.GetArrayLength() == 0)
                return (null, null);

            var commit = root[0];
            var commitHash = commit.GetProperty("sha").GetString();
            var dateStr = commit.GetProperty("commit")
                .GetProperty("committer")
                .GetProperty("date")
                .GetString();

            return (commitHash, DateTime.Parse(dateStr));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting commit info: {ex.Message}");
            return (null, null);
        }
    }

    public override async Task<bool> DoesFileExist(string filePath)
    {
        var url = $"repos/{_owner}/{_repo}/contents/{filePath}?ref={_branch}";
        try
        {
            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
