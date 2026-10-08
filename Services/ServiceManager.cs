using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CustomClipboardManager.Services
{
    /// <summary>
    /// Manages clipboard capture state (Active / Paused) for the application.
    /// Runs purely as a standard standalone desktop executable (.exe) without requiring any Windows Service.
    /// </summary>
    public static class ServiceManager
    {
        private static bool _inAppCaptureEnabled = true;

        /// <summary>
        /// Determines whether clipboard capture is currently active in the application.
        /// </summary>
        public static bool IsServiceActive()
        {
            return _inAppCaptureEnabled;
        }

        /// <summary>
        /// Starts/Resumes in-app clipboard capture.
        /// </summary>
        public static Task<bool> StartServiceAsync()
        {
            _inAppCaptureEnabled = true;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Pauses in-app clipboard capture.
        /// </summary>
        public static Task<bool> StopServiceAsync()
        {
            _inAppCaptureEnabled = false;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Toggles between active and paused states.
        /// </summary>
        public static Task<bool> ToggleServiceAsync()
        {
            _inAppCaptureEnabled = !_inAppCaptureEnabled;
            return Task.FromResult(_inAppCaptureEnabled);
        }

        /// <summary>
        /// Synchronous cleanup called during exit if needed.
        /// </summary>
        public static void StopServiceSync(int timeoutMs = 1000)
        {
            _inAppCaptureEnabled = false;
        }

        /// <summary>
        /// Silently cleans up and deletes any legacy "CustomClipboardService" Windows Service
        /// leftover from older installations, so user systems run cleanly as a standalone .exe.
        /// </summary>
        public static void CleanupLegacyWindowsService()
        {
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        var psiStop = new ProcessStartInfo("sc.exe", "stop CustomClipboardService")
                        {
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            UseShellExecute = false
                        };
                        var pStop = Process.Start(psiStop);
                        pStop?.WaitForExit(2000);

                        var psiDelete = new ProcessStartInfo("sc.exe", "delete CustomClipboardService")
                        {
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            UseShellExecute = false
                        };
                        var pDelete = Process.Start(psiDelete);
                        pDelete?.WaitForExit(2000);
                    }
                    catch { }
                });
            }
            catch { }
        }
    }
}
