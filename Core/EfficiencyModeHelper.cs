using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CustomClipboardManager.Core
{
    /// <summary>
    /// Helper to place the application in Windows Efficiency Mode (EcoQoS) and reduce memory/CPU footprint when idle.
    /// Uses ProcessPowerThrottling via SetProcessInformation, BelowNormal priority, and working set trimming.
    /// </summary>
    public static class EfficiencyModeHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_POWER_THROTTLING_STATE
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessInformation(
            IntPtr hProcess,
            int ProcessInformationClass,
            ref PROCESS_POWER_THROTTLING_STATE ProcessInformation,
            uint ProcessInformationSize);

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        private const int ProcessPowerThrottling = 4;
        private const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
        private const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 0x1;
        private const uint PROCESS_POWER_THROTTLING_IGNORE_TIMER_RESOLUTION = 0x4;

        private static bool _isThrottled = false;

        public static bool IsEfficiencyModeActive => _isThrottled;

        /// <summary>
        /// Enables Windows Efficiency Mode (EcoQoS) and sets priority to BelowNormal.
        /// Call when the application is idle in background.
        /// </summary>
        public static void EnableEfficiencyMode()
        {
            try
            {
                var hProcess = Process.GetCurrentProcess().Handle;
                var state = new PROCESS_POWER_THROTTLING_STATE
                {
                    Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                    ControlMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED | PROCESS_POWER_THROTTLING_IGNORE_TIMER_RESOLUTION,
                    StateMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED | PROCESS_POWER_THROTTLING_IGNORE_TIMER_RESOLUTION
                };

                SetProcessInformation(hProcess, ProcessPowerThrottling, ref state, (uint)Marshal.SizeOf(state));
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                _isThrottled = true;
            }
            catch { }
        }

        /// <summary>
        /// Disables Efficiency Mode and restores priority to Normal for silky-smooth responsiveness.
        /// Call when the user opens the clipboard window or performs active operations.
        /// </summary>
        public static void DisableEfficiencyMode()
        {
            try
            {
                var hProcess = Process.GetCurrentProcess().Handle;
                var state = new PROCESS_POWER_THROTTLING_STATE
                {
                    Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                    ControlMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED | PROCESS_POWER_THROTTLING_IGNORE_TIMER_RESOLUTION,
                    StateMask = 0
                };

                SetProcessInformation(hProcess, ProcessPowerThrottling, ref state, (uint)Marshal.SizeOf(state));
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                _isThrottled = false;
            }
            catch { }
        }

        /// <summary>
        /// Trims unused memory from the process working set when idle.
        /// </summary>
        public static void TrimMemory()
        {
            try
            {
                GC.Collect(2, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true);
                EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            }
            catch { }
        }
    }
}
