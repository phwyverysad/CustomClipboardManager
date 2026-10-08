using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CustomClipboardManager.Core
{
    public class ClipboardMonitor : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        private HwndSource? _hwndSource;
        private Window _window;

        public event EventHandler? ClipboardChanged;

        public ClipboardMonitor(Window window)
        {
            _window = window;
            var helper = new WindowInteropHelper(_window);
            if (helper.Handle != IntPtr.Zero)
            {
                Attach(helper.Handle);
            }
            else
            {
                _window.SourceInitialized += OnSourceInitialized;
            }
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var helper = new WindowInteropHelper(_window);
            if (helper.Handle != IntPtr.Zero)
            {
                Attach(helper.Handle);
            }
        }

        private void Attach(IntPtr hwnd)
        {
            if (_hwndSource != null || hwnd == IntPtr.Zero) return;
            try
            {
                _hwndSource = HwndSource.FromHwnd(hwnd);
                if (_hwndSource != null)
                {
                    _hwndSource.AddHook(HwndHook);
                    AddClipboardFormatListener(hwnd);
                }
            }
            catch { }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                ClipboardChanged?.Invoke(this, EventArgs.Empty);
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_hwndSource != null)
            {
                RemoveClipboardFormatListener(_hwndSource.Handle);
                _hwndSource.RemoveHook(HwndHook);
                _hwndSource = null;
            }
        }
    }
}
