using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace CustomClipboardManager.Core
{
    /// <summary>
    /// Provides robust, multi-channel Inter-Process Communication (IPC)
    /// to guarantee that invocation signals across privilege/UAC boundaries
    /// always wake up and display the primary application window.
    /// </summary>
    public static class IpcHelper
    {
        public const string SHOW_WINDOW_MESSAGE_NAME = "CustomClipboardManager_ShowMessage_v1";
        public const string SHOW_EVENT_BASE_NAME = "CustomClipboardManager_ShowEvent_";
        public const string WAKE_SIGNAL_FILE_NAME = "wake.signal";

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ChangeWindowMessageFilter(uint message, uint dwFlag);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, IntPtr pChangeFilterStruct);

        public const uint MSGFLT_ADD = 1;
        public const uint MSGFLT_ALLOW = 1;
        public static readonly IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;

        private static uint _wmShowMessage = 0;
        public static uint WmShowMessage
        {
            get
            {
                if (_wmShowMessage == 0)
                {
                    _wmShowMessage = RegisterWindowMessage(SHOW_WINDOW_MESSAGE_NAME);
                }
                return _wmShowMessage;
            }
        }

        public static EventWaitHandle CreateNamedEvent(string eventName, out bool createdNew)
        {
            try
            {
                var security = new EventWaitHandleSecurity();
                var rule = new EventWaitHandleAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    EventWaitHandleRights.FullControl,
                    AccessControlType.Allow);
                security.AddAccessRule(rule);

                return EventWaitHandleAcl.Create(
                    false,
                    EventResetMode.AutoReset,
                    eventName,
                    out createdNew,
                    security);
            }
            catch
            {
                return new EventWaitHandle(false, EventResetMode.AutoReset, eventName, out createdNew);
            }
        }

        /// <summary>
        /// Creates or opens an EventWaitHandle with permissive DACL granting Everyone (WorldSid)
        /// modify/synchronize access so that non-elevated or cross-integrity processes can signal it.
        /// </summary>
        public static EventWaitHandle CreateSessionShowEvent(int sessionId, out bool createdNew)
        {
            string eventName = $"Local\\{SHOW_EVENT_BASE_NAME}{sessionId}";
            return CreateNamedEvent(eventName, out createdNew);
        }

        /// <summary>
        /// Fires all three IPC signaling channels (Win32 message broadcast, named EventWaitHandle,
        /// and local file touch) to guarantee the primary instance is awakened.
        /// </summary>
        public static void SignalShowExistingInstance(int sessionId)
        {
            // Channel 1: Win32 Message Broadcast (bypasses UIPI via ChangeWindowMessageFilter)
            try
            {
                uint msg = WmShowMessage;
                if (msg != 0)
                {
                    PostMessage(HWND_BROADCAST, msg, IntPtr.Zero, IntPtr.Zero);
                }
            }
            catch { }

            // Channel 2: Named EventWaitHandle with Open Existing
            try
            {
                string eventName = $"Local\\{SHOW_EVENT_BASE_NAME}{sessionId}";
                using var waitHandle = EventWaitHandle.OpenExisting(eventName);
                waitHandle.Set();
            }
            catch { }

            // Channel 3: LocalAppData file wake signal (monitored by FileSystemWatcher)
            try
            {
                TouchWakeSignalFile();
            }
            catch { }
        }

        /// <summary>
        /// Touches or creates the wake.signal file in %LocalAppData%\CustomClipboardManager
        /// </summary>
        public static void TouchWakeSignalFile()
        {
            try
            {
                string signalDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CustomClipboardManager");
                if (!Directory.Exists(signalDir))
                {
                    Directory.CreateDirectory(signalDir);
                }
                string signalFile = Path.Combine(signalDir, WAKE_SIGNAL_FILE_NAME);
                File.WriteAllText(signalFile, DateTime.UtcNow.Ticks.ToString());
            }
            catch { }
        }

        /// <summary>
        /// Determines if the given command line arguments signify a silent background boot.
        /// </summary>
        public static bool IsBackgroundLaunch(string[]? args)
        {
            if (args == null || args.Length == 0) return false;
            return Array.Exists(args, a =>
                a.Equals("--background", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--startup", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--autostart", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--silent", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--service", StringComparison.OrdinalIgnoreCase));
        }
    }
}
