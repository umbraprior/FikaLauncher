﻿using System;
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

        foreach (var file in Directory.GetFiles(_languageJsonDirectory, "*.json"))
        {
            var language = Path.GetFileNameWithoutExtension(file);
            _languages.Add(language);
        }

        if (!_languages.Contains(_language))
            _language = DefaultLanguage;

        var languageFile = Path.Combine(_languageJsonDirectory, _language + ".json");
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
            .Where(x => x.EndsWith(".json"))
            .ToList();

        foreach (var resource in resourceNames)
        {
            var language = Path.GetFileNameWithoutExtension(
                resource.Split('.').Reverse().Skip(1).First()
            );
            _languages.Add(language);
        }

        if (!_languages.Contains(_language))
            _language = DefaultLanguage;

        var resourcePath = $"FikaLauncher.Languages.{_language}.json";
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null)
            throw new FileNotFoundException($"No embedded resource found: {resourcePath}");

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