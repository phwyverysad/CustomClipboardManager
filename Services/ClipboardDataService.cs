using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media.Imaging;
using CustomClipboardManager.Models;

namespace CustomClipboardManager.Services
{
    public class ClipboardDataDto
    {
        public bool IsWindowPinned { get; set; }
        public bool? IsDarkMode { get; set; } = null;
        public bool IsScreenCaptureProtectionEnabled { get; set; } = false;
        public bool IsEfficiencyModeEnabled { get; set; } = true;
        public bool? UserWindowsClipboardHistoryOverride { get; set; } = null;
        public List<ClipboardItemDto> Items { get; set; } = new List<ClipboardItemDto>();
    }

    public class ClipboardItemDto
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public ClipboardContentType ContentType { get; set; }
        public SmartCategory Category { get; set; }
        public string? TextContent { get; set; }
        public string? OriginalTextContent { get; set; }
        public string? ImageFileName { get; set; }
        public List<string>? FilePaths { get; set; }
        public bool IsPinned { get; set; }
    }

    public static class ClipboardDataService
    {
        private static string? _appDataFolder;
        public static string AppDataFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_appDataFolder))
                {
                    _appDataFolder = ResolveAppDataFolder();
                }
                return _appDataFolder;
            }
        }

        public static string ImagesFolder => Path.Combine(AppDataFolder, "Images");
        public static string DataFile => Path.Combine(AppDataFolder, "data.json");
        public static string BackupFile => Path.Combine(AppDataFolder, "data.json.bak");
        public static string BackupsFolder => Path.Combine(AppDataFolder, "Backups");
        private static readonly object FileLock = new object();

        private static string ResolveAppDataFolder()
        {
            try
            {
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrWhiteSpace(localApp) && !localApp.Contains("systemprofile", StringComparison.OrdinalIgnoreCase))
                {
                    string folder = Path.Combine(localApp, "CustomClipboardManager");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    return folder;
                }
            }
            catch { }

            try
            {
                string? userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
                if (!string.IsNullOrWhiteSpace(userProfile) && !userProfile.Contains("systemprofile", StringComparison.OrdinalIgnoreCase))
                {
                    string folder = Path.Combine(userProfile, "AppData", "Local", "CustomClipboardManager");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    return folder;
                }
            }
            catch { }

            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
                var localAppVal = key?.GetValue("Local AppData") as string;
                if (!string.IsNullOrWhiteSpace(localAppVal))
                {
                    localAppVal = Environment.ExpandEnvironmentVariables(localAppVal);
                    if (!string.IsNullOrWhiteSpace(localAppVal) && !localAppVal.Contains("systemprofile", StringComparison.OrdinalIgnoreCase))
                    {
                        string folder = Path.Combine(localAppVal, "CustomClipboardManager");
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                        return folder;
                    }
                }
            }
            catch { }

            string fallback = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(fallback))
            {
                fallback = AppDomain.CurrentDomain.BaseDirectory;
            }
            string finalFolder = Path.Combine(fallback, "CustomClipboardManager");
            try { if (!Directory.Exists(finalFolder)) Directory.CreateDirectory(finalFolder); } catch { }
            return finalFolder;
        }

        public static void SaveData(
            bool isWindowPinned, 
            bool isDarkMode, 
            IEnumerable<ClipboardItem> items,
            bool isScreenCaptureProtectionEnabled = false,
            bool isEfficiencyModeEnabled = true,
            bool? userWindowsClipboardHistoryOverride = null)
        {
            lock (FileLock)
            {
                try
                {
                    if (!Directory.Exists(AppDataFolder))
                    {
                        Directory.CreateDirectory(AppDataFolder);
                    }

                    if (!Directory.Exists(ImagesFolder))
                    {
                        Directory.CreateDirectory(ImagesFolder);
                    }

                    if (!Directory.Exists(BackupsFolder))
                    {
                        Directory.CreateDirectory(BackupsFolder);
                    }

                    // Safeguard existing data before overwriting: copy to data.json.bak
                    if (File.Exists(DataFile) && new FileInfo(DataFile).Length > 0)
                    {
                        try { File.Copy(DataFile, BackupFile, overwrite: true); } catch { }
                    }

                    var activeImageFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var itemDtos = new List<ClipboardItemDto>();

                    foreach (var item in items)
                    {
                        var dto = new ClipboardItemDto
                        {
                            Id = item.Id,
                            Timestamp = item.Timestamp,
                            ContentType = item.ContentType,
                            Category = item.Category,
                            TextContent = item.TextContent,
                            OriginalTextContent = item.OriginalTextContent,
                            IsPinned = item.IsPinned
                        };

                        if (item.ContentType == ClipboardContentType.Image && item.ImageContent != null)
                        {
                            string fileName = $"{item.Id}.png";
                            string fullImagePath = Path.Combine(ImagesFolder, fileName);
                            activeImageFiles.Add(fileName);

                            if (!File.Exists(fullImagePath))
                            {
                                try
                                {
                                    using var fs = new FileStream(fullImagePath, FileMode.Create, FileAccess.Write, FileShare.None);
                                    var encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(item.ImageContent));
                                    encoder.Save(fs);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to save image {fileName}: {ex.Message}");
                                }
                            }

                            dto.ImageFileName = fileName;
                        }
                        else if (item.ContentType == ClipboardContentType.FileDropList)
                        {
                            if (item.FilePaths != null && item.FilePaths.Count > 0)
                            {
                                dto.FilePaths = item.FilePaths;
                            }
                            else if (item.RawData is StringCollection sc)
                            {
                                dto.FilePaths = sc.Cast<string>().ToList();
                            }
                            else if (item.RawData is IEnumerable<string> paths)
                            {
                                dto.FilePaths = paths.ToList();
                            }
                        }

                        itemDtos.Add(dto);
                    }

                    var dataDto = new ClipboardDataDto
                    {
                        IsWindowPinned = isWindowPinned,
                        IsDarkMode = isDarkMode,
                        IsScreenCaptureProtectionEnabled = isScreenCaptureProtectionEnabled,
                        IsEfficiencyModeEnabled = isEfficiencyModeEnabled,
                        UserWindowsClipboardHistoryOverride = userWindowsClipboardHistoryOverride,
                        Items = itemDtos
                    };

                    // Clean up orphaned images that were removed or cleared
                    try
                    {
                        var existingFiles = Directory.GetFiles(ImagesFolder, "*.png");
                        foreach (var file in existingFiles)
                        {
                            string name = Path.GetFileName(file);
                            if (!activeImageFiles.Contains(name))
                            {
                                try { File.Delete(file); } catch { }
                            }
                        }
                    }
                    catch { }

                    // Atomic write: write to temp file then rename
                    string tempFile = Path.Combine(AppDataFolder, $"data_{Guid.NewGuid():N}.tmp");
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(dataDto, options);
                    File.WriteAllText(tempFile, json);
                    File.Move(tempFile, DataFile, overwrite: true);

                    // Keep safety backup
                    try { File.Copy(DataFile, BackupFile, overwrite: true); } catch { }

                    // Rotating timestamped backup (keep max 5 latest)
                    try
                    {
                        string rollingBackup = Path.Combine(BackupsFolder, $"data_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                        File.Copy(DataFile, rollingBackup, overwrite: true);

                        var oldBackups = Directory.GetFiles(BackupsFolder, "data_backup_*.json")
                            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                            .Skip(5);

                        foreach (var old in oldBackups)
                        {
                            try { File.Delete(old); } catch { }
                        }
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving clipboard data: {ex.Message}");
                }
            }
        }

        public static ClipboardDataDto LoadData()
        {
            lock (FileLock)
            {
                ClipboardDataDto? primaryDto = null;

                // 1. Try loading primary DataFile
                if (File.Exists(DataFile))
                {
                    try
                    {
                        var json = File.ReadAllText(DataFile);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            primaryDto = JsonSerializer.Deserialize<ClipboardDataDto>(json);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading primary data.json: {ex.Message}");
                    }
                }

                // 2. Fallback to BackupFile (data.json.bak) if primary missing or empty
                if (primaryDto == null && File.Exists(BackupFile))
                {
                    try
                    {
                        var json = File.ReadAllText(BackupFile);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            primaryDto = JsonSerializer.Deserialize<ClipboardDataDto>(json);
                        }
                    }
                    catch { }
                }

                if (primaryDto == null)
                {
                    primaryDto = new ClipboardDataDto();
                }

                if (primaryDto.Items == null)
                {
                    primaryDto.Items = new List<ClipboardItemDto>();
                }

                // 3. Historical Data Recovery & Smart Merge:
                // Scan BackupsFolder, BackupFile, and legacy installation locations
                // If any historical items exist from previous versions/updates, pull and merge them!
                bool mergedAny = false;
                var existingIds = new HashSet<Guid>(primaryDto.Items.Select(x => x.Id));
                var existingSignatures = new HashSet<string>(primaryDto.Items.Select(GetItemSignature));

                var candidateFiles = new List<string>();
                if (File.Exists(BackupFile)) candidateFiles.Add(BackupFile);

                if (Directory.Exists(BackupsFolder))
                {
                    candidateFiles.AddRange(Directory.GetFiles(BackupsFolder, "data_*.json")
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime));
                }

                // Legacy locations
                string progFilesLegacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Clipboard", "data.json");
                if (File.Exists(progFilesLegacy)) candidateFiles.Add(progFilesLegacy);
                string progFilesBak = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Clipboard", "data.json.bak");
                if (File.Exists(progFilesBak)) candidateFiles.Add(progFilesBak);
                string baseDirLegacy = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.json");
                if (File.Exists(baseDirLegacy) && !string.Equals(baseDirLegacy, DataFile, StringComparison.OrdinalIgnoreCase))
                {
                    candidateFiles.Add(baseDirLegacy);
                }
                string appDataRoaming = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CustomClipboardManager", "data.json");
                if (File.Exists(appDataRoaming)) candidateFiles.Add(appDataRoaming);

                foreach (var candidatePath in candidateFiles)
                {
                    if (string.Equals(candidatePath, DataFile, StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        if (File.Exists(candidatePath) && new FileInfo(candidatePath).Length > 0)
                        {
                            var candJson = File.ReadAllText(candidatePath);
                            if (!string.IsNullOrWhiteSpace(candJson))
                            {
                                var candDto = JsonSerializer.Deserialize<ClipboardDataDto>(candJson);
                                if (candDto?.Items != null && candDto.Items.Count > 0)
                                {
                                    string candImgDir = Path.Combine(Path.GetDirectoryName(candidatePath)!, "Images");
                                    if (!Directory.Exists(candImgDir)) candImgDir = ImagesFolder;

                                    foreach (var candItem in candDto.Items)
                                    {
                                        string sig = GetItemSignature(candItem);
                                        if (!existingIds.Contains(candItem.Id) && !existingSignatures.Contains(sig))
                                        {
                                            existingIds.Add(candItem.Id);
                                            existingSignatures.Add(sig);

                                            if (candItem.ContentType == ClipboardContentType.Text &&
                                                !string.IsNullOrEmpty(candItem.TextContent) &&
                                                ClipboardItem.IsPureEmoji(candItem.TextContent))
                                            {
                                                candItem.Category = SmartCategory.Emoji;
                                            }

                                            primaryDto.Items.Add(candItem);
                                            mergedAny = true;

                                            if (!string.IsNullOrEmpty(candItem.ImageFileName) && Directory.Exists(candImgDir))
                                            {
                                                string srcImg = Path.Combine(candImgDir, candItem.ImageFileName);
                                                string destImg = Path.Combine(ImagesFolder, candItem.ImageFileName);
                                                if (File.Exists(srcImg) && !File.Exists(destImg))
                                                {
                                                    try
                                                    {
                                                        if (!Directory.Exists(ImagesFolder)) Directory.CreateDirectory(ImagesFolder);
                                                        File.Copy(srcImg, destImg, true);
                                                    }
                                                    catch { }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Check existing items for any emojis that should be categorized as Emoji
                foreach (var item in primaryDto.Items)
                {
                    if (item.ContentType == ClipboardContentType.Text &&
                        !string.IsNullOrEmpty(item.TextContent) &&
                        item.Category == SmartCategory.Text &&
                        ClipboardItem.IsPureEmoji(item.TextContent))
                    {
                        item.Category = SmartCategory.Emoji;
                        mergedAny = true;
                    }
                }

                if (mergedAny)
                {
                    primaryDto.Items = primaryDto.Items.OrderByDescending(x => x.Timestamp).ToList();
                    try
                    {
                        if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);
                        var opt = new JsonSerializerOptions { WriteIndented = true };
                        var updatedJson = JsonSerializer.Serialize(primaryDto, opt);
                        File.WriteAllText(DataFile, updatedJson);
                        File.Copy(DataFile, BackupFile, overwrite: true);
                    }
                    catch { }
                }
                else if (File.Exists(DataFile))
                {
                    try { File.Copy(DataFile, BackupFile, overwrite: true); } catch { }
                }

                return primaryDto;
            }
        }

        private static string GetItemSignature(ClipboardItemDto item)
        {
            if (item.ContentType == ClipboardContentType.Text)
            {
                return $"text:{item.TextContent?.Trim()}";
            }
            if (item.ContentType == ClipboardContentType.FileDropList && item.FilePaths != null)
            {
                return $"files:{string.Join("|", item.FilePaths)}";
            }
            if (item.ContentType == ClipboardContentType.Image)
            {
                return $"img:{item.ImageFileName ?? item.Id.ToString()}";
            }
            return item.Id.ToString();
        }

        public static BitmapSource? LoadCachedImage(string? imageFileName)
        {
            if (string.IsNullOrEmpty(imageFileName)) return null;

            try
            {
                string fullPath = Path.Combine(ImagesFolder, imageFileName);
                if (File.Exists(fullPath))
                {
                    var bi = new BitmapImage();
                    using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = fs;
                        bi.EndInit();
                    }
                    bi.Freeze();
                    return bi;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load cached image {imageFileName}: {ex.Message}");
            }
            return null;
        }
    }
}
