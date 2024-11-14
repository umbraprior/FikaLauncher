using System.Reflection;

namespace FikaLauncher.Services;

public static class EmbeddedResourceHelper
{
    public static bool IsResourceEmbedded(string language, string resourceType)
    {
        if (language != "en-US") return false;

        var assembly = Assembly.GetExecutingAssembly();
        var resourceLanguage = language.Replace("-", "_");

        switch (resourceType.ToLower())
        {
            case "strings":
                return assembly.GetManifestResourceStream($"FikaLauncher.Languages.{resourceLanguage}.strings.json") !=
                       null;
            case "terms":
                var launcherTerms =
                    assembly.GetManifestResourceStream(
                        $"FikaLauncher.Languages.{resourceLanguage}.launcher-terms.md") != null;
                var fikaTerms =
                    assembly.GetManifestResourceStream($"FikaLauncher.Languages.{resourceLanguage}.fika-terms.md") !=
                    null;
                return launcherTerms || fikaTerms;
            case "readme":
                return assembly.GetManifestResourceStream($"FikaLauncher.Languages.{resourceLanguage}.README.md") !=
                       null;
            default:
                return false;
        }
    }
}