using System;
using System.Threading.Tasks;

namespace FikaLauncher.Services.Doc;

public interface IRepositoryService
{
    string BaseApiUrl { get; }
    string RawContentUrl { get; }
    Task<(string? commitHash, DateTime? commitDate)> GetLatestCommitInfo(string filePath);
    Task<bool> DoesFileExist(string filePath);
    Task<string?> DownloadContent(string filePath);
}
