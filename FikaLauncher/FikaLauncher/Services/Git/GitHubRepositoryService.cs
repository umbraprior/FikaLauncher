using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace FikaLauncher.Services.Doc;

public class GitHubRepositoryService : BaseRepositoryService
{
    public override string BaseApiUrl => "https://api.github.com";
    public override string RawContentUrl => "https://raw.githubusercontent.com";

    public GitHubRepositoryService(string owner, string repo, string branch)
        : base(owner, repo, branch)
    {
    }

    protected override void ConfigureHttpClient()
    {
        base.ConfigureHttpClient();
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FikaLauncher", "1.0"));
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    public override async Task<(string? commitHash, DateTime? commitDate)> GetLatestCommitInfo(string filePath)
    {
        try
        {
            var url = $"repos/{_owner}/{_repo}/commits?path={filePath}&sha={_branch}&per_page=1";
            var response = await _client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (null, null);

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
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
            var response = await _client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public override async Task<List<string>?> GetDirectoryContents(string path)
    {
        try
        {
            var url = $"repos/{_owner}/{_repo}/contents/{path}?ref={_branch}";
            var response = await _client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<List<GitHubContentItem>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return items?.Select(i => i.Name).ToList();
            }

            Console.WriteLine($"Failed to get directory contents with status: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting directory contents: {ex.Message}");
            return null;
        }
    }

    private class GitHubContentItem
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}