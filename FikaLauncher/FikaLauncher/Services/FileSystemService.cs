using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace FikaLauncher.Services;

public static class FileSystemService
{
    private static readonly string AppName = "FikaLauncher";
    
    public static string AppDataDirectory { get; private set; } = string.Empty;
    public static string TempDirectory { get; private set; } = string.Empty;
    public static string LogsDirectory { get; private set; } = string.Empty;
    public static string SettingsDirectory { get; private set; } = string.Empty;
    public static string CacheDirectory { get; private set; } = string.Empty;

    static FileSystemService()
    {
        try
        {
            Console.WriteLine("Initializing FileSystemService...");
            
            AppDataDirectory = GetPlatformSpecificAppDataPath();

            TempDirectory = Path.Combine(AppDataDirectory, "temp");
            LogsDirectory = Path.Combine(AppDataDirectory, "logs");
            SettingsDirectory = Path.Combine(AppDataDirectory, "settings");
            CacheDirectory = Path.Combine(AppDataDirectory, "cache");

            EnsureDirectoriesExist();
            
            Console.WriteLine($"FileSystemService initialized successfully.");
            Console.WriteLine($"AppData Directory: {AppDataDirectory}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing FileSystemService: {ex.Message}");
            throw;
        }
    }

    private static string GetPlatformSpecificAppDataPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine("Detected OS: Windows");
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppName
            );
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Console.WriteLine("Detected OS: Linux");
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, ".local", "share", AppName);
        }
        else
        {
            throw new PlatformNotSupportedException("Current platform is not supported.");
        }
    }

    private static void EnsureDirectoriesExist()
    {
        var directories = new[]
        {
            AppDataDirectory,
            TempDirectory,
            LogsDirectory,
            SettingsDirectory,
            CacheDirectory
        };

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }
        }
    }

    public static void CleanTempDirectory()
    {
        try
        {
            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, true);
                Directory.CreateDirectory(TempDirectory);
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to clean temp directory: {ex.Message}", ex);
        }
    }

    public static void CleanCacheDirectory()
    {
        try
        {
            if (Directory.Exists(CacheDirectory))
            {
                foreach (var file in Directory.GetFiles(CacheDirectory))
                {
                    try
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        
                        File.SetAttributes(file, FileAttributes.Normal);
                        

                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
                                File.Delete(file);
                                break;
                            }
                            catch (IOException) when (i < 2)
                            {
                                Thread.Sleep(100);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete file {file}: {ex.Message}");
                    }
                }

                foreach (var dir in Directory.GetDirectories(CacheDirectory))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete directory {dir}: {ex.Message}");
                    }
                }
            }

            Directory.CreateDirectory(CacheDirectory);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to clean cache directory: {ex.Message}", ex);
        }
    }

    public static string GetLogFilePath(string logName)
    {
        return Path.Combine(LogsDirectory, $"{logName}.log");
    }

    public static string GetSettingsFilePath(string settingsName)
    {
        return Path.Combine(SettingsDirectory, $"{settingsName}.json");
    }

    public static string GetTempFilePath(string fileName)
    {
        return Path.Combine(TempDirectory, fileName);
    }

    public static string GetCacheFilePath(string fileName)
    {
        return Path.Combine(CacheDirectory, fileName);
    }
}
