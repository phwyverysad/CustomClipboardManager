using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace CustomClipboardManager.Core
{
    public class HotkeyConfig
    {
        public bool Control { get; set; } = true;
        public bool Shift { get; set; } = true;
        public bool Alt { get; set; } = false;
        public bool Windows { get; set; } = false;
        public int VirtualKey { get; set; } = 0x56; // VK_V
        public string KeyName { get; set; } = "V";
        public string DisplayText { get; set; } = "Ctrl + Shift + V";

        public static HotkeyConfig Default => new HotkeyConfig
        {
            Control = true,
            Shift = true,
            Alt = false,
            Windows = false,
            VirtualKey = 0x56,
            KeyName = "V",
            DisplayText = "Ctrl + Shift + V"
        };

        public HotkeyConfig Clone()
        {
            return new HotkeyConfig
            {
                Control = this.Control,
                Shift = this.Shift,
                Alt = this.Alt,
                Windows = this.Windows,
                VirtualKey = this.VirtualKey,
                KeyName = this.KeyName,
                DisplayText = this.DisplayText
            };
        }

        public bool IsWinV => Windows && !Control && !Alt && !Shift && (VirtualKey == 0x56 || string.Equals(KeyName, "V", StringComparison.OrdinalIgnoreCase));

        public string BuildDisplayText()
        {
            var parts = new List<string>();
            if (Control) parts.Add("Ctrl");
            if (Alt) parts.Add("Alt");
            if (Shift) parts.Add("Shift");
            if (Windows) parts.Add("Win");
            parts.Add(string.IsNullOrWhiteSpace(KeyName) ? GetFriendlyKeyName(VirtualKey) : KeyName);
            return string.Join(" + ", parts);
        }

        public bool Matches(int vk, bool ctrl, bool shift, bool alt, bool win)
        {
            return (vk == VirtualKey) &&
                   (ctrl == Control) &&
                   (shift == Shift) &&
                   (alt == Alt) &&
                   (win == Windows);
        }

        public static string GetFriendlyKeyName(int vk)
        {
            if (vk >= 0x41 && vk <= 0x5A) // A-Z
                return ((char)vk).ToString();
            if (vk >= 0x30 && vk <= 0x39) // 0-9
                return ((char)vk).ToString();
            if (vk >= 0x70 && vk <= 0x7B) // F1-F12
                return "F" + (vk - 0x70 + 1);

            switch (vk)
            {
                case 0xC0: return "`";
                case 0x20: return "Space";
                case 0x09: return "Tab";
                case 0x0D: return "Enter";
                case 0x1B: return "Esc";
                case 0x21: return "PageUp";
                case 0x22: return "PageDown";
                case 0x23: return "End";
                case 0x24: return "Home";
                case 0x25: return "Left";
                case 0x26: return "Up";
                case 0x27: return "Right";
                case 0x28: return "Down";
                case 0x2D: return "Insert";
                case 0x2E: return "Delete";
                case 0xBA: return ";";
                case 0xBB: return "=";
                case 0xBC: return ",";
                case 0xBD: return "-";
                case 0xBE: return ".";
                case 0xBF: return "/";
                case 0xDB: return "[";
                case 0xDC: return "\\";
                case 0xDD: return "]";
                case 0xDE: return "'";
                default:
                    return $"Key_0x{vk:X2}";
            }
        }

        public static string ToJson(HotkeyConfig config)
        {
            if (config == null) config = Default;
            config.DisplayText = config.BuildDisplayText();
            return "{\n" +
                   $"  \"control\": {(config.Control ? "true" : "false")},\n" +
                   $"  \"shift\": {(config.Shift ? "true" : "false")},\n" +
                   $"  \"alt\": {(config.Alt ? "true" : "false")},\n" +
                   $"  \"windows\": {(config.Windows ? "true" : "false")},\n" +
                   $"  \"virtualKey\": {config.VirtualKey},\n" +
                   $"  \"keyName\": \"{config.KeyName}\",\n" +
                   $"  \"displayText\": \"{config.DisplayText}\"\n" +
                   "}";
        }

        public static HotkeyConfig FromJson(string json)
        {
            var config = Default.Clone();
            if (string.IsNullOrWhiteSpace(json)) return config;

            try
            {
                config.Control = Regex.IsMatch(json, "\"control\"\\s*:\\s*true", RegexOptions.IgnoreCase);
                config.Shift = Regex.IsMatch(json, "\"shift\"\\s*:\\s*true", RegexOptions.IgnoreCase);
                config.Alt = Regex.IsMatch(json, "\"alt\"\\s*:\\s*true", RegexOptions.IgnoreCase);
                config.Windows = Regex.IsMatch(json, "\"windows\"\\s*:\\s*true", RegexOptions.IgnoreCase);

                var matchVk = Regex.Match(json, "\"virtualKey\"\\s*:\\s*(\\d+)", RegexOptions.IgnoreCase);
                if (matchVk.Success && int.TryParse(matchVk.Groups[1].Value, out int vk))
                {
                    config.VirtualKey = vk;
                }

                var matchKey = Regex.Match(json, "\"keyName\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
                if (matchKey.Success)
                {
                    config.KeyName = matchKey.Groups[1].Value;
                }

                config.DisplayText = config.BuildDisplayText();
            }
            catch { }

            return config;
        }
    }

    public static class HotkeyManager
    {
        private static readonly object _syncLock = new object();
        private static HotkeyConfig? _currentConfig;

        public static event Action<HotkeyConfig>? HotkeyChanged;

        public static HotkeyConfig Current
        {
            get
            {
                if (_currentConfig == null)
                {
                    lock (_syncLock)
                    {
                        if (_currentConfig == null)
                        {
                            _currentConfig = Load();
                        }
                    }
                }
                return _currentConfig;
            }
        }

        private static string? TryReadAllTextWithRetry(string filePath, int retries = 3)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var sr = new StreamReader(fs);
                        return sr.ReadToEnd();
                    }
                    return null;
                }
                catch (IOException) when (i < retries - 1)
                {
                    System.Threading.Thread.Sleep(25);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        public static HotkeyConfig Load(string? targetDir = null)
        {
            lock (_syncLock)
            {
                // 1. Try LocalAppData
                try
                {
                    string localAppDataDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "CustomClipboardManager");
                    string jsonPath = Path.Combine(localAppDataDir, "hotkey.json");
                    string? content = TryReadAllTextWithRetry(jsonPath);
                    if (!string.IsNullOrEmpty(content))
                    {
                        var config = HotkeyConfig.FromJson(content);
                        if (config != null) return config;
                    }
                }
                catch { }

                // 2. Try App BaseDirectory or targetDir
                try
                {
                    string baseDir = !string.IsNullOrEmpty(targetDir) ? targetDir : AppDomain.CurrentDomain.BaseDirectory;
                    string jsonPath = Path.Combine(baseDir, "hotkey.json");
                    string? content = TryReadAllTextWithRetry(jsonPath);
                    if (!string.IsNullOrEmpty(content))
                    {
                        var config = HotkeyConfig.FromJson(content);
                        if (config != null) return config;
                    }
                }
                catch { }

                // 3. Try Registry
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\CustomClipboardManager"))
                    {
                        if (key != null)
                        {
                            string? json = key.GetValue("HotkeyConfig") as string;
                            if (!string.IsNullOrEmpty(json))
                            {
                                var config = HotkeyConfig.FromJson(json);
                                if (config != null) return config;
                            }
                        }
                    }
                }
                catch { }

                return HotkeyConfig.Default;
            }
        }

        public static bool Save(HotkeyConfig config, string? targetDir = null)
        {
            if (config == null) return false;
            lock (_syncLock)
            {
                config.DisplayText = config.BuildDisplayText();
                _currentConfig = config;

                string json = HotkeyConfig.ToJson(config);
                bool saved = false;

                // 1. Save to LocalAppData
                try
                {
                    string localAppDataDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "CustomClipboardManager");
                    Directory.CreateDirectory(localAppDataDir);
                    string jsonPath = Path.Combine(localAppDataDir, "hotkey.json");
                    File.WriteAllText(jsonPath, json);
                    saved = true;
                }
                catch { }

                // 2. Save to targetDir / BaseDirectory if writable
                try
                {
                    string baseDir = !string.IsNullOrEmpty(targetDir) ? targetDir : AppDomain.CurrentDomain.BaseDirectory;
                    if (Directory.Exists(baseDir))
                    {
                        string jsonPath = Path.Combine(baseDir, "hotkey.json");
                        File.WriteAllText(jsonPath, json);
                        saved = true;
                    }
                }
                catch { }

                // 3. Save to Registry
                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\CustomClipboardManager"))
                    {
                        if (key != null)
                        {
                            key.SetValue("HotkeyConfig", json);
                            key.SetValue("HotkeyDisplayText", config.DisplayText);
                            key.SetValue("HotkeyVirtualKey", config.VirtualKey);
                            saved = true;
                        }
                    }
                }
                catch { }

                try
                {
                    HotkeyChanged?.Invoke(config);
                }
                catch { }

                return saved;
            }
        }

        public static HotkeyConfig GetPreset(string presetName)
        {
            switch (presetName?.ToUpperInvariant())
            {
                case "ALT+V":
                    return new HotkeyConfig { Control = false, Alt = true, Shift = false, Windows = false, VirtualKey = 0x56, KeyName = "V", DisplayText = "Alt + V" };
                case "CTRL+OEM3":
                case "CTRL+`":
                    return new HotkeyConfig { Control = true, Alt = false, Shift = false, Windows = false, VirtualKey = 0xC0, KeyName = "`", DisplayText = "Ctrl + `" };
                case "WIN+V":
                    return new HotkeyConfig { Control = false, Alt = false, Shift = false, Windows = true, VirtualKey = 0x56, KeyName = "V", DisplayText = "Win + V" };
                case "CTRL+SHIFT+Z":
                    return new HotkeyConfig { Control = true, Alt = false, Shift = true, Windows = false, VirtualKey = 0x5A, KeyName = "Z", DisplayText = "Ctrl + Shift + Z" };
                case "CTRL+ALT+V":
                    return new HotkeyConfig { Control = true, Alt = true, Shift = false, Windows = false, VirtualKey = 0x56, KeyName = "V", DisplayText = "Ctrl + Alt + V" };
                case "F8":
                    return new HotkeyConfig { Control = false, Alt = false, Shift = false, Windows = false, VirtualKey = 0x77, KeyName = "F8", DisplayText = "F8" };
                case "CTRL+SHIFT+V":
                default:
                    return HotkeyConfig.Default;
            }
        }
    }
}
