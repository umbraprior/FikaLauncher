using Markdig;
using System;
using System.IO;
using System.Reflection;

namespace FikaLauncher.Services;

public static class TermsService
{
    public static string GetProcessedTerms(bool isDark, bool isLauncherTerms = true)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var currentLanguage = LocalizationService.Instance.CurrentLanguage;
            var resourcePath = isLauncherTerms ? 
                $"FikaLauncher.Assets.Terms.Launcher.{currentLanguage}.md" : 
                $"FikaLauncher.Assets.Terms.Fika.{currentLanguage}.md";
            
            Console.WriteLine($"[Terms] Current language: {currentLanguage}");
            Console.WriteLine($"[Terms] Attempting to load terms from: {resourcePath}");

            Stream? stream = assembly.GetManifestResourceStream(resourcePath);
            
            if (stream == null)
            {
                Console.WriteLine($"[Terms] No localized version found for {currentLanguage}");
                
                resourcePath = isLauncherTerms ? 
                    "FikaLauncher.Assets.Terms.Launcher.en-US.md" : 
                    "FikaLauncher.Assets.Terms.Fika.en-US.md";
                
                Console.WriteLine($"[Terms] Falling back to English terms: {resourcePath}");
                stream = assembly.GetManifestResourceStream(resourcePath);
                
                if (stream == null)
                {
                    Console.WriteLine($"[Terms] Failed to load English terms file: {resourcePath}");
                    Console.WriteLine("[Terms] Available resources:");
                    var resources = assembly.GetManifestResourceNames();
                    foreach (var resource in resources)
                    {
                        Console.WriteLine($"[Terms] - {resource}");
                    }
                    return "Error loading terms of use.";
                }
                Console.WriteLine("[Terms] Successfully loaded English fallback terms");
            }
            else
            {
                Console.WriteLine($"[Terms] Successfully loaded localized terms for {currentLanguage}");
            }

