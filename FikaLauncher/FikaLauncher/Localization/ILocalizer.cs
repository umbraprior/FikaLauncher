using System;
using System.Collections.Generic;

namespace FikaLauncher.Localization;

public interface ILocalizer
{
    string DefaultLanguage { get; set; }
    List<string> Languages { get; }
    string Language { get; set; }
    void Reload();
    string Get(string key);
    event EventHandler LanguageChanged;
    void RefreshUI();
    IEnumerable<string> GetAllKeys();
}