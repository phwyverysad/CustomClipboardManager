using System;
using Microsoft.Win32;

namespace CustomClipboardManager.Core
{
    /// <summary>
    /// Helper to check and toggle Windows built-in Clipboard History (Win+V).
    /// Location: HKCU\Software\Microsoft\Clipboard -> EnableClipboardHistory (DWORD: 1 or 0).
    /// </summary>
    public static class WindowsClipboardHelper
    {
        private const string RegistryPath = @"Software\Microsoft\Clipboard";
        private const string ValueName = "EnableClipboardHistory";

        /// <summary>
        /// Reads whether Windows built-in Clipboard History is enabled in the current user's profile.
        /// Defaults to true if the registry key/value does not exist yet.
        /// </summary>
        public static bool IsWindowsClipboardHistoryEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);
                if (key != null)
                {
                    object? val = key.GetValue(ValueName);
                    if (val is int intVal)
                    {
                        return intVal != 0;
                    }
                }
            }
            catch { }
            return true;
        }

        /// <summary>
        /// Sets whether Windows built-in Clipboard History is enabled (1) or disabled (0).
        /// </summary>
        public static bool SetWindowsClipboardHistoryEnabled(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryPath, true);
                if (key != null)
                {
                    key.SetValue(ValueName, enable ? 1 : 0, RegistryValueKind.DWord);
                    return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Synchronizes Windows Clipboard History state based on the current hotkey and optional user override.
        /// If user has an explicit preference, that preference is honored.
        /// If user preference is null (auto mode):
        /// - If hotkey is Win+V: automatically disable Windows Clipboard History to prevent clashes.
        /// - If hotkey is anything else: automatically enable Windows Clipboard History.
        /// Returns the resulting active state of Windows Clipboard History.
        /// </summary>
        public static bool SyncWithHotkey(HotkeyConfig config, bool? userExplicitOverride = null)
        {
            if (userExplicitOverride.HasValue)
            {
                SetWindowsClipboardHistoryEnabled(userExplicitOverride.Value);
                return userExplicitOverride.Value;
            }

            // Auto mode:
            if (config != null && config.IsWinV)
            {
                // Our shortcut is Win + V: disable Windows Clipboard History
                SetWindowsClipboardHistoryEnabled(false);
                return false;
            }
            else
            {
                // Our shortcut does not clash: re-enable Windows Clipboard History
                SetWindowsClipboardHistoryEnabled(true);
                return true;
            }
        }
    }
}
