using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FikaLauncher.Services.Doc;

public abstract class BaseRepositoryService : IRepositoryService
{
    protected readonly string _owner;
    protected readonly string _repo;
    protected readonly string _branch;
    protected readonly HttpClient _client;

    public abstract string BaseApiUrl { get; }
    public abstract string RawContentUrl { get; }

    protected BaseRepositoryService(string owner, string repo, string branch)
    {
        _owner = owner;
        _repo = repo;
        _branch = branch;
        _client = new HttpClient();
        ConfigureHttpClient();
    }

    protected virtual void ConfigureHttpClient()
    {
        if (!string.IsNullOrEmpty(BaseApiUrl))
        {
            _client.BaseAddress = new Uri(BaseApiUrl.TrimEnd('/') + "/");
        }
    }

    public virtual string GetRawContentUrl(string path)
    {
        return $"{RawContentUrl.TrimEnd('/')}/{_owner}/{_repo}/{_branch}/{path.TrimStart('/')}";
    }

    public abstract Task<(string? commitHash, DateTime? commitDate)> GetLatestCommitInfo(string filePath);

    public abstract Task<bool> DoesFileExist(string filePath);

    public virtual async Task<string?> DownloadContent(string filePath)
    {
        try
        {
            using var rawClient = new HttpClient
            {
                BaseAddress = new Uri(RawContentUrl)
            };

            if (_client.DefaultRequestHeaders.Authorization != null)
                rawClient.DefaultRequestHeaders.Authorization = _client.DefaultRequestHeaders.Authorization;

            var fullPath = $"{_owner}/{_repo}/{_branch}/{filePath}";
            Console.WriteLine($"Downloading from: {RawContentUrl}{fullPath}");

            var response = await rawClient.GetAsync(fullPath);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Successfully downloaded content (length: {content.Length})");
                return content;
            }

            Console.WriteLine($"Download failed with status: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading content: {ex.Message}");
            return null;
        }
    }

    public abstract Task<List<string>?> GetDirectoryContents(string path);
}