using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using CustomClipboardManager.Models;

namespace CustomClipboardManager.Services
{
    public static class FileHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000; // 32x32
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static BitmapSource? GetFileIcon(string path, bool isDirectory)
        {
            try
            {
                var shinfo = new SHFILEINFO();
                uint flags = SHGFI_ICON | SHGFI_LARGEICON;

                bool exists = isDirectory ? Directory.Exists(path) : File.Exists(path);
                if (!exists)
                {
                    flags |= SHGFI_USEFILEATTRIBUTES;
                    uint attr = isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
                    SHGetFileInfo(path, attr, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
                }
                else
                {
                    SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
                }

                if (shinfo.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        var bitmap = Imaging.CreateBitmapSourceFromHIcon(
                            shinfo.hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bitmap.Freeze();
                        return bitmap;
                    }
                    finally
                    {
                        DestroyIcon(shinfo.hIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting icon for {path}: {ex.Message}");
            }

            return null;
        }

        public static string ShortenPath(string fullPath, int maxChars = 22)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return string.Empty;

            try
            {
                bool isDir = Directory.Exists(fullPath);
                string dir = isDir ? fullPath : (Path.GetDirectoryName(fullPath) ?? fullPath);

                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(userProfile) && dir.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
                {
                    dir = "~" + dir.Substring(userProfile.Length);
                }

                if (dir.Length <= maxChars)
                    return dir;

                // Split directories
                var parts = dir.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length <= 2)
                    return dir.Length > maxChars ? "..." + dir.Substring(dir.Length - maxChars + 3) : dir;

                string root = parts[0] + Path.DirectorySeparatorChar;
                string last = parts[^1];

                if (parts.Length >= 3)
                {
                    string secondLast = parts[^2];
                    string candidate = $"{root}...\\{secondLast}\\{last}";
                    if (candidate.Length <= maxChars)
                        return candidate;
                }

                string shortCandidate = $"{root}...\\{last}";
                if (shortCandidate.Length <= maxChars)
                    return shortCandidate;

                return "..." + dir.Substring(dir.Length - maxChars + 3);
            }
            catch
            {
                return fullPath.Length > maxChars ? "..." + fullPath.Substring(fullPath.Length - maxChars + 3) : fullPath;
            }
        }

        public static string FormatFileSize(long bytes)
        {
            if (bytes < 0) return "0 B";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{(bytes / 1024.0):0.#} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{(bytes / (1024.0 * 1024.0)):0.##} MB";
            return $"{(bytes / (1024.0 * 1024.0 * 1024.0)):0.##} GB";
        }

        public static void PopulateFileDetails(ClipboardItem item, IEnumerable<string> filePaths)
        {
            var filesList = filePaths?.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
            if (filesList == null || filesList.Count == 0) return;

            item.ContentType = ClipboardContentType.FileDropList;
            item.Category = SmartCategory.Files;
            item.FilePaths = filesList;
            item.FileCount = filesList.Count;

            var sc = new System.Collections.Specialized.StringCollection();
            sc.AddRange(filesList.ToArray());
            item.RawData = sc;

            string first = filesList[0];
            bool firstIsDir = Directory.Exists(first);
            item.FilePath = first;
            item.FileExtension = firstIsDir ? "FOLDER" : (Path.GetExtension(first).TrimStart('.').ToUpperInvariant());
            if (string.IsNullOrEmpty(item.FileExtension))
            {
                item.FileExtension = firstIsDir ? "FOLDER" : "FILE";
            }

            item.ShortenedPath = ShortenPath(first);
            item.FileIcon = GetFileIcon(first, firstIsDir);

            if (filesList.Count == 1)
            {
                item.FileName = firstIsDir ? Path.GetFileName(first.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) : Path.GetFileName(first);
                if (string.IsNullOrEmpty(item.FileName)) item.FileName = first;

                if (firstIsDir)
                {
                    item.FileSizeText = "Folder";
                    item.TextContent = $"[Folder] {item.FileName}\nPath: {first}";
                }
                else
                {
                    long size = 0;
                    try
                    {
                        var fi = new FileInfo(first);
                        if (fi.Exists) size = fi.Length;
                    }
                    catch { }

                    item.FileSizeText = FormatFileSize(size);
                    item.TextContent = $"{item.FileName} ({item.FileSizeText})\nPath: {first}";
                }
            }
            else
            {
                string firstFileName = Path.GetFileName(first);
                if (string.IsNullOrEmpty(firstFileName)) firstFileName = first;
                item.FileName = $"{firstFileName} (+{filesList.Count - 1} files)";

                long totalBytes = 0;
                int validFiles = 0;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"{filesList.Count} items selected:");

                foreach (var path in filesList)
                {
                    bool isDir = Directory.Exists(path);
                    if (isDir)
                    {
                        sb.AppendLine($"- [Folder] {Path.GetFileName(path)}");
                    }
                    else
                    {
                        long size = 0;
                        try
                        {
                            var fi = new FileInfo(path);
                            if (fi.Exists)
                            {
                                size = fi.Length;
                                totalBytes += size;
                                validFiles++;
                            }
                        }
                        catch { }
                        sb.AppendLine($"- {Path.GetFileName(path)} ({FormatFileSize(size)})");
                    }
                }

                if (validFiles > 0)
                {
                    item.FileSizeText = $"{FormatFileSize(totalBytes)}";
                }
                else
                {
                    item.FileSizeText = $"{filesList.Count} items";
                }

                item.TextContent = sb.ToString().TrimEnd();
            }
        }

        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".ico", ".tiff", ".tif", ".svg"
        };

        private static readonly HashSet<string> TextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".md", ".json", ".xml", ".csv", ".tsv", ".log", ".cs", ".xaml",
            ".js", ".ts", ".jsx", ".tsx", ".html", ".htm", ".css", ".scss", ".less",
            ".py", ".sql", ".ini", ".cfg", ".config", ".yaml", ".yml", ".sh", ".bat",
            ".cmd", ".ps1", ".c", ".cpp", ".h", ".hpp", ".java", ".php", ".rs", ".go",
            ".rb", ".swift", ".kt", ".dart", ".svg", ".reg", ".env", ".gitignore", ".props", ".targets"
        };

