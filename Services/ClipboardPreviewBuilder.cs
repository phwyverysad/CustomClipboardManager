using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CustomClipboardManager.Models;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace CustomClipboardManager.Services
{
    public static class ClipboardPreviewBuilder
    {
        public static ClipboardPreviewInfo Build(ClipboardItem item)
        {
            var info = new ClipboardPreviewInfo(item);

            switch (item.ContentType)
            {
                case ClipboardContentType.Image:
                    BuildImagePreview(info, item);
                    break;
                case ClipboardContentType.FileDropList:
                    BuildFilePreview(info, item);
                    break;
                default:
                    if (item.Category == SmartCategory.ColorCode)
                    {
                        BuildColorPreview(info, item);
                    }
                    else if (item.Category == SmartCategory.Link)
                    {
                        BuildLinkPreview(info, item);
                    }
                    else
                    {
                        BuildTextPreview(info, item);
                    }
                    break;
            }

            return info;
        }

        private static void BuildImagePreview(ClipboardPreviewInfo info, ClipboardItem item)
        {
            var strings = I18n.Current;
            info.HasImagePreview = true;
            info.ImageSource = item.ImageContent;
            info.CanSaveImage = item.ImageContent != null;

            int w = item.ImageContent?.PixelWidth ?? 0;
            int h = item.ImageContent?.PixelHeight ?? 0;
            double dpiX = item.ImageContent?.DpiX ?? 96;
            double dpiY = item.ImageContent?.DpiY ?? 96;

            string ar = CalculateAspectRatio(w, h);
            info.ImageResolutionText = $"{w} × {h} px";
            info.ImageAspectRatioText = ar;

            info.Title = strings.PreviewImageTitle;
            info.Subtitle = $"{w} × {h} px • {ar} • {item.TimeAgo}";
            info.BadgeText = strings.BadgeImage;
            info.BadgeBackground = new SolidColorBrush(Color.FromArgb(0x26, 0xD2, 0x47, 0x26));
            info.BadgeBorder = new SolidColorBrush(Color.FromArgb(0x4D, 0xD2, 0x47, 0x26));

            info.DetailsList.Add(new DetailProperty { Label = strings.DetailItemType, Value = strings.DetailItemTypeImage });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailResolution, Value = $"{w} × {h} px", CopyValue = $"{w}x{h}" });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailMegapixels, Value = $"{(w * h) / 1000000.0:F2} MP" });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailAspectRatio, Value = ar });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailDpi, Value = $"{dpiX:0} × {dpiY:0} DPI" });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailColorFormat, Value = item.ImageContent?.Format.ToString() ?? "32-bit BGRA" });
            long rawBytes = (long)w * h * 4;
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailMemoryFootprint, Value = $"~{FileHelper.FormatFileSize(rawBytes)}" });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailPinned, Value = item.IsPinned ? strings.DetailYesPinned : strings.DetailNoNotPinned });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailTimeCopied, Value = $"{item.FullTimestamp} ({item.TimeAgo})" });
        }

        private static void BuildFilePreview(ClipboardPreviewInfo info, ClipboardItem item)
        {
            var strings = I18n.Current;
            info.HasFilePreview = true;
            var paths = item.FilePaths ?? new List<string>();
            if (!paths.Any() && !string.IsNullOrEmpty(item.FilePath))
            {
                paths = new List<string> { item.FilePath };
            }

            info.BadgeText = paths.Count > 1 ? strings.BadgeFiles : strings.BadgeFile;
            info.BadgeBackground = new SolidColorBrush(Color.FromArgb(0x26, 0x00, 0x78, 0xD7));
            info.BadgeBorder = new SolidColorBrush(Color.FromArgb(0x4D, 0x00, 0x78, 0xD7));

            if (paths.Count == 1)
            {
                string path = paths[0];
                bool isDir = Directory.Exists(path);
                bool exists = isDir || File.Exists(path);

                info.FileName = item.FileName ?? Path.GetFileName(path);
                info.FilePath = path;
                info.FileSizeText = item.FileSizeText;
                info.FileExtension = item.FileExtension;
                info.LargeFileIcon = FileHelper.GetLargeFileIcon(path, isDir);
                info.Title = info.FileName;
                info.Subtitle = $"{item.FileSizeText} • {FileHelper.GetFileTypeDescription(path, isDir)}";
                info.CanOpenFile = exists;
                info.CanOpenFolder = exists;

                if (!isDir && FileHelper.IsImageFile(path))
                {
                    info.ImageSource = FileHelper.LoadImageFromFile(path);
                    if (info.ImageSource != null)
                    {
                        info.HasImagePreview = true;
                        info.CanSaveImage = true;
                        int iw = info.ImageSource.PixelWidth;
                        int ih = info.ImageSource.PixelHeight;
                        info.ImageResolutionText = $"{iw} × {ih} px";
                        info.ImageAspectRatioText = CalculateAspectRatio(iw, ih);
                        info.Subtitle = $"{iw} × {ih} px • {item.FileSizeText} • {strings.FileTypeImageFile}";
                    }
                }
                else if (!isDir && FileHelper.IsTextFile(path))
                {
                    info.FileTextContent = FileHelper.ReadTextFileSnippet(path);
                    info.HasFileTextContent = !string.IsNullOrEmpty(info.FileTextContent);
                }

                info.DetailsList.Add(new DetailProperty { Label = strings.DetailFileName, Value = info.FileName, CopyValue = info.FileName });
                info.DetailsList.Add(new DetailProperty { Label = strings.DetailFileType, Value = FileHelper.GetFileTypeDescription(path, isDir) });
                info.DetailsList.Add(new DetailProperty { Label = strings.DetailFullPath, Value = path, CopyValue = path });

                if (exists)
                {
                    if (isDir)
                    {
                        var di = new DirectoryInfo(path);
                        info.DetailsList.Add(new DetailProperty { Label = strings.DetailDirectorySize, Value = item.FileSizeText ?? strings.DetailCalculating });
                        info.DetailsList.Add(new DetailProperty { Label = strings.DetailCreated, Value = di.CreationTime.ToString("g", I18n.Culture) });
                        info.DetailsList.Add(new DetailProperty { Label = strings.DetailLastModified, Value = di.LastWriteTime.ToString("g", I18n.Culture) });
                        info.DetailsList.Add(new DetailProperty { Label = strings.DetailAttributes, Value = di.Attributes.ToString() });
                    }
                    else
                    {
                        var fi = new FileInfo(path);
                        info.DetailsList.Add(new DetailProperty { Label = strings.DetailPayloadSize, Value = $"{item.FileSizeText} ({fi.Length:N0} B)", CopyValue = fi.Length.ToString() });
                        info.DetailsList.Add(new DetailProperty { Label = strings.FilterFiles, Value = fi.Extension });
                        info.DetailsList.Add(new DetailProperty { Label = strings.DetailCreated, Value = fi.CreationTime.ToString("g", I18n.Culture) });
                        info.DetailsList.Add(new DetailProperty { Label = strings.DetailLastModified, Value = fi.LastWriteTime.ToString("g", I18n.Culture) });
                        info.DetailsList.Add(new DetailProperty { Label = strings.DetailLastAccessed, Value = fi.LastAccessTime.ToString("g", I18n.Culture) });
                        info.DetailsList.Add(new DetailProperty { Label = strings.DetailAttributes, Value = fi.Attributes.ToString() });
                    }
                }
                else
                {
                    info.DetailsList.Add(new DetailProperty { Label = strings.DetailStatus, Value = strings.DetailStatusNotFound });
                }

                info.DetailsList.Add(new DetailProperty { Label = strings.DetailTimeCopied, Value = $"{item.FullTimestamp} ({item.TimeAgo})" });
            }
            else
            {
                // Multi-file
                info.IsMultipleFiles = true;
                info.Title = string.Format(strings.PreviewFilesTitleFormat, paths.Count);
                info.Subtitle = string.Format(strings.PreviewFilesSubtitle, item.FileSizeText, paths.Count);
                info.CanOpenFolder = paths.Any(p => File.Exists(p) || Directory.Exists(p));

                var fileList = new List<FileListItem>();
                foreach (var p in paths)
                {
                    bool isD = Directory.Exists(p);
                    long s = 0;
                    if (!isD && File.Exists(p))
                    {
                        try { s = new FileInfo(p).Length; } catch { }
                    }

                    fileList.Add(new FileListItem
                    {
                        Name = Path.GetFileName(p),
                        Path = p,
                        SizeText = isD ? strings.DetailFolderType : FileHelper.FormatFileSize(s),
                        Icon = FileHelper.GetFileIcon(p, isD),
                        IsDirectory = isD
                    });
                }
                info.MultipleFilesList = fileList;

                info.DetailsList.Add(new DetailProperty { Label = strings.DetailTotalItems, Value = string.Format(strings.SelectionCountFormat, paths.Count) });
                info.DetailsList.Add(new DetailProperty { Label = strings.DetailCombinedSize, Value = item.FileSizeText ?? strings.DetailCalculating });
                info.DetailsList.Add(new DetailProperty { Label = strings.DetailPrimaryPath, Value = paths[0], CopyValue = paths[0] });
                info.DetailsList.Add(new DetailProperty { Label = strings.DetailTimeCopied, Value = $"{item.FullTimestamp} ({item.TimeAgo})" });
            }
        }

        private static void BuildColorPreview(ClipboardPreviewInfo info, ClipboardItem item)
        {
            var strings = I18n.Current;
            info.HasColorPreview = true;
            string text = item.TextContent?.Trim() ?? string.Empty;

            Color color = Colors.Transparent;
            try
            {
                var converted = ColorConverter.ConvertFromString(text);
                if (converted is Color c) color = c;
            }
            catch
            {
                color = Colors.DodgerBlue;
            }

            info.ColorBrush = new SolidColorBrush(color);
            info.ColorHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            string colorHexAlpha = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            info.ColorRgb = $"rgb({color.R}, {color.G}, {color.B})";
            string colorRgba = $"rgba({color.R}, {color.G}, {color.B}, {(color.A / 255.0):0.##})";

            RgbToHsl(color.R, color.G, color.B, out double h, out double s, out double l);
            info.ColorHsl = $"hsl({h:0}°, {s * 100:0}%, {l * 100:0}%)";

            RgbToHsv(color.R, color.G, color.B, out double vh, out double vs, out double vv);
            info.ColorHsv = $"hsv({vh:0}°, {vs * 100:0}%, {vv * 100:0}%)";

            info.Title = strings.PreviewColorTitle;
            info.Subtitle = $"{info.ColorHex} • {info.ColorRgb}";
            info.BadgeText = strings.BadgeColor;
            info.BadgeBackground = new SolidColorBrush(Color.FromArgb(0x26, 0x10, 0x7C, 0x10));
            info.BadgeBorder = new SolidColorBrush(Color.FromArgb(0x4D, 0x10, 0x7C, 0x10));

            info.DetailsList.Add(new DetailProperty { Label = "HEX (RGB)", Value = info.ColorHex, CopyValue = info.ColorHex });
            info.DetailsList.Add(new DetailProperty { Label = "HEX (RGBA)", Value = colorHexAlpha, CopyValue = colorHexAlpha });
            info.DetailsList.Add(new DetailProperty { Label = "RGB", Value = info.ColorRgb, CopyValue = info.ColorRgb });
            info.DetailsList.Add(new DetailProperty { Label = "RGBA", Value = colorRgba, CopyValue = colorRgba });
            info.DetailsList.Add(new DetailProperty { Label = "HSL", Value = info.ColorHsl, CopyValue = info.ColorHsl });
            info.DetailsList.Add(new DetailProperty { Label = "HSV / HSB", Value = info.ColorHsv, CopyValue = info.ColorHsv });
            info.DetailsList.Add(new DetailProperty { Label = "CSS", Value = $"color: {info.ColorHex};", CopyValue = $"color: {info.ColorHex};" });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailTimeCopied, Value = $"{item.FullTimestamp} ({item.TimeAgo})" });
        }

        private static void BuildLinkPreview(ClipboardPreviewInfo info, ClipboardItem item)
        {
            var strings = I18n.Current;
            info.HasLinkPreview = true;
            string url = item.TextContent?.Trim() ?? string.Empty;
            info.LinkUrl = url;
            info.CanOpenLink = true;

            string host = string.Empty;
            string scheme = "HTTPS";
            string path = "/";

            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                {
                    host = uri.Host;
                    scheme = uri.Scheme.ToUpperInvariant();
                    path = uri.PathAndQuery;
                }
            }
            catch { }

            info.LinkDomain = string.IsNullOrEmpty(host) ? url : host;
            info.Title = strings.PreviewLinkTitle;
            info.Subtitle = info.LinkDomain;
            info.BadgeText = strings.BadgeLink;
            info.BadgeBackground = new SolidColorBrush(Color.FromArgb(0x26, 0x00, 0x78, 0xD7));
            info.BadgeBorder = new SolidColorBrush(Color.FromArgb(0x4D, 0x00, 0x78, 0xD7));

            info.DetailsList.Add(new DetailProperty { Label = strings.DetailDomain, Value = info.LinkDomain, CopyValue = info.LinkDomain });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailProtocol, Value = scheme });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailPathQuery, Value = path, CopyValue = path });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailFullUrl, Value = url, CopyValue = url });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailUrlLength, Value = string.Format(strings.DetailUrlLengthFormat, url.Length) });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailTimeCopied, Value = $"{item.FullTimestamp} ({item.TimeAgo})" });
        }

        private static void BuildTextPreview(ClipboardPreviewInfo info, ClipboardItem item)
        {
            var strings = I18n.Current;
            info.HasTextPreview = true;
            string text = item.TextContent ?? string.Empty;
            info.TextContent = text;
            info.IsCode = item.Category == SmartCategory.Code;

            int chars = text.Length;
            int charsNoSpaces = text.Count(c => !char.IsWhiteSpace(c));
            int lines = text.Split('\n').Length;
            int words = Regex.Matches(text, @"\b[\w'-]+\b").Count;
            long bytes = System.Text.Encoding.UTF8.GetByteCount(text);

            info.Title = info.IsCode ? strings.PreviewCodeTitle : strings.PreviewTextTitle;
            info.Subtitle = string.Format(strings.PreviewTextSubtitle, lines, words, chars);
            info.BadgeText = info.IsCode ? strings.BadgeCode : strings.BadgeText;

            // Analyze and format structured components (markdown chips, slash commands, JSON, tags, etc.)
            var analysis = TextComponentFormatter.Analyze(text);
            if (analysis.HasComponents)
            {
                info.HasStructuredComponents = true;
                info.ComponentSummaryText = string.Format(strings.DetectedPrefix, analysis.SummaryText);
                info.RawTextContent = text;
                info.FormattedTextContent = analysis.FormattedText;
                info.CleanTextContent = analysis.CleanText;
                info.TextFormatMode = 0; // Default to Organized/Formatted View
                info.Subtitle = string.Format(strings.PreviewComponentSubtitle, lines, analysis.SummaryText, chars);
            }

            if (info.IsCode)
            {
                info.BadgeBackground = new SolidColorBrush(Color.FromArgb(0x26, 0xEA, 0xA3, 0x00));
                info.BadgeBorder = new SolidColorBrush(Color.FromArgb(0x4D, 0xEA, 0xA3, 0x00));
            }
            else
            {
                info.BadgeBackground = new SolidColorBrush(Color.FromArgb(0x1A, 0x80, 0x80, 0x80));
                info.BadgeBorder = new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80));
            }

            info.DetailsList.Add(new DetailProperty { Label = strings.DetailCharacters, Value = string.Format(strings.DetailCharactersFormat, chars, charsNoSpaces) });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailWordCount, Value = $"{words:N0}" });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailLineCount, Value = $"{lines:N0}" });
            if (analysis.HasComponents)
            {
                info.DetailsList.Add(new DetailProperty { Label = strings.DetailComponents, Value = analysis.SummaryText });
            }
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailPayloadSize, Value = $"{FileHelper.FormatFileSize(bytes)} ({bytes:N0} B)" });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailTextCategory, Value = item.Category.ToString() });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailTransformed, Value = item.HasTransformedText ? strings.DetailYesModified : strings.DetailNoOriginal });

            if (item.HasTransformedText && !string.IsNullOrEmpty(item.OriginalTextContent))
            {
                info.DetailsList.Add(new DetailProperty { Label = strings.DetailOriginalText, Value = item.OriginalTextContent, CopyValue = item.OriginalTextContent });
            }

            info.DetailsList.Add(new DetailProperty { Label = strings.DetailPinned, Value = item.IsPinned ? strings.DetailYesPinned : strings.DetailNoNotPinned });
            info.DetailsList.Add(new DetailProperty { Label = strings.DetailTimeCopied, Value = $"{item.FullTimestamp} ({item.TimeAgo})" });
        }

        private static string CalculateAspectRatio(int w, int h)
        {
            if (w <= 0 || h <= 0) return "1:1";
            int gcd = GCD(w, h);
            int rw = w / gcd;
            int rh = h / gcd;

            if (rw == 16 && rh == 9) return "16:9";
            if (rw == 4 && rh == 3) return "4:3";
            if (rw == 1 && rh == 1) return "1:1";
            if (rw == 21 && rh == 9) return "21:9";
            if (rw == 3 && rh == 2) return "3:2";

            double ratio = (double)w / h;
            return $"{ratio:0.##}:1 ({rw}:{rh})";
        }

        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return Math.Max(1, a);
        }

        private static void RgbToHsl(byte r, byte g, byte b, out double h, out double s, out double l)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            l = (max + min) / 2.0;

            if (Math.Abs(delta) < 0.00001)
            {
                h = 0;
                s = 0;
                return;
            }

            s = l > 0.5 ? delta / (2.0 - max - min) : delta / (max + min);

            if (Math.Abs(max - rd) < 0.00001)
            {
                h = ((gd - bd) / delta) + (gd < bd ? 6 : 0);
            }
            else if (Math.Abs(max - gd) < 0.00001)
            {
                h = ((bd - rd) / delta) + 2;
            }
            else
            {
                h = ((rd - gd) / delta) + 4;
            }

            h *= 60;
        }

        private static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            v = max;
            s = max == 0 ? 0 : delta / max;

            if (Math.Abs(delta) < 0.00001)
            {
                h = 0;
                return;
            }

            if (Math.Abs(max - rd) < 0.00001)
            {
                h = ((gd - bd) / delta) + (gd < bd ? 6 : 0);
            }
            else if (Math.Abs(max - gd) < 0.00001)
            {
                h = ((bd - rd) / delta) + 2;
            }
            else
            {
                h = ((rd - gd) / delta) + 4;
            }

            h *= 60;
        }
    }
}