using System;
using System.Collections.Generic;

public class RepositoryInfo
{
    public string Owner { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
}

public static class RepositoryConfiguration
{
    private static readonly Dictionary<string, RepositoryInfo> Repositories = new()
    {
        ["FikaLauncher"] = new()
        {
            Owner = "umbraprior",
            Repository = "FikaLauncher",
            Branch = "main"
        },
        ["FikaDocumentation"] = new()
        {
            Owner = "project-fika",
            Repository = "Fika-Documentation",
            Branch = "main"
        },
        ["FikaServer"] = new()
        {
            Owner = "project-fika",
            Repository = "Fika-Server",
            Branch = "main"
        },
        ["FikaPlugin"] = new()
        {
            Owner = "project-fika",
            Repository = "Fika-Plugin",
            Branch = "main"
        }
    };

    public static RepositoryInfo GetRepository(string name)
    {
        if (!Repositories.TryGetValue(name, out var repo))
        {
            throw new ArgumentException($"Repository '{name}' not found in configuration", nameof(name));
        }
        return repo;
    }
}