            using (stream)
            using (var reader = new StreamReader(stream))
            {
                var markdown = reader.ReadToEnd();
                Console.WriteLine($"[Terms] Successfully read terms content (length: {markdown.Length} characters)");

                var (backgroundColor, textColor, headingColor, linkColor) = isDark
                    ? ("#242424", "#d4d4d4", "#ffffff", "#569cd6")
                    : ("#ebeef0", "#24292f", "#000000", "#0366d6");

                var noteBlockBgColor = isDark 
                    ? "#2d2d2d" 
                    : "#dfe2e5"; 

                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();

                var html = Markdig.Markdown.ToHtml(markdown, pipeline);
                html = ProcessNoteBlocks(html, isDark, noteBlockBgColor);

                return $@"
                    <html>
                    <head>
                        <style>
                            html {{
                                height: 100%;
                                margin: 0;
                                padding: 0;
                            }}

                            body {{ 
                                font-family: 'Inter', sans-serif;
                                line-height: 1.6;
                                padding: 20px;
                                background-color: {backgroundColor};
                                color: {textColor};
                                margin: 0;
                                height: 100%;
                                overflow-y: auto;
                                box-sizing: border-box;
                            }}

                            h1, h2, h3, h4, h5, h6 {{
                                color: {headingColor};
                                margin-top: 24px;
                                margin-bottom: 16px;
                                font-weight: 600;
                                line-height: 1.25;
                            }}

                            h1 {{ font-size: 2em; margin-top: 0; }}
                            h2 {{ font-size: 1.5em; }}
                            h3 {{ font-size: 1.25em; }}

                            p {{ margin-bottom: 16px !important; }}

                            ul, ol {{
                                margin: 0.8em 0;
                                padding-left: 2em;
                            }}

                            blockquote {{
                                margin: 16px 0;
                                padding: 0 1em;
                                color: {textColor};
                                border-left: 0.25em solid {(isDark ? "#404040" : "#d0d7de")};
                            }}

                            .note-block {{
                                margin: 16px 0;
                                padding: 10px 16px;
                                border-left: 4px solid;
                                border-radius: 6px;
                                background-color: {noteBlockBgColor};
                            }}

                            .note-block-title {{
                                font-weight: 600;
                                margin-bottom: 8px;
                                display: block;
                            }}

                            code {{
                                background-color: {(isDark ? "#1E1E1E" : "#f6f8fa")} !important;
                                padding: 0.2em 0.4em !important;
                                border-radius: 6px !important;
                                font-family: 'Consolas', 'Monaco', 'Courier New', monospace !important;
                                font-size: 85% !important;
                                border: 1px solid {(isDark ? "#333333" : "#e1e4e8")} !important;
                                display: inline-block !important;
                                vertical-align: middle !important;
                                margin: 0.2em 0.4em !important;
                                line-height: 1.4 !important;
                            }}

                            pre {{
                                margin: 16px 0 !important;
                                border-radius: 8px !important;
                                background-color: {(isDark ? "#2D2D2D" : "#F0F0F0")} !important;
                                padding: 12px !important;
                                overflow-x: auto !important;
                                border: 1px solid {(isDark ? "#404040" : "#E0E0E0")} !important;
                                display: block !important;
                            }}

                            pre > code {{
                                padding: 0 !important;
                                margin: 0 !important;
                                display: block !important;
                                white-space: pre !important;
                                font-family: 'Consolas', 'Monaco', 'Courier New', monospace !important;
                                line-height: 1.4 !important;
                                min-height: 0 !important;
                            }}
                        </style>
                    </head>
                    <body>
                        {html}
                    </body>
                    </html>";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load terms: {ex.Message}");
            return "Error loading terms of use.";
        }
    }

    private static string ProcessNoteBlocks(string html, bool isDark, string bgColor)
    {
        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<div class=""markdown-alert markdown-alert-note"">\s*<p class=""markdown-alert-title""[^>]*>.*?Note</p>\s*<p>(.*?)</p>\s*</div>",
            match => {
                var content = match.Groups[1].Value;
                var noteColor = "#58a6ff";
                return $@"<div class=""note-block note"" style=""background-color: {bgColor}; border-left-color: {noteColor} !important;"">
                           <span class=""note-block-title"" style=""color: {noteColor} !important;"">ℹ️ Note</span>
                           <div class=""note-block-content"" style=""font-weight: 600"">{content}</div></div>";
            },
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<div class=""markdown-alert markdown-alert-tip"">\s*<p class=""markdown-alert-title""[^>]*>.*?Tip</p>\s*<p>(.*?)</p>\s*</div>",
            match => {
                var content = match.Groups[1].Value;
                var tipColor = "#3fb950";
                return $@"<div class=""note-block tip"" style=""background-color: {bgColor}; border-left-color: {tipColor} !important;"">
                           <span class=""note-block-title"" style=""color: {tipColor} !important;"">✅ Tip</span>
                           <div class=""note-block-content"" style=""font-weight: 600"">{content}</div></div>";
            },
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<div class=""markdown-alert markdown-alert-important"">\s*<p class=""markdown-alert-title""[^>]*>.*?Important</p>\s*<p>(.*?)</p>\s*</div>",
            match => {
                var content = match.Groups[1].Value;
                var importantColor = "#8957e5";
                return $@"<div class=""note-block important"" style=""background-color: {bgColor}; border-left-color: {importantColor} !important;"">
                           <span class=""note-block-title"" style=""color: {importantColor} !important;"">☑️ Important</span>
                           <div class=""note-block-content"" style=""font-weight: 600"">{content}</div></div>";
            },
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<div class=""markdown-alert markdown-alert-warning"">\s*<p class=""markdown-alert-title""[^>]*>.*?Warning</p>\s*<p>(.*?)</p>\s*</div>",
            match => {
                var content = match.Groups[1].Value;
                var warningColor = "#d29922";
                return $@"<div class=""note-block warning"" style=""background-color: {bgColor}; border-left-color: {warningColor} !important;"">
                           <span class=""note-block-title"" style=""color: {warningColor} !important;"">⚠️ Warning</span>
                           <div class=""note-block-content"" style=""font-weight: 600"">{content}</div></div>";
            },
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<div class=""markdown-alert markdown-alert-caution"">\s*<p class=""markdown-alert-title""[^>]*>.*?Caution</p>\s*<p>(.*?)</p>\s*</div>",
            match => {
                var content = match.Groups[1].Value;
                var cautionColor = "#f85149";
                return $@"<div class=""note-block caution"" style=""background-color: {bgColor}; border-left-color: {cautionColor} !important;"">
                           <span class=""note-block-title"" style=""color: {cautionColor} !important;"">⛔ Caution</span>
                           <div class=""note-block-content"" style=""font-weight: 600"">{content}</div></div>";
            },
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        return html;
    }
}
