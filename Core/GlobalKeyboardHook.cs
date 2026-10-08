using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CustomClipboardManager.Core
{
    public class GlobalKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private HotkeyConfig _config = HotkeyConfig.Default;
        private bool _isHotKeyDown = false;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event EventHandler? OnHotkeyPressed;
        public Func<int, bool>? InterceptKeyDown;
        public bool IsHookActive => _hookID != IntPtr.Zero;

        public GlobalKeyboardHook(HotkeyConfig? config = null)
        {
            if (config != null) _config = config;
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        public void UpdateHotkey(HotkeyConfig config)
        {
            if (config != null)
            {
                _config = config;
                _isHotKeyDown = false;
            }
        }

        public static bool IsMatch(HotkeyConfig? config, int vkCode, bool ctrl, bool shift, bool alt, bool win)
        {
            if (config == null) return false;
            return (vkCode == config.VirtualKey) &&
                   (ctrl == config.Control) &&
                   (shift == config.Shift) &&
                   (alt == config.Alt) &&
                   (win == config.Windows);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            IntPtr hMod = IntPtr.Zero;
            try
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule? curModule = curProcess.MainModule)
                {
                    string moduleName = curModule?.ModuleName ?? string.Empty;
                    if (!string.IsNullOrEmpty(moduleName))
                    {
                        hMod = GetModuleHandle(moduleName);
                    }
                }
            }
            catch { }

            if (hMod == IntPtr.Zero)
            {
                try
                {
                    hMod = GetModuleHandle(null);
                }
                catch { }
            }

            IntPtr hook = SetWindowsHookEx(WH_KEYBOARD_LL, proc, hMod, 0);
            if (hook == IntPtr.Zero && hMod != IntPtr.Zero)
            {
                hook = SetWindowsHookEx(WH_KEYBOARD_LL, proc, IntPtr.Zero, 0);
            }
            return hook;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool isTargetKey = (vkCode == _config.VirtualKey);

                if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    if (isTargetKey || vkCode == 0x11 || vkCode == 0x10 || vkCode == 0x12 || vkCode == 0x5B || vkCode == 0x5C)
                    {
                        _isHotKeyDown = false;
                    }
                }
                else if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    if (InterceptKeyDown != null)
                    {
                        try
                        {
                            if (InterceptKeyDown(vkCode))
                            {
                                return (IntPtr)1;
                            }
                        }
                        catch { }
                    }

                    if (isTargetKey)
                    {
                        bool isCtrlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0;  // VK_CONTROL
                        bool isShiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT
                        bool isAltPressed = (GetAsyncKeyState(0x12) & 0x8000) != 0;   // VK_MENU
                        bool isWinPressed = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;

                        bool match = (isCtrlPressed == _config.Control) &&
                                     (isShiftPressed == _config.Shift) &&
                                     (isAltPressed == _config.Alt) &&
                                     (isWinPressed == _config.Windows);

                        if (match)
                        {
                            if (_isHotKeyDown)
                            {
                                // Ignore repeat keydowns while user holds the hotkey down
                                return (IntPtr)1;
                            }
                            _isHotKeyDown = true;

                            // Fire event asynchronously so we never block the low-level hook thread
                            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                            {
                                try
                                {
                                    OnHotkeyPressed?.Invoke(this, EventArgs.Empty);
                                }
                                catch { }
                            });

                            // Swallow the hotkey press
                            return (IntPtr)1;
                        }

                        _isHotKeyDown = false;
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }
    }
}
