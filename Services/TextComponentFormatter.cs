using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CustomClipboardManager.Services
{
    public class TextComponentAnalysis
    {
        public bool HasComponents { get; set; }
        public string ComponentType { get; set; } = string.Empty;
        public int ComponentCount { get; set; }
        public string FormattedText { get; set; } = string.Empty;
        public string CleanText { get; set; } = string.Empty;
        public bool HasCleanVariant => !string.IsNullOrEmpty(CleanText) && CleanText != FormattedText;
        public string SummaryText => ComponentCount > 0 
            ? $"{ComponentCount} {ComponentType}" 
            : string.Empty;
    }

    public static class TextComponentFormatter
    {
        private static readonly Regex MarkdownLinkRegex = new Regex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex SlashCommandRegex = new Regex(@"(?:^|\s)(/[a-zA-Z0-9_\-\.]+)", RegexOptions.Compiled);
        private static readonly Regex HtmlTagRegex = new Regex(@"<([a-zA-Z0-9\-]+)([^>]*)>(.*?)</\1>|<([a-zA-Z0-9\-]+)([^>]*)/>", RegexOptions.Compiled | RegexOptions.Singleline);

        public static TextComponentAnalysis Analyze(string? text)
        {
            var result = new TextComponentAnalysis();
            if (string.IsNullOrWhiteSpace(text)) return result;

            string trimmed = text.Trim();

            // 1. Markdown Links / Command Chips (e.g. [migrate-workflows](slashCommand;migrate-workflows)...)
            var mdMatches = MarkdownLinkRegex.Matches(trimmed);
            if (mdMatches.Count >= 2)
            {
                result.HasComponents = true;
                result.ComponentCount = mdMatches.Count;
                result.ComponentType = I18n.Current.CompComponents;

                // Put each markdown component on its own line while preserving any prefix header
                string formatted = Regex.Replace(trimmed, @"(?<=\))\s+(?=\[)", "\n");
                formatted = Regex.Replace(formatted, @"(:)\s+(?=\[)", ":\n");
                result.FormattedText = formatted;

                // Extract clean text in-place so all surrounding and trailing text is 100% preserved
                string clean = Regex.Replace(trimmed, @"\[([^\]]+)\]\(([^)]+)\)", m =>
                {
                    string label = m.Groups[1].Value.Trim();
                    string target = m.Groups[2].Value.Trim();
                    if (target.Contains("slashCommand;"))
                    {
                        string cmd = target.Substring(target.IndexOf("slashCommand;") + 13).TrimStart('/');
                        return "/" + (string.IsNullOrEmpty(cmd) ? label.TrimStart('/') : cmd);
                    }
                    if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"{label} ({target})";
                    }
                    return label;
                });
                result.CleanText = Regex.Replace(clean, @"(?<=/[a-zA-Z0-9_\-\.]+)\s+(?=/[a-zA-Z0-9_\-\.]+)", "\n");
                return result;
            }

            // 2. Consecutive Slash Commands (e.g. /migrate-workflows /cavecrew /caveman...)
            var slashMatches = SlashCommandRegex.Matches(trimmed);
            if (slashMatches.Count >= 2)
            {
                result.HasComponents = true;
                result.ComponentCount = slashMatches.Count;
                result.ComponentType = I18n.Current.CompCommands;

                // Put newlines between consecutive commands while keeping 100% of all user text intact!
                string formatted = Regex.Replace(trimmed, @"(?<=/[a-zA-Z0-9_\-\.]+)\s+(?=/[a-zA-Z0-9_\-\.]+)", "\n");
                result.FormattedText = formatted;
                result.CleanText = formatted;
                return result;
            }

            // 3. Minified or Single-Line JSON
            if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) || (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
            {
                try
                {
                    using var doc = JsonDocument.Parse(trimmed);
                    string pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                    if (pretty.Contains('\n') && !trimmed.Contains('\n'))
                    {
                        result.HasComponents = true;
                        result.ComponentType = I18n.Current.CompJson;
                        result.ComponentCount = 1;
                        result.FormattedText = pretty;
                        result.CleanText = pretty;
                        return result;
                    }
                }
                catch { }
            }

            // 4. Multiple Consecutive HTML/XML Tags on a single line
            if (trimmed.Contains('<') && trimmed.Contains('>') && !trimmed.Contains('\n'))
            {
                var htmlMatches = HtmlTagRegex.Matches(trimmed);
                if (htmlMatches.Count >= 2)
                {
                    result.HasComponents = true;
                    result.ComponentCount = htmlMatches.Count;
                    result.ComponentType = I18n.Current.CompHtmlXml;

                    string formatted = Regex.Replace(trimmed, @">\s*<", ">\n<");
                    result.FormattedText = formatted;
                    result.CleanText = Regex.Replace(trimmed, @"<[^>]+>", " ").Trim();
                    return result;
                }
            }

            // 5. Delimited Items (e.g. Semicolon-separated statements/items on 1 line)
            if (!trimmed.Contains('\n') && trimmed.Contains(';'))
            {
                var parts = trimmed.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(p => p.Trim())
                                   .Where(p => !string.IsNullOrEmpty(p))
                                   .ToList();
                if (parts.Count >= 3)
                {
                    result.HasComponents = true;
                    result.ComponentCount = parts.Count;
                    result.ComponentType = I18n.Current.CompListItems;
                    result.FormattedText = string.Join(";\n", parts);
                    result.CleanText = string.Join("\n", parts);
                    return result;
                }
            }

            // 6. URL with multiple query parameters (e.g. https://...?a=1&b=2&c=3)
            if ((trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                && trimmed.Contains('?') && trimmed.Contains('&') && !trimmed.Contains('\n'))
            {
                int qIdx = trimmed.IndexOf('?');
                string baseUrl = trimmed.Substring(0, qIdx);
                string queryString = trimmed.Substring(qIdx + 1);
                var qParams = queryString.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                if (qParams.Length >= 2)
                {
                    result.HasComponents = true;
                    result.ComponentCount = qParams.Length;
                    result.ComponentType = I18n.Current.CompQueryParams;

                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine(baseUrl);
                    for (int i = 0; i < qParams.Length; i++)
                    {
                        sb.AppendLine((i == 0 ? "  ? " : "  & ") + Uri.UnescapeDataString(qParams[i]));
                    }
                    result.FormattedText = sb.ToString().TrimEnd();
                    result.CleanText = result.FormattedText;
                    return result;
                }
            }

            result.FormattedText = trimmed;
            result.CleanText = trimmed;
            return result;
        }

        public static string Format(string? text)
        {
            var analysis = Analyze(text);
            return analysis.HasComponents ? analysis.FormattedText : (text ?? string.Empty);
        }

        public static string CleanExtract(string? text)
        {
            var analysis = Analyze(text);
            return analysis.HasComponents ? analysis.CleanText : (text ?? string.Empty);
        }
    }
}
