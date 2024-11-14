using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace FikaLauncher.Localization;

public class JsonLocalizer(string languageJsonDirectory = "") : BaseLocalizer
{
    private readonly string _languageJsonDirectory =
        languageJsonDirectory != ""
            ? languageJsonDirectory
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");

    private Dictionary<string, string>? _languageStrings;

    public override void Reload()
    {
        _languageStrings = null;
        _languages.Clear();

        try
        {
            LoadFromFileSystem();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load from file system: {ex.Message}");
            LoadFromEmbeddedResources();
        }
    }

    private void LoadFromFileSystem()
    {
        if (!Directory.Exists(_languageJsonDirectory))
            throw new FileNotFoundException(_languageJsonDirectory);

        foreach (var dir in Directory.GetDirectories(_languageJsonDirectory))
        {
            var localeName = Path.GetFileName(dir);
            var stringsPath = Path.Combine(dir, "strings.json");
            if (File.Exists(stringsPath)) _languages.Add(localeName);
        }

        if (!_languages.Contains(_language))
            _language = DefaultLanguage;

        var languageFile = Path.Combine(_languageJsonDirectory, _language, "strings.json");
        if (!File.Exists(languageFile))
            throw new FileNotFoundException($"No language file {languageFile}");

        var json = File.ReadAllText(languageFile);
        _languageStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        _hasLoaded = true;
    }

    private void LoadFromEmbeddedResources()
    {
        var assembly = typeof(JsonLocalizer).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(x => x.EndsWith("strings.json"))
            .ToList();

        Console.WriteLine("Available embedded resources:");
        foreach (var name in resourceNames) Console.WriteLine($"  {name}");

        foreach (var resource in resourceNames)
        {
            var cleanPath = resource.Replace('\\', '.');
            var parts = cleanPath.Split('.');
            var languageIndex = Array.IndexOf(parts, "Languages") + 1;
            if (languageIndex > 0 && languageIndex < parts.Length - 1)
            {
                var language = parts[languageIndex].Replace("_", "-").Replace("\\", "");
                _languages.Add(language);
                Console.WriteLine($"Found language: {language}");
            }
        }

        if (!_languages.Contains(_language))
        {
            _language = DefaultLanguage;
            Console.WriteLine($"Language not found, falling back to: {_language}");
        }

        var resourceLanguage = _language.Replace("-", "_");
        var resourcePath = $"FikaLauncher.Languages.{resourceLanguage}.strings.json";
        Console.WriteLine($"Trying to load resource: {resourcePath}");

        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null)
        {
            Console.WriteLine($"Available languages: {string.Join(", ", _languages)}");
            throw new FileNotFoundException($"No embedded resource found for language: {_language}");
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        _languageStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        Console.WriteLine($"Successfully loaded embedded strings for language: {_language}");
        _hasLoaded = true;
    }

    protected override void SetLanguage(string language)
    {
        _language = language;

        Reload();

        RefreshUI();
    }

    public override string Get(string key)
    {
        if (!_hasLoaded)
            Reload();

        if (_languageStrings == null)
            throw new Exception("No language strings loaded.");

        if (_languageStrings.TryGetValue(key, out var langStr))
            return langStr.Replace("\\n", "\n");

        return $"{Language}:{key}";
    }

    public override IEnumerable<string> GetAllKeys()
    {
        if (!_hasLoaded)
            Reload();

        return _languageStrings?.Keys ?? Enumerable.Empty<string>();
    }
}