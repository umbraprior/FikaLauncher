using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FikaLauncher.Services.Doc;

public class GiteaRepositoryService : BaseRepositoryService
{
    private readonly string _instanceUrl;

    public override string BaseApiUrl => $"{_instanceUrl}/api/v1/";
    public override string RawContentUrl => $"{_instanceUrl}/raw/";

    public GiteaRepositoryService(string instanceUrl, string owner, string repo, string branch) 
        : base(owner, repo, branch)
    {
        _instanceUrl = instanceUrl.TrimEnd('/');
    }

    protected override void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FikaLauncher", "1.0"));
    }

    public override async Task<(string? commitHash, DateTime? commitDate)> GetLatestCommitInfo(string filePath)
    {
        try
        {
            var url = $"repos/{_owner}/{_repo}/commits?path={filePath}&sha={_branch}&limit=1";
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