        public static bool IsImageFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            string ext = Path.GetExtension(path);
            return ImageExtensions.Contains(ext);
        }

        public static bool IsTextFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            string ext = Path.GetExtension(path);
            return TextExtensions.Contains(ext);
        }

        public static BitmapSource? LoadImageFromFile(string? path, int maxWidth = 1200)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

            try
            {
                var bi = new BitmapImage();
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = fs;
                if (maxWidth > 0)
                {
                    bi.DecodePixelWidth = maxWidth;
                }
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load image from {path}: {ex.Message}");
                return null;
            }
        }

        public static string? ReadTextFileSnippet(string? path, int maxChars = 32768)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs, System.Text.Encoding.UTF8, true);
                char[] buffer = new char[maxChars];
                int read = sr.ReadBlock(buffer, 0, maxChars);
                if (read > 0)
                {
                    string result = new string(buffer, 0, read);
                    if (!sr.EndOfStream)
                    {
                        result += "\n\n... [Preview truncated — file is larger] ...";
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                return $"(Unable to read text preview: {ex.Message})";
            }
            return null;
        }

        public static BitmapSource? GetLargeFileIcon(string path, bool isDirectory)
        {
            try
            {
                if (!isDirectory && File.Exists(path))
                {
                    using var sysIcon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                    if (sysIcon != null)
                    {
                        var bs = Imaging.CreateBitmapSourceFromHIcon(
                            sysIcon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bs.Freeze();
                        return bs;
                    }
                }
            }
            catch { }

            return GetFileIcon(path, isDirectory);
        }

        public static string GetFileTypeDescription(string path, bool isDirectory)
        {
            if (isDirectory) return "File Folder";

            try
            {
                var shinfo = new SHFILEINFO();
                SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), 0x000000400 /* SHGFI_TYPENAME */);
                if (!string.IsNullOrWhiteSpace(shinfo.szTypeName))
                {
                    // If UI language is NOT Thai, prevent Windows OS Thai shell strings from leaking
                    if (I18n.CurrentLanguage != "th" && Regex.IsMatch(shinfo.szTypeName, @"[\u0E00-\u0E7F]"))
                    {
                        string extName = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
                        return string.IsNullOrEmpty(extName) ? "File" : $"{extName} Document";
                    }
                    return shinfo.szTypeName;
                }
            }
            catch { }

            string ext = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
            return string.IsNullOrEmpty(ext) ? "File" : $"{ext} Document";
        }

        public static void OpenFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                if (Uri.TryCreate(path, UriKind.Absolute, out Uri? uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                    return;
                }

                if (File.Exists(path) || Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }

        public static void OpenInExplorer(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                if (File.Exists(path))
                {
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
                else if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }

        public static bool SaveBitmapSourceToFile(BitmapSource image, string destinationPath)
        {
            try
            {
                string? dir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string ext = Path.GetExtension(destinationPath).ToLowerInvariant();
                BitmapEncoder encoder = ext switch
                {
                    ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
                    ".bmp" => new BmpBitmapEncoder(),
                    _ => new PngBitmapEncoder()
                };

                using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, false);
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fs);
                return true;
            }
            catch { return false; }
        }
    }
}
