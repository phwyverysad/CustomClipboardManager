using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CustomClipboardManager.Core
{
    public static class AutoPasteService
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int dwProcessId);
        private const int ASFW_ANY = -1;

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_SHIFT = 0x10;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_MENU = 0x12; // Alt
        private const byte VK_LWIN = 0x5B;
        private const byte VK_RWIN = 0x5C;
        private const byte VK_V = 0x56;

        public static bool ActivateTargetWindow(IntPtr targetHwnd, IntPtr focusControlHwnd = default)
        {
            if (targetHwnd == IntPtr.Zero || !IsWindow(targetHwnd)) return false;

            try
            {
                // Grant permission to target process to become foreground
                uint targetThreadId = GetWindowThreadProcessId(targetHwnd, out uint targetProcId);
                if (targetProcId != 0)
                {
                    AllowSetForegroundWindow((int)targetProcId);
                }
                else
                {
                    AllowSetForegroundWindow(ASFW_ANY);
                }

                if (IsIconic(targetHwnd))
                {
                    ShowWindow(targetHwnd, SW_RESTORE);
                }

                // Seamlessly bring the target window back to foreground without disturbing active text selection or carets.
                // DO NOT call AttachThreadInput, simulate Alt keys, or call SetFocus on child controls,
                // as those destroy text selection/highlights in other applications.
                BringWindowToTop(targetHwnd);
                SetForegroundWindow(targetHwnd);
                SetActiveWindow(targetHwnd);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Task PasteAsync() => PasteAsync(IntPtr.Zero, IntPtr.Zero);
        public static Task PasteAsync(IntPtr targetHwnd) => PasteAsync(targetHwnd, IntPtr.Zero);

        public static async Task PasteAsync(IntPtr targetHwnd, IntPtr focusControlHwnd)
        {
            // 1. Activate and ensure target window has gained foreground
            if (targetHwnd != IntPtr.Zero && IsWindow(targetHwnd))
            {
                ActivateTargetWindow(targetHwnd, focusControlHwnd);

                for (int i = 0; i < 6; i++)
                {
                    if (GetForegroundWindow() == targetHwnd) break;
                    await Task.Delay(20);
                }
            }
            else
            {
                await Task.Delay(50);
            }

            // Stabilization pause to let target control's message queue accept focus and position caret
            await Task.Delay(75);

            // 2. Release any modifier keys (Shift/Alt/Ctrl/Win) that might still be logically down
            ReleaseModifiersIfNeeded();

            // 3. Dispatch Ctrl+V keystrokes via SendInput with hardware scan codes
            ushort scanCtrl = (ushort)MapVirtualKey(VK_CONTROL, 0);
            ushort scanV = (ushort)MapVirtualKey(VK_V, 0);

            INPUT[] inputs = new INPUT[4];

            // Press Ctrl
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT { wVk = VK_CONTROL, wScan = scanCtrl, dwFlags = 0, time = 0, dwExtraInfo = IntPtr.Zero }
                }
            };

            // Press V
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT { wVk = VK_V, wScan = scanV, dwFlags = 0, time = 0, dwExtraInfo = IntPtr.Zero }
                }
            };

            // Release V
            inputs[2] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT { wVk = VK_V, wScan = scanV, dwFlags = KEYEVENTF_KEYUP, time = 0, dwExtraInfo = IntPtr.Zero }
                }
            };

            // Release Ctrl
            inputs[3] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT { wVk = VK_CONTROL, wScan = scanCtrl, dwFlags = KEYEVENTF_KEYUP, time = 0, dwExtraInfo = IntPtr.Zero }
                }
            };

            int structSize = Marshal.SizeOf(typeof(INPUT));
            uint sent = SendInput((uint)inputs.Length, inputs, structSize);

            // 4. Fallback to keybd_event if SendInput fails or returns 0
            if (sent == 0)
            {
                byte bScanCtrl = (byte)scanCtrl;
                byte bScanV = (byte)scanV;
                keybd_event(VK_CONTROL, bScanCtrl, 0, UIntPtr.Zero);
                keybd_event(VK_V, bScanV, 0, UIntPtr.Zero);
                keybd_event(VK_V, bScanV, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(VK_CONTROL, bScanCtrl, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        public static void ReleaseModifiersIfNeeded()
        {
            // If Shift is down, release it so Ctrl+Shift+V doesn't result in shifted paste
            if ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0)
            {
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }

            // If Alt is down, release it
            if ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0)
            {
                keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }

            // If Win key is down, release it
            if ((GetAsyncKeyState(VK_LWIN) & 0x8000) != 0)
            {
                keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            if ((GetAsyncKeyState(VK_RWIN) & 0x8000) != 0)
            {
                keybd_event(VK_RWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }

            // If Ctrl is down from earlier shortcut, release it so the new sequence is clean
            if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
            {
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }
    }
}
