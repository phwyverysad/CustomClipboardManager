using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CustomClipboardManager.Core;
using CustomClipboardManager.Services;

namespace CustomClipboardManager
{
    public static class Program
    {
        private static Mutex? _sessionMutex;

        public static void LogException(string title, Exception? ex)
        {
            try
            {
                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CustomClipboardManager",
                    "crash.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {title}: {ex}\n");
            }
            catch { }
        }

        public static bool StartVisible { get; set; } = true;

        [STAThread]
        public static void Main(string[] args)
        {
            // Manual user launches (desktop shortcut, empty args, --show) MUST show the window immediately.
            // Silent background execution occurs ONLY when explicitly flagged by Windows boot.
            StartVisible = !IpcHelper.IsBackgroundLaunch(args);

            // 1. Single-instance check for desktop user session (using Local\ namespace so standard user permissions work)
            bool isTestMode = args != null && args.Any(a => a.StartsWith("--test", StringComparison.OrdinalIgnoreCase));
            if (!isTestMode)
            {
                bool isNewInstance = true;
                int sessionId = Process.GetCurrentProcess().SessionId;

                try
                {
                    string mutexName = $"Local\\CustomClipboardManager_UserSession_{sessionId}";
                    _sessionMutex = new Mutex(true, mutexName, out isNewInstance);
                }
                catch
                {
                    isNewInstance = true;
                }

                if (!isNewInstance)
                {
                    // Another instance is already running in this user session
                    // Signal the existing instance to display its window immediately across all IPC channels
                    IpcHelper.SignalShowExistingInstance(sessionId);
                    return;
                }
            }

            // 2. Silently clean up legacy CustomClipboardService Windows Service if leftover from older versions
            ServiceManager.CleanupLegacyWindowsService();

            // 3. Launch WPF Application immediately as standalone desktop executable
            try
            {
                var app = new App();
                app.InitializeComponent();
                app.DispatcherUnhandledException += (s, e) =>
                {
                    LogException("DispatcherUnhandledException", e.Exception);
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    LogException("AppDomain UnhandledException", e.ExceptionObject as Exception);
                };
                app.Run();
            }
            catch (Exception ex)
            {
                LogException("Exception in Main", ex);
            }
        }
    }
}
