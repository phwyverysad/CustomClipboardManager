using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CustomClipboardManager.Models;

namespace CustomClipboardManager.Services
{
    public class ClipboardDataDto
    {
        public bool IsWindowPinned { get; set; }
        public List<ClipboardItemDto> Items { get; set; } = new List<ClipboardItemDto>();
    }

    public class ClipboardItemDto
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public ClipboardContentType ContentType { get; set; }
        public SmartCategory Category { get; set; }
        public string? TextContent { get; set; }
        public bool IsPinned { get; set; }
    }

    public static class ClipboardDataService
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomClipboardManager");
        private static readonly string DataFile = Path.Combine(AppDataFolder, "data.json");

        public static void SaveData(bool isWindowPinned, IEnumerable<ClipboardItem> items)
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                var dto = new ClipboardDataDto
                {
                    IsWindowPinned = isWindowPinned,
                    Items = items
                        .Where(x => x.ContentType == ClipboardContentType.Text) // Save text only for now to keep JSON simple
                        .Select(x => new ClipboardItemDto
                        {
                            Id = x.Id,
                            Timestamp = x.Timestamp,
                            ContentType = x.ContentType,
                            Category = x.Category,
                            TextContent = x.TextContent,
                            IsPinned = x.IsPinned
                        }).ToList()
                };

                var json = JsonSerializer.Serialize(dto);
                File.WriteAllText(DataFile, json);
            }
            catch (Exception ex)
            {
                // Ignore save errors
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public static ClipboardDataDto LoadData()
        {
            try
            {
                if (File.Exists(DataFile))
                {
                    var json = File.ReadAllText(DataFile);
                    return JsonSerializer.Deserialize<ClipboardDataDto>(json) ?? new ClipboardDataDto();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return new ClipboardDataDto();
        }
    }
}
