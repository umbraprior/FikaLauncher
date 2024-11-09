using System;
using Markdig;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using FikaLauncher.Services;
using System.Linq;

namespace FikaLauncher.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    private readonly Window? _mainWindow;
    
    [ObservableProperty]
    private string _htmlContent = string.Empty;

    public AboutViewModel()
    {
        _mainWindow = App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
            ? desktop.MainWindow 
            : null;

        if (_mainWindow != null)
        {
            _mainWindow.ActualThemeVariantChanged += OnThemeChanged;
        }
        
        LoadReadmeAsync();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        LoadReadmeAsync();
    }

    private async void LoadReadmeAsync()
    {
        try
        {
            Console.WriteLine("Starting LoadReadme");
            
            var markdown = await GitHubReadmeService.GetReadmeContentAsync();
            
            Console.WriteLine("Markdown content loaded from cache or GitHub");

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
            
            Console.WriteLine("Pipeline built");

            var html = Markdig.Markdown.ToHtml(markdown, pipeline);

            html = RemoveTableOfContents(html);
            html = ProcessNoteBlocks(html, App.Current?.RequestedThemeVariant == ThemeVariant.Dark);


            var isDark = App.Current?.RequestedThemeVariant == ThemeVariant.Dark;
                
            var (backgroundColor, textColor, headingColor, linkColor) = isDark
                ? ("#242424", "#d4d4d4", "#ffffff", "#569cd6")
                : ("#ebeef0", "#24292f", "#000000", "#0366d6");

            var noteBlockBgColor = isDark 
                ? "#2d2d2d"
                : "#dfe2e5";

            HtmlContent = $@"
                <html>
                <head>
                    <style>
                        body {{ 
                            font-family: 'Inter', sans-serif;
                            line-height: 1.6;
                            padding: 20px;
                            background-color: {backgroundColor};
                            color: {textColor};
                            margin: 0;
                        }}

                        /* Code block container */
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

                        /* Remove any bottom padding/margin from the last line in code blocks */
                        pre > code:last-child {{
                            margin-bottom: 0 !important;
                            padding-bottom: 0 !important;
                        }}

                        /* Inline code */
                        :not(pre) > code {{
                            background-color: {(isDark ? "#1E1E1E" : "#f6f8fa")} !important;
                            padding: 0.2em 0.4em !important;
                            border-radius: 6px !important;
                            font-family: 'Consolas', 'Monaco', 'Courier New', monospace !important;
                            font-size: 85% !important;
                            border: 1px solid {(isDark ? "#333333" : "#e1e4e8")} !important;
                            display: inline-block !important;
                            vertical-align: middle !important;
                            margin: 0.2em 0.4em !important;  // Increased horizontal and vertical margins
                            line-height: 1.4 !important;
                        }}

                        pre > code {{
                            font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
                            font-size: 14px;
                            line-height: 1.4;
                            display: block;
                            color: {(isDark ? "#D4D4D4" : "#24292e")};
                            
                        }}

                        /* Syntax highlighting */
                        .keyword {{ color: {(isDark ? "#569CD6" : "#0000FF")} !important; }}
                        .string {{ color: {(isDark ? "#CE9178" : "#A31515")} !important; }}
                        .number {{ color: {(isDark ? "#B5CEA8" : "#098658")} !important; }}
                        .comment {{ color: {(isDark ? "#6A9955" : "#008000")} !important; }}
                        .class-name {{ color: {(isDark ? "#4EC9B0" : "#2B91AF")} !important; }}
                        .function {{ color: {(isDark ? "#DCDCAA" : "#795E26")} !important; }}
                        .type {{ color: {(isDark ? "#4EC9B0" : "#2B91AF")} !important; }}
                        .property {{ color: {(isDark ? "#9CDCFE" : "#0451A5")} !important; }}
                        .operator {{ color: {(isDark ? "#D4D4D4" : "#000000")} !important; }}

                        /* Regular text elements */
                        p, ul, ol, li, table {{
                            color: {textColor};
                        }}

                        h1, h2, h3, h4, h5, h6 {{
                            color: {headingColor};
                            margin-top: 24px;
                            margin-bottom: 16px;
                            font-weight: 600;
                        }}

                        a {{
                            color: {linkColor};
                            text-decoration: none;
                        }}
                        a:hover {{
                            text-decoration: underline;
                        }}

                        /* Inline code */
                        code.inline-code {{
                            background-color: {(isDark ? "#1E1E1E" : "#f6f8fa")} !important;
                            color: {(isDark ? "#D4D4D4" : "#24292e")} !important;
                            padding: 0.1em 0.4em !important;
                            border: 1px solid {(isDark ? "#333333" : "#e1e4e8")} !important;
                            display: inline !important;
                            font-family: inherit !important;
                            font-size: 0.95em !important;
                            line-height: 1 !important;
                            position: relative !important;
                            top: -0.1em !important;
                            border-radius: 2px !important;
                        }}

                        /* Code block container */
                        pre {{
                            margin: 16px 0 32px 0 !important;  // Changed bottom margin to 32px
                            border-radius: 8px !important;
                            background-color: {(isDark ? "#1E1E1E" : "#f6f8fa")} !important;
                            padding: 12px !important;
                            overflow-x: auto !important;
                            border: 1px solid {(isDark ? "#333333" : "#e1e4e8")} !important;
                            display: block !important;
                            position: relative !important;
                        }}

                        pre > code {{
                            font-family: 'Consolas', 'Monaco', 'Courier New', monospace !important;
                            font-size: 14px !important;
                            line-height: 1.4 !important;
                            display: block !important;
                            color: {(isDark ? "#D4D4D4" : "#24292e")} !important;
                            background-color: transparent !important;
                            border: none !important;
                            padding: 0 !important;
                            margin: 0 !important;  /* Added margin: 0 */
                            white-space: pre !important;
                        }}

                        /* Plain text highlighting */
                        .plaintext {{
                            color: {(isDark ? "#CE9178" : "#A31515")} !important;  /* Using string color for full block */
                        }}

                        .url {{
                            color: {(isDark ? "#569CD6" : "#0366d6")} !important;
                        }}

                        .ip-address {{
                            color: {(isDark ? "#4EC9B0" : "#2B91AF")} !important;
                        }}

                        pre {{
                            margin: 16px 0 !important;
                            border-radius: 8px !important;
                            background-color: {(isDark ? "#1E1E1E" : "#f6f8fa")} !important;
                            padding: 16px !important;
                            overflow-x: auto !important;
                            border: 1px solid {(isDark ? "#333333" : "#e1e4e8")} !important;
                        }}

                        pre > code {{
                            font-family: 'Consolas', 'Monaco', 'Courier New', monospace !important;
                            font-size: 14px !important;
                            line-height: 1.4 !important;
                            display: block !important;
                            color: {(isDark ? "#D4D4D4" : "#24292e")} !important;
                            background-color: transparent !important;
                            border: none !important;
                            padding: 0 !important;
                            white-space: pre !important;
                        }}

                        code.inline-code {{
                            background-color: {(isDark ? "#1E1E1E" : "#f6f8fa")} !important;
                            color: {(isDark ? "#D4D4D4" : "#24292e")} !important;
                            padding: 0.2em 0.4em !important;
                            border-radius: 6px !important;
                            font-family: 'Consolas', 'Monaco', 'Courier New', monospace !important;
                            font-size: 85% !important;
                            border: 1px solid {(isDark ? "#333333" : "#e1e4e8")} !important;
                            white-space: nowrap !important;
                            display: inline !important;
                            margin: 0 0.2em !important;
                        }}

                        /* Full block highlighting */
                        .plaintext {{
                            color: {(isDark ? "#CE9178" : "#A31515")} !important;
                        }}

                        .batch {{
                            color: {(isDark ? "#569CD6" : "#0000FF")} !important;
                        }}

                        /* Comment blocks */
                        .comment-block {{
                            color: {(isDark ? "#6A9955" : "#008000")} !important;
                            display: inline !important;
                            width: 100% !important;
                        }}

                        pre {{
                            margin: 16px 0 !important;
                            border-radius: 8px !important;
                            background-color: {(isDark ? "#1E1E1E" : "#f6f8fa")} !important;
                            padding: 12px !important;
                            overflow-x: auto !important;
                            border: 1px solid {(isDark ? "#333333" : "#e1e4e8")} !important;
                            display: block !important;
                            position: relative !important;
                        }}

                        pre > code {{
                            font-family: 'Consolas', 'Monaco', 'Courier New', monospace !important;
                            font-size: 14px !important;
                            line-height: 1.4 !important;
                            display: block !important;
                            color: {(isDark ? "#D4D4D4" : "#24292e")} !important;
                            background-color: transparent !important;
                            border: none !important;
                            padding: 0 !important;
                            margin: 0 !important;
                            white-space: pre !important;
                        }}

                        /* Ensure higher specificity for inline code styles */
                        html body :not(pre) > code.inline-code {{
                            background-color: {(isDark ? "#1E1E1E" : "#f6f8fa")} !important;
                            padding: 0.4em 0.6em;  // Ensure sufficient padding
                            border-radius: 6px;
                            font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
                            font-size: 85%;
                            border: 1px solid {(isDark ? "#333333" : "#e1e4e8")};
                            display: inline-block;
                            vertical-align: middle;
                            margin: 0 0.2em;
                            line-height: 1.4;  // Adjust line height for better vertical spacing
                            min-height: 1.4em;  // Ensure minimum height for the container
                        }}

                        /* Add spacing after paragraphs */
                        p {{
                            margin-bottom: 16px !important;
                        }}

                        /* Inline code wrapper */
                        .inline-code-wrapper {{
                            display: inline-flex !important;
                            align-items: baseline !important;
                        }}

                        .inline-code-container {{
                            display: inline-block !important;
                            padding: 0 4px !important;
                            background-color: {(isDark ? "#2D2D2D" : "#F0F0F0")} !important;
                            border: 1px solid {(isDark ? "#404040" : "#E0E0E0")} !important;
                            border-radius: 3px !important;
                        }}

                        .inline-code-container code {{
                            font-family: 'Consolas', 'Monaco', 'Courier New', monospace !important;
                            font-size: 0.9em !important;
                            color: {(isDark ? "#E0E0E0" : "#333333")} !important;
                            background: none !important;
                            border: none !important;
                            padding: 0 !important;
                            margin: 0 !important;
                        }}

                        .code-block-container {{
                            margin: 12px 0 !important;
                        }}

                        .code-block-container pre {{
                            background-color: {(isDark ? "#2D2D2D" : "#F0F0F0")} !important;
                            border: 1px solid {(isDark ? "#404040" : "#E0E0E0")} !important;
                            border-radius: 6px !important;
                            padding: 8px 12px !important;
                            margin: 0 !important;
                            overflow-x: auto !important;
                        }}

                        .code-block-container code {{
                            font-family: 'Consolas', 'Monaco', 'Courier New', monospace !important;
                            font-size: 13px !important;
                            line-height: 1.45 !important;
                            display: block !important;
                            color: {(isDark ? "#D4D4D4" : "#24292e")} !important;
                            white-space: pre !important;
                            padding: 0 !important;
                            margin: 0 !important;
                        }}

                        .inline-code-container .file-path {{
                            color: {(isDark ? "#4EC9B0" : "#2B91AF")} !important;
                        }}

                        .inline-code-container .url {{
                            color: {(isDark ? "#569CD6" : "#0366d6")} !important;
                        }}

                        .inline-code-container .ip-address {{
                            color: {(isDark ? "#4EC9B0" : "#2B91AF")} !important;
                        }}

                        .inline-code-container .keyword {{
                            color: {(isDark ? "#569CD6" : "#0000FF")} !important;
                        }}

                        .inline-code-container .string {{
                            color: {(isDark ? "#CE9178" : "#A31515")} !important;
                        }}

                        .inline-code-container .number {{
                            color: {(isDark ? "#B5CEA8" : "#098658")} !important;
                        }}

                        /* Add these new styles inside the <style> tag */
                        .note-block {{
                            padding: 16px 16px 16px 12px !important;
                            margin: 16px 0 !important;
                            border-left-width: 4px !important;
                            border-left-style: solid !important;
                            border-radius: 4px !important;
                            background-color: {noteBlockBgColor};
                        }}

                        /* Blue for Note */
                        .note-block.note {{
                            border-left-color: #58a6ff !important;
                        }}
                        .note-block.note .note-block-title {{
                            color: #58a6ff !important;
                        }}

                        /* Purple for Important */
                        .note-block.important {{
                            border-left-color: #8957e5 !important;
                        }}
                        .note-block.important .note-block-title {{
                            color: #8957e5 !important;
                        }}

                        /* Yellow for Warning */
                        .note-block.warning {{
                            border-left-color: #d29922 !important;
                        }}
                        .note-block.warning .note-block-title {{
                            color: #d29922 !important;
                        }}

                        /* Red for Caution */
                        .note-block.caution {{
                            border-left-color: #f85149 !important;
                        }}
                        .note-block.caution .note-block-title {{
                            color: #f85149 !important;
                        }}

                        .note-block-title {{
                            font-weight: 600 !important;
                            margin-bottom: 0.5em !important;
                            display: block !important;
                        }}

                        /* Green for Tip */
                        .note-block.tip {{
                            border-left-color: #3fb950 !important;
                        }}
                        .note-block.tip .note-block-title {{
                            color: #3fb950 !important;
                        }}
                    </style>
                </head>
                <body>
                    {ProcessCodeBlocks(html, isDark)}
                </body>
                </html>";

            Console.WriteLine("HTML content set");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in LoadReadme: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            var errorColor = App.Current?.RequestedThemeVariant == ThemeVariant.Dark ? "#f14c4c" : "#ff0000";
            HtmlContent = $@"
                <div style='color: {errorColor}; padding: 20px;'>
                    <h3>Error loading content</h3>
                    <p>{ex.Message}</p>
                    <pre>{ex.StackTrace}</pre>
                </div>";
        }
    }

    private string ProcessCodeBlocks(string html, bool isDark)
    {
        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<pre><code(?:\s+class=""language-(\w+)"")?>([^<]*)</code></pre>",
            match =>
            {
                var language = match.Groups[1].Success ? match.Groups[1].Value : "plaintext";
                var code = System.Web.HttpUtility.HtmlDecode(match.Groups[2].Value)
                    .TrimStart('\n')
                    .TrimEnd('\n')
                    .TrimEnd();
                
                code = language switch
                {
                    "csharp" => HighlightCSharp(code, isDark),
                    "json" => HighlightJson(code, isDark),
                    "bat" or "cmd" or "batch" => $"<span class=\"batch\">{code}</span>",
                    "plaintext" => $"<span class=\"plaintext\">{code}</span>",
                    _ => $"<span class=\"plaintext\">{code}</span>"
                };
                
                return $"<div class=\"code-block-container\"><pre><code class=\"language-{language}\">{code}</code></pre></div>";
            }
        );

        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<code>([^<]+)</code>",
            match =>
            {
                var code = System.Web.HttpUtility.HtmlDecode(match.Groups[1].Value);
                
                var highlightedCode = code switch
                {
                    var x when x.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) 
                        || x.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                        || x.Contains("/") || x.Contains("\\") 
                        => $"<span class=\"file-path\">{code}</span>",
                    
                    var x when x.StartsWith("http://") || x.StartsWith("https://") || x.StartsWith("ws://")
                        => $"<span class=\"url\">{code}</span>",
                    
                    var x when x.Contains(":") && x.Any(c => char.IsDigit(c))
                        => $"<span class=\"ip-address\">{code}</span>",
                    
                    _ => HighlightInlineCode(code, isDark)
                };

                return $"<span class=\"inline-code-container\"><code>{highlightedCode}</code></span>";
            }
        );

        return html;
    }

    private string HighlightCSharp(string code, bool isDark)
    {
        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(//[^\n]*|/\*[\s\S]*?\*/)",
            "<span class=\"comment-block\">$1</span>"
        );

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"([\+\-\*/%=<>!&\|\^~\?\:]+)",
            "<span class=\"operator\">$1</span>"
        );

        var keywords = new[] { 
            "using", "public", "private", "protected", "internal", "class", "interface", 
            "void", "string", "int", "bool", "var", "new", "return", "static", "async", 
            "await", "try", "catch", "throw", "if", "else", "foreach", "in", "for", "while",
            "do", "switch", "case", "break", "continue", "default", "namespace", "this",
            "virtual", "override", "abstract", "sealed", "readonly", "const", "null", "true", "false"
        };

        foreach (var keyword in keywords)
        {
            code = System.Text.RegularExpressions.Regex.Replace(
                code,
                $@"\b{keyword}\b(?![""'])",
                $"<span class=\"keyword\">{keyword}</span>"
            );
        }

        var types = new[] {
            "string", "int", "bool", "double", "float", "decimal", "object", "char", "byte",
            "sbyte", "uint", "long", "ulong", "short", "ushort", "dynamic", "var", "void",
            "Task", "List", "Dictionary", "IEnumerable", "Array", "Exception"
        };

        foreach (var type in types)
        {
            code = System.Text.RegularExpressions.Regex.Replace(
                code,
                $@"\b{type}\b(?![""'])",
                $"<span class=\"type\">{type}</span>"
            );
        }

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(@""(?:""""|[^""])*""|""(?:\\.|[^""\\])*"")",
            "<span class=\"string\">$1</span>"
        );

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b|0x[a-fA-F0-9]+\b",
            "<span class=\"number\">$0</span>"
        );

        return code;
    }

    private string HighlightJson(string code, bool isDark)
    {
        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(//[^\n]*|/\*[\s\S]*?\*/)",
            "<span class=\"comment-block\">$1</span>"
        );

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"""([^""\\]*(?:\\.[^""\\]*)*)""(\s*:)",
            "<span class=\"property\">\"$1\"</span>$2"
        );

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @":\s*""([^""\\]*(?:\\.[^""\\]*)*)""",
            ": <span class=\"string\">\"$1\"</span>"
        );

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"\b(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)\b",
            "<span class=\"number\">$1</span>"
        );

        var keywords = new[] { "true", "false", "null" };
        foreach (var keyword in keywords)
        {
            code = System.Text.RegularExpressions.Regex.Replace(
                code,
                $@"\b{keyword}\b",
                $"<span class=\"keyword\">{keyword}</span>"
            );
        }

        return code;
    }

    private string HighlightBatch(string code, bool isDark)
    {
        var commands = new[] { 
            "cd", "dir", "copy", "del", "mkdir", "rmdir", "echo", "set", "if", "else", 
            "goto", "call", "start", "exit", "rem", "type", "for", "in", "do"
        };

        foreach (var command in commands)
        {
            code = System.Text.RegularExpressions.Regex.Replace(
                code,
                $@"\b{command}\b",
                $"<span class=\"keyword\">{command}</span>"
            );
        }

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"%([^%]+)%",
            "<span class=\"property\">%$1%</span>"
        );

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(?m)^[\s]*::.*$|^[\s]*REM.*$",
            match => $"<span class=\"comment\">{match.Value}</span>"
        );

        return code;
    }

    private string HighlightPlainText(string code, bool isDark)
    {
        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(https?:\/\/[^\s<]+[^<.,:;""\'\]\s])",
            "<span class=\"url\">$1</span>"
        );

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(?::\d+)?)\b",
            "<span class=\"ip-address\">$1</span>"
        );

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"\b(\d+)\b",
            "<span class=\"number\">$1</span>"
        );

        return code;
    }

    private string HighlightInlineCode(string code, bool isDark)
    {
        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"\b(true|false|null|this|new|return|if|else|var|const|let|function|class|public|private|static|void|string|int|bool)\b",
            "<span class=\"keyword\">{1}</span>"
        );

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(['""])(?:\\\1|.)*?\1",
            "<span class=\"string\">$0</span>"
        );

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"\b\d+\b",
            "<span class=\"number\">$0</span>"
        );

        return code;
    }

    private string RemoveTableOfContents(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<details[^>]*>.*?</details>",
            "",
            System.Text.RegularExpressions.RegexOptions.Singleline
        );
    }

    private string ProcessNoteBlocks(string html, bool isDark)
    {
        var bgColor = isDark 
            ? "#2d2d2d"
            : "#dfe2e5";

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

    public override void Dispose()
    {
        if (_mainWindow != null)
        {
            _mainWindow.ActualThemeVariantChanged -= OnThemeChanged;
        }
        base.Dispose();
    }
}
