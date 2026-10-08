using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using CustomClipboardManager.Core;
using CustomClipboardManager.Models;
using CustomClipboardManager.ViewModels;
using CustomClipboardManager.Services;
using System.Windows.Media.Imaging;
using DragEventArgs = System.Windows.DragEventArgs;
using DataObject = System.Windows.DataObject;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using QueryContinueDragEventHandler = System.Windows.QueryContinueDragEventHandler;

namespace CustomClipboardManager
{
    public partial class MainWindow : Window
    {
        private GlobalKeyboardHook? _keyboardHook;
        private ClipboardMonitor? _clipboardMonitor;
        private MainViewModel _viewModel;
        
        // Anti-loop flag
        private bool _isPasting = false;
        private DateTime _lastClipboardEventTime = DateTime.MinValue;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint WINEVENT_OUTOFCONTEXT = 0;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(out bool pfEnabled);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Win32Point lpPoint);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const int SW_SHOWNOACTIVATE = 4;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);

        private const uint WDA_NONE = 0x00000000;
        private const uint WDA_MONITOR = 0x00000001;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);


        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int HOTKEY_ID = 9000;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_V = 0x56;
        private const int WM_HOTKEY = 0x0312;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private const int WM_SETFOCUS = 0x0007;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;

        private bool _isHotKeyRegistered = false;
        private System.Windows.Interop.HwndSource? _windowHwndSource;

        private WinEventDelegate? _foregroundDelegate;
        private IntPtr _foregroundHook = IntPtr.Zero;
        private IntPtr _previousHwnd = IntPtr.Zero;
        private IntPtr _previousFocusHwnd = IntPtr.Zero;
        private EventWaitHandle? _showEvent;
        private bool _isDisposed = false;
        private bool _isHwndInitialized = false;
        private System.IO.FileSystemWatcher? _hotkeyWatcher = null;
        private HotkeyConfig _hotkeyConfig = HotkeyConfig.Default;
        private IntPtr _windowHwnd = IntPtr.Zero;
        private bool _isRecordingAppHotkey = false;
        private bool _isUpdatingCheckboxes = false;
        private DateTime _lastHotKeyTime = DateTime.MinValue;
        private DateTime _windowShownTime = DateTime.MinValue;
        private bool _isHiding = false;
        private int _hideAnimationId = 0;
        private System.Windows.Threading.DispatcherTimer? _outsideClickTimer;

        public static readonly DependencyProperty IsItemPressedProperty =
            DependencyProperty.RegisterAttached(
                "IsItemPressed",
                typeof(bool),
                typeof(MainWindow),
                new FrameworkPropertyMetadata(false));

        public static bool GetIsItemPressed(DependencyObject obj) => (bool)obj.GetValue(IsItemPressedProperty);
        public static void SetIsItemPressed(DependencyObject obj, bool value) => obj.SetValue(IsItemPressedProperty, value);

        private System.Windows.Controls.ListViewItem? _pressedListViewItem = null;

        private void ClearPressedListViewItem()
        {
            if (_pressedListViewItem != null)
            {
                _pressedListViewItem.SetValue(IsItemPressedProperty, false);
                _pressedListViewItem = null;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            _viewModel.GetTargetWindowHwnd = () => _previousHwnd;
            _viewModel.GetTargetFocusHwnd = () => _previousFocusHwnd;
            _viewModel.RequestApplyScreenCaptureProtection = (enable) => ApplyScreenCaptureProtection(enable);
            _viewModel.RequestPreparePaste = () =>
            {
                _isPasting = true;
                ClearPressedListViewItem();
                _dragTargetItem = null;
                _dragStartPoint = null;
                _isDragging = false;
            };
            _viewModel.RequestClose = () =>
            {
                _isPasting = true; // prevent clipboard monitor from catching our own paste
                ClearPressedListViewItem();
                _dragTargetItem = null;
                _dragStartPoint = null;
                _isDragging = false;
                ResetPreviewImmediate();
                ResetSearchAndScrollToLatest();
                
                // First restore target focus while our window is still active
                RestoreFocusToPreviousWindow();

                if (!_viewModel.IsWindowPinned)
                {
                    _isHiding = true;
                    this.Hide(); // Hide instantly after target window receives focus
                    _viewModel.StopTimeAgoTimer();
                    ScheduleIdleEfficiencyMode();
                }
            };

            _viewModel.ShowToastNotification = () =>
            {
                ShowToast();
            };

            _viewModel.RequestConfirmClearAll = () =>
            {
                ShowClearConfirm();
            };

            _viewModel.RequestShowPreview = (info) =>
            {
                ShowPreviewModal();
            };

            _viewModel.RequestHidePreview = () =>
            {
                HidePreviewModal();
            };

            _viewModel.RequestSavePreviewImage = (bitmap) =>
            {
                SavePreviewImageToFile(bitmap);
            };

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsWindowPinned))
                {
                    if (_viewModel.IsWindowPinned)
                    {
                        _outsideClickTimer?.Stop();
                    }
                    else if (this.IsVisible)
                    {
                        _outsideClickTimer?.Start();
                    }
                }
            };

            this.DataContext = _viewModel;

            // Completely hide icon from Windows System Tray per user request
            // InitializeSystemTrayIcon();

            // Set up IPC show event listener for instant wake-up from secondary instances or installer
            try
            {
                int sessionId = Process.GetCurrentProcess().SessionId;
                _showEvent = IpcHelper.CreateSessionShowEvent(sessionId, out _);
                Task.Run(() =>
                {
                    while (!_isDisposed)
                    {
                        try
                        {
                            if (_showEvent.WaitOne())
                            {
                                if (_isDisposed) break;
                                this.Dispatcher.BeginInvoke(new Action(() => ShowAtCursor(forceShow: true)));
                            }
                        }
                        catch { break; }
                    }
                });
            }
            catch { }

            // React to hotkey configuration updates dynamically
            HotkeyManager.HotkeyChanged += (newConfig) =>
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _hotkeyConfig = newConfig;
                    IntPtr hwnd = _windowHwnd != IntPtr.Zero ? _windowHwnd : new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        ApplyRegisteredHotkey(hwnd);
                    }
                }));
            };

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            this.Deactivated += MainWindow_Deactivated;
            this.SourceInitialized += MainWindow_SourceInitialized;
            this.IsVisibleChanged += MainWindow_IsVisibleChanged;
            
            this.MouseEnter += Window_MouseEnter;
            this.MouseLeave += Window_MouseLeave;

            _clipboardMonitor = new ClipboardMonitor(this);
            _clipboardMonitor.ClipboardChanged += ClipboardMonitor_ClipboardChanged;

            // Setup configuration & wake signal file watcher for multi-process sync
            try
            {
                string configDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CustomClipboardManager");
                if (!System.IO.Directory.Exists(configDir)) System.IO.Directory.CreateDirectory(configDir);

                _hotkeyWatcher = new System.IO.FileSystemWatcher(configDir)
                {
                    NotifyFilter = System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                _hotkeyWatcher.Changed += (s, e) => OnConfigFileChanged(e.Name);
                _hotkeyWatcher.Created += (s, e) => OnConfigFileChanged(e.Name);
            }
            catch { }

            // CRITICAL: Force synchronous HWND creation immediately so global hotkeys and message hooks are live on startup
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            IntPtr hwnd = helper.EnsureHandle();
            InitializeHwndComponents(hwnd);
            InitializeOutsideClickMonitor();
            this.SizeChanged += (s, e) => UpdateRoundedCorners();
        }

        private void InitializeOutsideClickMonitor()
        {
            _outsideClickTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Input)
            {
                Interval = TimeSpan.FromMilliseconds(40)
            };
            _outsideClickTimer.Tick += (s, e) =>
            {
                if (!this.IsVisible || _isHiding) return;
                if (_viewModel != null && _viewModel.IsWindowPinned) return;
                if (_isDragging || _isMovingWindow) return;
                if ((DateTime.UtcNow - _windowShownTime).TotalMilliseconds < 350) return;

                // Check mouse buttons (VK_LBUTTON = 0x01, VK_RBUTTON = 0x02)
                short lButton = (short)(GetAsyncKeyState(0x01) & 0x8000);
                short rButton = (short)(GetAsyncKeyState(0x02) & 0x8000);
                if (lButton != 0 || rButton != 0)
                {
                    var mousePt = new Win32Point();
                    GetCursorPos(ref mousePt);

                    try
                    {
                        var topLeft = this.PointToScreen(new System.Windows.Point(0, 0));
                        var bottomRight = this.PointToScreen(new System.Windows.Point(this.ActualWidth, this.ActualHeight));

                        bool isInside = (mousePt.X >= topLeft.X && mousePt.X <= bottomRight.X &&
                                         mousePt.Y >= topLeft.Y && mousePt.Y <= bottomRight.Y);

                        if (!isInside)
                        {
                            HideWindowAnimated();
                        }
                    }
                    catch { }
                }
            };
        }

        private void OnConfigFileChanged(string? fileName)
        {
            if (string.Equals(fileName, "hotkey.json", StringComparison.OrdinalIgnoreCase))
            {
                ReloadHotkeyFromDisk();
            }
            else if (string.Equals(fileName, IpcHelper.WAKE_SIGNAL_FILE_NAME, StringComparison.OrdinalIgnoreCase))
            {
                this.Dispatcher.BeginInvoke(new Action(() => ShowAtCursor(forceShow: true)));
            }
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;

            // Apply WS_EX_TOOLWINDOW, WS_EX_NOACTIVATE, and WS_EX_TOPMOST extended styles so the clipboard manager behaves as a pure on-screen HUD/overlay:
            // 1. Excluded completely from Alt+Tab and taskbar (pure overlay, not a standalone program)
            // 2. Never steals focus or activates when clicked, preserving background text selection and carets
            // 3. Stays cleanly floating on top without interfering with other windows
            try
            {
                int exStyle = GetWindowLong32(hwnd, GWL_EXSTYLE);
                SetWindowLong32(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST);
            }
            catch { }

            InitializeHwndComponents(hwnd);
        }

        private void InitializeHwndComponents(IntPtr hwnd)
        {
            if (_isHwndInitialized || hwnd == IntPtr.Zero) return;
            _isHwndInitialized = true;
            _windowHwnd = hwnd;

            // Enable native smooth DWM rounding on Windows 11
            try
            {
                if (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000)
                {
                    int cornerPreference = DWMWCP_ROUND; // Native rounded corners on Windows 11
                    DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
                }
            }
            catch { }

            UpdateRoundedCorners();
            ApplyScreenCaptureProtection(_viewModel?.IsScreenCaptureProtectionEnabled ?? false);

            try
            {
                _foregroundDelegate = new WinEventDelegate(WinEventProc);
                _foregroundHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _foregroundDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
            }
            catch { }

            try
            {
                _windowHwndSource = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
                _windowHwndSource?.AddHook(HwndMessageHook);

                // Allow custom IPC wake message through UIPI from any integrity level
                if (IpcHelper.WmShowMessage != 0)
                {
                    IpcHelper.ChangeWindowMessageFilter(IpcHelper.WmShowMessage, IpcHelper.MSGFLT_ADD);
                    IpcHelper.ChangeWindowMessageFilterEx(hwnd, IpcHelper.WmShowMessage, IpcHelper.MSGFLT_ALLOW, IntPtr.Zero);
                }
            }
            catch { }

            _hotkeyConfig = HotkeyManager.Load();
            ApplyRegisteredHotkey(hwnd);
        }

        private void ReloadHotkeyFromDisk()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    _hotkeyConfig = HotkeyManager.Load();
                    IntPtr hwnd = _windowHwnd != IntPtr.Zero ? _windowHwnd : new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        ApplyRegisteredHotkey(hwnd);
                    }
                }
                catch { }
            }));
        }

        private void ApplyRegisteredHotkey(IntPtr hwnd)
        {
            if (hwnd != IntPtr.Zero)
            {
                _windowHwnd = hwnd;
            }

            uint fsModifiers = MOD_NOREPEAT;
            if (_hotkeyConfig.Control) fsModifiers |= MOD_CONTROL;
            if (_hotkeyConfig.Alt) fsModifiers |= MOD_ALT;
            if (_hotkeyConfig.Shift) fsModifiers |= MOD_SHIFT;
            if (_hotkeyConfig.Windows) fsModifiers |= MOD_WIN;

            try
            {
                UnregisterHotKey(hwnd, HOTKEY_ID);
                _isHotKeyRegistered = RegisterHotKey(hwnd, HOTKEY_ID, fsModifiers, (uint)_hotkeyConfig.VirtualKey);
            }
            catch { }

            if (_keyboardHook == null)
            {
                _keyboardHook = new GlobalKeyboardHook(_hotkeyConfig);
                _keyboardHook.OnHotkeyPressed += KeyboardHook_OnHotkeyPressed;
                _keyboardHook.InterceptKeyDown = HandleGlobalKeyDownWhenOverlayVisible;
            }
            else
            {
                _keyboardHook.UpdateHotkey(_hotkeyConfig);
                _keyboardHook.InterceptKeyDown = HandleGlobalKeyDownWhenOverlayVisible;
            }

            _viewModel?.SyncWindowsClipboardWithHotkey(_hotkeyConfig);
            ApplyScreenCaptureProtection(_viewModel?.IsScreenCaptureProtectionEnabled ?? false);
        }

        private void TriggerHotKey()
        {
            try
            {
                if ((DateTime.UtcNow - _lastHotKeyTime).TotalMilliseconds < 300)
                {
                    return;
                }
                _lastHotKeyTime = DateTime.UtcNow;

                // Synchronously capture active foreground window AND child focus control before any dispatching.
                // MUST NOT access WPF Window properties directly here because this method can be called from a background ThreadPool thread!
                IntPtr ourHwnd = _windowHwnd;
                IntPtr fg = GetForegroundWindow();
                if (fg != IntPtr.Zero && (ourHwnd == IntPtr.Zero || fg != ourHwnd))
                {
                    _previousHwnd = fg;
                    uint threadId = GetWindowThreadProcessId(fg, out _);
                    if (threadId != 0)
                    {
                        var guiInfo = new GUITHREADINFO();
                        guiInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<GUITHREADINFO>();
                        if (GetGUIThreadInfo(threadId, ref guiInfo))
                        {
                            _previousFocusHwnd = guiInfo.hwndFocus != IntPtr.Zero ? guiInfo.hwndFocus : guiInfo.hwndCaret;
                        }
                    }
                }

                if (this.Dispatcher.CheckAccess())
                {
                    ShowAtCursor(forceShow: false);
                }
                else
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            ShowAtCursor(forceShow: false);
                        }
                        catch (Exception ex)
                        {
                            Program.LogException("ShowAtCursor", ex);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Program.LogException("TriggerHotKey", ex);
            }
        }

        private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                // When moving/dragging the window or clicking items, do not activate the window,
                // so active text selections and carets in background applications remain completely undisturbed.
                // However, when interacting with PreviewGrid (e.g. selecting text or clicking modal controls)
                // or when SearchBox is activating, allow activation so text selection works naturally.
                var previewGrid = (FrameworkElement?)this.FindName("PreviewGrid");
                bool isPreviewActive = previewGrid != null && previewGrid.Visibility == Visibility.Visible;
                if (_isMovingWindow || (!_isSearchBoxActivating && !isPreviewActive))
                {
                    handled = true;
                    return (IntPtr)MA_NOACTIVATE;
                }
            }
            else if (msg == WM_SETFOCUS)
            {
                // If keyboard focus was somehow directed to this window while not typing in SearchBox or preview modal,
                // do not steal focus from target application (avoid cross-process SetFocus which can deadlock message pumps)
                var previewGrid = (FrameworkElement?)this.FindName("PreviewGrid");
                bool isPreviewActive = previewGrid != null && previewGrid.Visibility == Visibility.Visible;
                if (!_isSearchBoxActivating && !isPreviewActive && _previousHwnd != IntPtr.Zero && _previousHwnd != hwnd)
                {
                    handled = true;
                    return IntPtr.Zero;
                }
            }
            else if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                handled = true;
                TriggerHotKey();
            }
            else if (IpcHelper.WmShowMessage != 0 && (uint)msg == IpcHelper.WmShowMessage)
            {
                handled = true;
                this.Dispatcher.BeginInvoke(new Action(() => ShowAtCursor(forceShow: true)));
            }
            else if (msg == WM_SETTINGCHANGE || msg == WM_DWMCOLORIZATIONCOLORCHANGED)
            {
                _viewModel.ApplyTheme(_viewModel.IsDarkMode);
            }
            return IntPtr.Zero;
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            IntPtr ourHwnd = _windowHwnd != IntPtr.Zero ? _windowHwnd : new System.Windows.Interop.WindowInteropHelper(this).Handle;

            // If the new foreground window is NOT our window, save it.
            if (hwnd != ourHwnd && hwnd != IntPtr.Zero)
            {
                var sb = new System.Text.StringBuilder(256);
                GetClassName(hwnd, sb, sb.Capacity);
                string cls = sb.ToString();

                if (cls != "Shell_TrayWnd" && cls != "Shell_SecondaryTrayWnd" &&
                    cls != "Progman" && cls != "WorkerW" && cls != "Xaml_WindowedPopupClass" &&
                    cls != "TaskListThumbnailWnd")
                {
                    uint procId = 0;
                    GetWindowThreadProcessId(hwnd, out procId);
                    if (procId != (uint)Process.GetCurrentProcess().Id)
                    {
                        if (!this.IsVisible)
                        {
                            _previousHwnd = hwnd;
                            uint threadId = GetWindowThreadProcessId(hwnd, IntPtr.Zero);
                            if (threadId != 0)
                            {
                                var guiInfo = new GUITHREADINFO();
                                guiInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<GUITHREADINFO>();
                                if (GetGUIThreadInfo(threadId, ref guiInfo))
                                {
                                    _previousFocusHwnd = guiInfo.hwndFocus != IntPtr.Zero ? guiInfo.hwndFocus : guiInfo.hwndCaret;
                                }
                            }
                        }
                    }
                }
                
                // Auto-hide ONLY if the foreground window switched to a completely different application
                // (neither our window NOR the target window _previousHwnd we opened against)
                if (hwnd != ourHwnd && hwnd != _previousHwnd)
                {
                    // Never auto-hide while the user is actively dragging an item or moving the window
                    if (_isDragging || _isMovingWindow) return;

                    // Guard: If the window just opened within the last 350ms, do not auto-hide due to focus transition artifacts
                    if ((DateTime.UtcNow - _windowShownTime).TotalMilliseconds < 350) return;

                    // If our window is currently visible, not pinned, and user switched to a third application, hide it.
                    if (this.IsVisible && !_viewModel.IsWindowPinned)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (!_isDragging && !_isMovingWindow && (DateTime.UtcNow - _windowShownTime).TotalMilliseconds >= 350)
                            {
                                HideWindowAnimated();
                            }
                        }));
                    }
                }
            }
        }

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            this.SetResourceReference(Window.BackgroundProperty, "AppBackgroundBrush");
            UpdateRoundedCorners();
            ResetPreviewImmediate();
            ResetSearchAndScrollToLatest();
            ApplyScreenCaptureProtection(_viewModel.IsScreenCaptureProtectionEnabled);
            if (Program.StartVisible)
            {
                ShowAtCursor(forceShow: true);
            }
            else
            {
                // Hide window on startup
                this.Hide();
                _viewModel.StopTimeAgoTimer();
                ScheduleIdleEfficiencyMode();
            }
        }

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                ResetSearchAndScrollToLatest();
            }
        }

        private void MainWindow_Deactivated(object? sender, EventArgs e)
        {
            ClearPressedListViewItem();
            // Never auto-hide while the user is actively dragging an item or moving the window
            if (_isDragging || _isMovingWindow) return;

            // Guard: If the window just opened in the last 350ms, do not auto-hide
            if ((DateTime.UtcNow - _windowShownTime).TotalMilliseconds < 350) return;

            // Auto hide when clicking outside, unless pinned
            if (!_viewModel.IsWindowPinned)
            {
                HideWindowAnimated();
            }
        }

        private void Window_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // If user explicitly clicked the SearchBox to search, allow keyboard focus
            if (e.NewFocus == SearchBox && _isSearchBoxActivating)
            {
                return;
            }

            // If user is recording a custom hotkey in settings dialog, allow keyboard focus
            if (_isRecordingAppHotkey)
            {
                return;
            }

            // If SearchBox is already active and focus is moving within SearchBox (or its children), allow it
            if (SearchBox.IsKeyboardFocusWithin && (e.NewFocus == SearchBox || (e.NewFocus is DependencyObject d && IsDescendantOf(d, SearchBox))))
            {
                return;
            }

            // If focus is within PreviewGrid (such as the text preview box, so the user can select and copy text), allow it!
            var previewGrid = (FrameworkElement?)this.FindName("PreviewGrid");
            if (previewGrid != null && previewGrid.Visibility == Visibility.Visible &&
                e.NewFocus is DependencyObject previewDep && IsDescendantOf(previewDep, previewGrid))
            {
                return;
            }

            // Block all other focus requests inside this overlay window!
            // Setting e.Handled = true prevents WPF KeyboardDevice from calling Win32 SetFocus(),
            // ensuring the background app (Notepad, Word, browser, etc.) NEVER receives WM_KILLFOCUS,
            // and its blinking caret and text selection remain 100% active and undisturbed.
            e.Handled = true;
        }

        private static bool IsDescendantOf(DependencyObject node, DependencyObject target)
        {
            DependencyObject? current = node;
            while (current != null)
            {
                if (current == target) return true;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }
            return false;
        }

        private void KeyboardHook_OnHotkeyPressed(object? sender, EventArgs e)
        {
            try
            {
                TriggerHotKey();
            }
            catch (Exception ex)
            {
                Program.LogException("KeyboardHook_OnHotkeyPressed", ex);
            }
        }

        private void CalculateSmartWindowPosition(IntPtr targetHwnd, out double finalLeft, out double finalTop)
        {
            var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
            double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

            double winWidth = double.IsNaN(this.Width) || this.Width <= 0 ? 430.0 : this.Width;
            double winHeight = double.IsNaN(this.Height) || this.Height <= 0 ? 595.0 : this.Height;
            const double screenPadding = 16.0;

            // 1. Attempt to locate active text caret in the target foreground thread
            if (targetHwnd != IntPtr.Zero)
            {
                uint threadId = GetWindowThreadProcessId(targetHwnd, IntPtr.Zero);
                if (threadId != 0)
                {
                    var guiInfo = new GUITHREADINFO();
                    guiInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<GUITHREADINFO>();
                    if (GetGUIThreadInfo(threadId, ref guiInfo))
                    {
                        IntPtr caretHwnd = guiInfo.hwndCaret != IntPtr.Zero ? guiInfo.hwndCaret : guiInfo.hwndFocus;
                        if (caretHwnd != IntPtr.Zero)
                        {
                            var rc = guiInfo.rcCaret;
                            if (rc.Right > rc.Left || rc.Bottom > rc.Top)
                            {
                                var ptTL = new Win32Point { X = rc.Left, Y = rc.Top };
                                var ptBR = new Win32Point { X = rc.Right, Y = rc.Bottom };
                                if (ClientToScreen(caretHwnd, ref ptTL) && ClientToScreen(caretHwnd, ref ptBR))
                                {
                                    var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(ptTL.X, ptTL.Y));
                                    if (screen != null && screen.Bounds.Contains(ptTL.X, ptTL.Y))
                                    {
                                        var workArea = screen.WorkingArea;
                                        double waLeft = workArea.Left / scaleX;
                                        double waTop = workArea.Top / scaleY;
                                        double waRight = workArea.Right / scaleX;
                                        double waBottom = workArea.Bottom / scaleY;

                                        double caretLeft = ptTL.X / scaleX;
                                        double caretTop = ptTL.Y / scaleY;
                                        double caretBottom = ptBR.Y / scaleY;
                                        double caretHeight = Math.Max(18.0, caretBottom - caretTop);

                                        const double caretGap = 8.0;
                                        // Preferred: directly below the caret line
                                        double desiredTop = caretTop + caretHeight + caretGap;
                                        double desiredLeft = caretLeft;

                                        // If overflowing the bottom work area, flip above the caret line
                                        if (desiredTop + winHeight > waBottom - screenPadding)
                                        {
                                            double aboveTop = caretTop - winHeight - caretGap;
                                            if (aboveTop >= waTop + screenPadding)
                                            {
                                                desiredTop = aboveTop;
                                            }
                                            else
                                            {
                                                desiredTop = waBottom - screenPadding - winHeight;
                                            }
                                        }

                                        // Ensure horizontal bounds with screenPadding
                                        if (desiredLeft + winWidth > waRight - screenPadding)
                                        {
                                            desiredLeft = waRight - screenPadding - winWidth;
                                        }
                                        if (desiredLeft < waLeft + screenPadding)
                                        {
                                            desiredLeft = waLeft + screenPadding;
                                        }

                                        // Ensure vertical bounds
                                        if (desiredTop < waTop + screenPadding)
                                        {
                                            desiredTop = waTop + screenPadding;
                                        }
                                        if (desiredTop + winHeight > waBottom - screenPadding)
                                        {
                                            desiredTop = waBottom - screenPadding - winHeight;
                                        }

                                        finalLeft = desiredLeft;
                                        finalTop = desiredTop;
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 2. Fallback: Position relative to the mouse cursor without obscuring it
            var mousePt = GetMousePosition();
            var mouseScreen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)mousePt.X, (int)mousePt.Y));
            var mouseWorkArea = mouseScreen.WorkingArea;

            double mWaLeft = mouseWorkArea.Left / scaleX;
            double mWaTop = mouseWorkArea.Top / scaleY;
            double mWaRight = mouseWorkArea.Right / scaleX;
            double mWaBottom = mouseWorkArea.Bottom / scaleY;

            double mouseX = mousePt.X / scaleX;
            double mouseY = mousePt.Y / scaleY;

            const double cursorOffsetX = 14.0;
            const double cursorOffsetY = 18.0;

            // Preferred: below and to the right of the cursor pointer
            double calcLeft = mouseX + cursorOffsetX;
            double calcTop = mouseY + cursorOffsetY;

            // If overflowing the right edge, flip to the left of the cursor
            if (calcLeft + winWidth > mWaRight - screenPadding)
            {
                double flippedLeft = mouseX - winWidth - cursorOffsetX;
                if (flippedLeft >= mWaLeft + screenPadding)
                {
                    calcLeft = flippedLeft;
                }
                else
                {
                    calcLeft = mWaRight - screenPadding - winWidth;
                }
            }
            if (calcLeft < mWaLeft + screenPadding)
            {
                calcLeft = mWaLeft + screenPadding;
            }

            // If overflowing the bottom edge, flip to above the cursor
            if (calcTop + winHeight > mWaBottom - screenPadding)
            {
                double flippedTop = mouseY - winHeight - cursorOffsetY;
                if (flippedTop >= mWaTop + screenPadding)
                {
                    calcTop = flippedTop;
                }
                else
                {
                    calcTop = mWaBottom - screenPadding - winHeight;
                }
            }
            if (calcTop < mWaTop + screenPadding)
            {
                calcTop = mWaTop + screenPadding;
            }
            if (calcTop + winHeight > mWaBottom - screenPadding)
            {
                calcTop = mWaBottom - screenPadding - winHeight;
            }

            finalLeft = calcLeft;
            finalTop = calcTop;
        }

        public void ShowAtCursor(bool forceShow = false)
        {
            // Toggle dismiss ONLY when triggered by hotkey; NEVER dismiss on IPC wake-up or direct launch!
            if (!forceShow && this.IsVisible && MainBorder.Opacity > 0.5 && !_isHiding)
            {
                // Only dismiss if the window has been active for at least 450ms
                if ((DateTime.UtcNow - _windowShownTime).TotalMilliseconds > 450)
                {
                    HideWindowAnimated();
                }
                return;
            }

            IntPtr ourHwnd = _windowHwnd != IntPtr.Zero ? _windowHwnd : new System.Windows.Interop.WindowInteropHelper(this).Handle;

            // Capture the currently active window before we show ours (never overwrite with our own window!)
            IntPtr currentFg = GetForegroundWindow();
            if (currentFg != IntPtr.Zero && currentFg != ourHwnd)
            {
                _previousHwnd = currentFg;
            }

            // Cancel any in-flight hide animation so the window never disappears unexpectedly
            _isHiding = false;
            _hideAnimationId++;
            ResetPreviewImmediate();
            var confirmGrid = (FrameworkElement?)this.FindName("ConfirmGrid");
            if (confirmGrid != null)
            {
                confirmGrid.BeginAnimation(UIElement.OpacityProperty, null);
                confirmGrid.Visibility = Visibility.Collapsed;
                confirmGrid.Opacity = 0;
            }
            var settingsGrid = (FrameworkElement?)this.FindName("HotkeySettingsGrid");
            if (settingsGrid != null)
            {
                settingsGrid.BeginAnimation(UIElement.OpacityProperty, null);
                settingsGrid.Visibility = Visibility.Collapsed;
                settingsGrid.Opacity = 0;
            }
            _isRecordingAppHotkey = false;

            MainBorder.BeginAnimation(UIElement.OpacityProperty, null);
            if (MainBorder.RenderTransform is System.Windows.Media.TranslateTransform tt)
            {
                tt.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null);
            }

            // If window is pinned and already visible, keep its pinned position
            if (!(_viewModel.IsWindowPinned && this.IsVisible))
            {
                // Calculate intelligent non-obtrusive window position
                CalculateSmartWindowPosition(_previousHwnd, out double desiredLeft, out double desiredTop);
                this.Left = desiredLeft;
                this.Top = desiredTop;
            }

            ResetSearchAndScrollToLatest();

            this.SetResourceReference(Window.BackgroundProperty, "AppBackgroundBrush");
            if (this.WindowState != WindowState.Normal)
            {
                this.WindowState = WindowState.Normal;
            }

            _windowShownTime = DateTime.UtcNow;
            _isPasting = false;

            // Show as a pure non-activating on-screen GUI overlay without taking focus away from target window:
            // 1. Target application (Notepad, Word, Chrome, etc.) remains foreground window with active text selection and caret 100% intact.
            // 2. Display with SW_SHOWNOACTIVATE and SWP_NOACTIVATE.
            // Cancel idle efficiency sleep and restore full performance
            _efficiencyCts?.Cancel();
            EfficiencyModeHelper.DisableEfficiencyMode();
            _viewModel.StartTimeAgoTimer();
            ApplyScreenCaptureProtection(_viewModel.IsScreenCaptureProtectionEnabled);

            if (ourHwnd != IntPtr.Zero)
            {
                ShowWindowAsync(ourHwnd, SW_SHOWNOACTIVATE);
                SetWindowPos(ourHwnd, HWND_TOPMOST, 0, 0, 0, 0, 
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            this.Show();
            ShowWindowAnimated();
            if (!_viewModel.IsWindowPinned)
            {
                _outsideClickTimer?.Start();
            }
        }

        private void RestoreFocusToPreviousWindow()
        {
            IntPtr ourHwnd = _windowHwnd != IntPtr.Zero ? _windowHwnd : new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (_previousHwnd != IntPtr.Zero && _previousHwnd != ourHwnd && GetForegroundWindow() != _previousHwnd)
            {
                AutoPasteService.ActivateTargetWindow(_previousHwnd);
            }
        }

        private System.Threading.CancellationTokenSource? _efficiencyCts;

        private void ScheduleIdleEfficiencyMode()
        {
            if (!_viewModel.IsEfficiencyModeEnabled) return;
            try
            {
                _efficiencyCts?.Cancel();
                _efficiencyCts?.Dispose();
                _efficiencyCts = new System.Threading.CancellationTokenSource();
                var token = _efficiencyCts.Token;

                Task.Delay(5000, token).ContinueWith(t =>
                {
                    if (!t.IsCanceled && !this.IsVisible && _viewModel.IsEfficiencyModeEnabled)
                    {
                        EfficiencyModeHelper.EnableEfficiencyMode();
                    }
                }, TaskScheduler.Default);
            }
            catch { }
        }

        public bool ApplyScreenCaptureProtection(bool enable)
        {
            try
            {
                IntPtr hwnd = _windowHwnd != IntPtr.Zero ? _windowHwnd : new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return false;

                // WDA_EXCLUDEFROMCAPTURE = 0x11 (Windows 10 version 2004+ and Windows 11 completely excludes window from capture/screenshots)
                // If it fails on older Windows builds, fallback to WDA_MONITOR = 0x01
                uint affinity = enable ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE;
                bool ok = SetWindowDisplayAffinity(hwnd, affinity);
                if (!ok && enable)
                {
                    ok = SetWindowDisplayAffinity(hwnd, WDA_MONITOR);
                }
                return ok;
            }
            catch (Exception ex)
            {
                Program.LogException("ApplyScreenCaptureProtection", ex);
                return false;
            }
        }

        public uint GetCurrentDisplayAffinity()
        {
            try
            {
                IntPtr hwnd = _windowHwnd != IntPtr.Zero ? _windowHwnd : new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return 0;
                if (GetWindowDisplayAffinity(hwnd, out uint affinity))
                {
                    return affinity;
                }
            }
            catch { }
            return 0;
        }

        private void UpdateRoundedCorners()
        {
            try
            {
                IntPtr hwnd = _windowHwnd != IntPtr.Zero ? _windowHwnd : new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return;

                if (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000)
                {
                    int cornerPreference = DWMWCP_ROUND;
                    DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
                }
                else
                {
                    var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
                    double w = this.ActualWidth > 0 ? this.ActualWidth : (double.IsNaN(this.Width) ? 430 : this.Width);
                    double h = this.ActualHeight > 0 ? this.ActualHeight : (double.IsNaN(this.Height) ? 595 : this.Height);
                    int pixelWidth = (int)Math.Round(w * (dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0));
                    int pixelHeight = (int)Math.Round(h * (dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0));
                    int radius = (int)Math.Round(18 * (dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0));
                    if (pixelWidth > 0 && pixelHeight > 0)
                    {
                        IntPtr hRgn = CreateRoundRectRgn(0, 0, pixelWidth + 1, pixelHeight + 1, radius * 2, radius * 2);
                        if (hRgn != IntPtr.Zero)
                        {
                            SetWindowRgn(hwnd, hRgn, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogException("UpdateRoundedCorners", ex);
            }
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            UpdateRoundedCorners();
        }

        private bool HandleGlobalKeyDownWhenOverlayVisible(int vkCode)
        {
            if (!this.IsVisible || _isHiding || MainBorder.Opacity < 0.5) return false;

            // 0. If currently recording hotkey, process key via low-level hook
            if (_isRecordingAppHotkey)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProcessRecordedKey(vkCode);
                }));
                return true; // Swallow key system-wide while recording
            }

            // 1. Escape key
            if (vkCode == 0x1B) // VK_ESCAPE
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var settingsGrid = (FrameworkElement?)this.FindName("HotkeySettingsGrid");
                    if (settingsGrid != null && settingsGrid.Visibility == Visibility.Visible)
                    {
                        HideHotkeySettings();
                    }
                    else if (ConfirmGrid != null && ConfirmGrid.Visibility == Visibility.Visible)
                    {
                        HideClearConfirm();
                    }
                    else if (PreviewGrid != null && PreviewGrid.Visibility == Visibility.Visible)
                    {
                        _viewModel.ClosePreviewCommand.Execute(null);
                    }
                    else if (_viewModel.IsSelectionMode)
                    {
                        _viewModel.IsSelectionMode = false;
                    }
                    else
                    {
                        HideWindowAnimated();
                    }
                }));
                return true;
            }

            // If modal dialogs are open, do not handle list navigation or paste
            var hotkeyModal = (FrameworkElement?)this.FindName("HotkeySettingsGrid");
            if (hotkeyModal != null && hotkeyModal.Visibility == Visibility.Visible)
            {
                return false;
            }
            if (ConfirmGrid != null && ConfirmGrid.Visibility == Visibility.Visible)
            {
                return false;
            }
            if (PreviewGrid != null && PreviewGrid.Visibility == Visibility.Visible)
            {
                return false;
            }

            // 2. If user explicitly clicked into SearchBox, let SearchBox handle typing directly
            if (SearchBox.IsKeyboardFocusWithin) return false;

            // 3. Arrow Up: navigate previous item in list
            if (vkCode == 0x26) // VK_UP
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (ItemsListView.SelectedIndex > 0)
                    {
                        ItemsListView.SelectedIndex--;
                        ItemsListView.ScrollIntoView(ItemsListView.SelectedItem);
                    }
                    else if (ItemsListView.SelectedIndex == -1 && ItemsListView.Items.Count > 0)
                    {
                        ItemsListView.SelectedIndex = ItemsListView.Items.Count - 1;
                        ItemsListView.ScrollIntoView(ItemsListView.SelectedItem);
                    }
                }));
                return true;
            }

            // 4. Arrow Down: navigate next item in list
            if (vkCode == 0x28) // VK_DOWN
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (ItemsListView.SelectedIndex < ItemsListView.Items.Count - 1)
                    {
                        ItemsListView.SelectedIndex++;
                        ItemsListView.ScrollIntoView(ItemsListView.SelectedItem);
                    }
                    else if (ItemsListView.SelectedIndex == -1 && ItemsListView.Items.Count > 0)
                    {
                        ItemsListView.SelectedIndex = 0;
                        ItemsListView.ScrollIntoView(ItemsListView.SelectedItem);
                    }
                }));
                return true;
            }

            // 5. Enter / Return: paste selected item to target window
            if (vkCode == 0x0D) // VK_RETURN
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_viewModel.IsSelectionMode)
                    {
                        if (ItemsListView.SelectedItem is ClipboardItem selItem)
                        {
                            selItem.IsSelected = !selItem.IsSelected;
                        }
                    }
                    else
                    {
                        var itemToPaste = ItemsListView.SelectedItem as ClipboardItem 
                            ?? (ItemsListView.Items.Count > 0 ? ItemsListView.Items[0] as ClipboardItem : null);
                        if (itemToPaste != null)
                        {
                            _viewModel.PasteItemCommand.Execute(itemToPaste);
                        }
                    }
                }));
                return true;
            }

            return false;
        }

        private void ShowToast()
        {
            var toastGrid = (FrameworkElement?)this.FindName("ToastGrid");
            if (toastGrid == null) return;

            var translate = toastGrid.RenderTransform as System.Windows.Media.TranslateTransform;
            if (translate == null)
            {
                translate = new System.Windows.Media.TranslateTransform(0, 0);
                toastGrid.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                toastGrid.RenderTransform = translate;
            }

            var opacityAnim = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames();
            opacityAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0, TimeSpan.FromMilliseconds(0)));
            opacityAnim.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(1, TimeSpan.FromMilliseconds(130), new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }));
            opacityAnim.KeyFrames.Add(new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(1, TimeSpan.FromMilliseconds(1200)));
            opacityAnim.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(1450), new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }));

            var slideAnim = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames();
            slideAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(8, TimeSpan.FromMilliseconds(0)));
            slideAnim.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(140), new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }));
            slideAnim.KeyFrames.Add(new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(0, TimeSpan.FromMilliseconds(1200)));
            slideAnim.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(4, TimeSpan.FromMilliseconds(1450), new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }));

            toastGrid.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
            translate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideAnim);
        }

        private void ShowClearConfirm()
        {
            var confirmGrid = (FrameworkElement?)this.FindName("ConfirmGrid");
            var confirmBorder = (FrameworkElement?)this.FindName("ConfirmBorder");
            if (confirmGrid != null)
            {
                confirmGrid.Visibility = Visibility.Visible;
                
                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120))
                {
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                confirmGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                if (confirmBorder != null)
                {
                    var scale = confirmBorder.RenderTransform as System.Windows.Media.ScaleTransform;
                    if (scale == null)
                    {
                        scale = new System.Windows.Media.ScaleTransform(0.96, 0.96);
                        confirmBorder.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                        confirmBorder.RenderTransform = scale;
                    }
                    var scaleAnim = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 0.96,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(140),
                        EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                    };
                    scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
                    scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
                }

                var cancelBtn = this.FindName("ConfirmCancelButton") as System.Windows.Controls.Button;
                cancelBtn?.Focus();
            }
        }

        private void HideClearConfirm(Action? onComplete = null)
        {
            var confirmGrid = (FrameworkElement?)this.FindName("ConfirmGrid");
            if (confirmGrid != null)
            {
                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100))
                {
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
                };
                fadeOut.Completed += (s, e) =>
                {
                    confirmGrid.Visibility = Visibility.Collapsed;
                    onComplete?.Invoke();
                };
                confirmGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private void ConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            HideClearConfirm(() =>
            {
                _viewModel.ConfirmClearAll();
                if (SearchBox.IsKeyboardFocusWithin) SearchBox.Focus();
            });
        }

        private void ConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            HideClearConfirm(() =>
            {
                if (SearchBox.IsKeyboardFocusWithin) SearchBox.Focus();
            });
        }

        private void ShowPreviewModal()
        {
            var previewGrid = (FrameworkElement?)this.FindName("PreviewGrid");
            var previewBorder = (FrameworkElement?)this.FindName("PreviewBorder");
            if (previewGrid != null)
            {
                try
                {
                    this.Activate();
                }
                catch { }

                previewGrid.Visibility = Visibility.Visible;

                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(130))
                {
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                previewGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                if (previewBorder != null)
                {
                    if (previewBorder.RenderTransform is not System.Windows.Media.ScaleTransform)
                    {
                        previewBorder.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                        previewBorder.RenderTransform = new System.Windows.Media.ScaleTransform(0.95, 0.95);
                    }

                    var scale = (System.Windows.Media.ScaleTransform)previewBorder.RenderTransform;
                    var scaleAnim = new System.Windows.Media.Animation.DoubleAnimation(0.95, 1.0, TimeSpan.FromMilliseconds(150))
                    {
                        EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                    };
                    scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
                    scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
                }

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var tb = (TextBox?)this.FindName("PreviewTextBox");
                        tb?.Focus();
                    }
                    catch { }
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void HidePreviewModal(Action? onComplete = null)
        {
            var previewGrid = (FrameworkElement?)this.FindName("PreviewGrid");
            var previewBorder = (FrameworkElement?)this.FindName("PreviewBorder");
            if (previewGrid != null && previewGrid.Visibility == Visibility.Visible)
            {
                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(110))
                {
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
                };
                fadeOut.Completed += (s, e) =>
                {
                    previewGrid.Visibility = Visibility.Collapsed;
                    previewGrid.Opacity = 0;
                    _viewModel.PreviewInfo = null;
                    onComplete?.Invoke();
                    if (SearchBox.IsKeyboardFocusWithin) SearchBox.Focus();
                };
                previewGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);

                if (previewBorder?.RenderTransform is System.Windows.Media.ScaleTransform scale)
                {
                    var scaleAnim = new System.Windows.Media.Animation.DoubleAnimation(1.0, 0.95, TimeSpan.FromMilliseconds(110))
                    {
                        EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
                    };
                    scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
                    scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
                }
            }
            else
            {
                _viewModel.PreviewInfo = null;
                onComplete?.Invoke();
            }
        }

        private void ResetPreviewImmediate()
        {
            var previewGrid = (FrameworkElement?)this.FindName("PreviewGrid");
            var previewBorder = (FrameworkElement?)this.FindName("PreviewBorder");
            if (previewGrid != null)
            {
                previewGrid.BeginAnimation(UIElement.OpacityProperty, null);
                previewGrid.Visibility = Visibility.Collapsed;
                previewGrid.Opacity = 0;
            }
            if (previewBorder != null)
            {
                previewBorder.BeginAnimation(UIElement.OpacityProperty, null);
                if (previewBorder.RenderTransform is System.Windows.Media.ScaleTransform scale)
                {
                    scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, null);
                    scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, null);
                    scale.ScaleX = 0.95;
                    scale.ScaleY = 0.95;
                }
            }
            _viewModel.PreviewInfo = null;
        }

        private async void SavePreviewImageToFile(BitmapSource bitmap)
        {
            try
            {
                // 1. Clone or Freeze the bitmap so it can be safely passed to and written from a background thread
                BitmapSource frozenBitmap = bitmap;
                if (!frozenBitmap.IsFrozen)
                {
                    if (frozenBitmap.CanFreeze)
                    {
                        frozenBitmap.Freeze();
                    }
                    else
                    {
                        frozenBitmap = frozenBitmap.Clone();
                        frozenBitmap.Freeze();
                    }
                }

                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Image",
                    Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp",
                    FileName = $"ClipboardImage_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                bool? dialogResult = sfd.ShowDialog(this);

                if (dialogResult == true)
                {
                    string targetFile = sfd.FileName;
                    // 3. Offload image compression and disk writing to a background task so the UI thread NEVER freezes
                    bool success = await Task.Run(() => CustomClipboardManager.Services.FileHelper.SaveBitmapSourceToFile(frozenBitmap, targetFile));
                    if (success)
                    {
                        _viewModel.ToastMessage = "Image saved successfully!";
                        _viewModel.ShowToastNotification?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save image failed: {ex.Message}");
            }
        }

        private void PreviewBackdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                _viewModel.ClosePreviewCommand.Execute(null);
            }
        }

        private void HotkeySettingsBackdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                HideHotkeySettings();
            }
        }

        private void ConfirmBackdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                HideClearConfirm();
            }
        }

        private void PreviewTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.Activate();
                if (sender is System.Windows.Controls.TextBox tb)
                {
                    if (!tb.IsKeyboardFocused)
                    {
                        tb.Focus();
                    }
                }
            }
            catch { }
        }

        private void PreviewSelectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Activate();
                var tb = this.FindName("PreviewTextBox") as System.Windows.Controls.TextBox;
                if (tb != null)
                {
                    tb.Focus();
                    tb.SelectAll();
                }
            }
            catch { }
        }

        private void ShowWindowAnimated()
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(110),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };

            var slideAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 8,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(130),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };

            var translate = new System.Windows.Media.TranslateTransform(0, 8);
            MainBorder.RenderTransformOrigin = new System.Windows.Point(0.5, 0.3);
            MainBorder.RenderTransform = translate;

            translate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideAnim);
            MainBorder.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void HideWindowAnimated()
        {
            _outsideClickTimer?.Stop();
            ResetPreviewImmediate();
            if (_viewModel != null && _viewModel.IsSelectionMode)
            {
                _viewModel.IsSelectionMode = false;
            }
            ResetSearchAndScrollToLatest();
            var confirmGrid = (FrameworkElement?)this.FindName("ConfirmGrid");
            if (confirmGrid != null)
            {
                confirmGrid.BeginAnimation(UIElement.OpacityProperty, null);
                confirmGrid.Visibility = Visibility.Collapsed;
                confirmGrid.Opacity = 0;
            }
            var settingsGrid = (FrameworkElement?)this.FindName("HotkeySettingsGrid");
            if (settingsGrid != null)
            {
                settingsGrid.BeginAnimation(UIElement.OpacityProperty, null);
                settingsGrid.Visibility = Visibility.Collapsed;
                settingsGrid.Opacity = 0;
            }
            _isRecordingAppHotkey = false;

            // When unpinned, seamlessly return focus to previous window so user context and highlights are undisturbed
            if (_viewModel != null && !_viewModel.IsWindowPinned)
            {
                RestoreFocusToPreviousWindow();
            }

            _isHiding = true;
            int currentSession = ++_hideAnimationId;

            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(80),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };

            var slideAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 6,
                Duration = TimeSpan.FromMilliseconds(80),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };

            var translate = MainBorder.RenderTransform as System.Windows.Media.TranslateTransform;
            if (translate == null)
            {
                translate = new System.Windows.Media.TranslateTransform(0, 0);
                MainBorder.RenderTransformOrigin = new System.Windows.Point(0.5, 0.3);
                MainBorder.RenderTransform = translate;
            }

            translate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideAnim);
            anim.Completed += (s, e) =>
            {
                if (currentSession == _hideAnimationId && _isHiding)
                {
                    this.Hide();
                    _isHiding = false;
                    _viewModel?.StopTimeAgoTimer();
                    ScheduleIdleEfficiencyMode();
                    ResetPreviewImmediate();
                    ResetSearchAndScrollToLatest();
                }
            };
            MainBorder.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private static System.Windows.Controls.ScrollViewer? GetScrollViewer(DependencyObject? depObj)
        {
            if (depObj == null) return null;
            if (depObj is System.Windows.Controls.ScrollViewer sv) return sv;
            if (depObj is System.Windows.Controls.Control ctrl && ctrl.Template != null)
            {
                var found = ctrl.Template.FindName("ItemsScrollViewer", ctrl) as System.Windows.Controls.ScrollViewer;
                if (found != null) return found;
            }
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private void ScrollToLatestItem()
        {
            var listView = ItemsListView;
            if (listView == null) return;

            listView.SelectedIndex = -1;

            var sv = GetScrollViewer(listView);
            if (sv != null)
            {
                sv.ScrollToTop();
                sv.ScrollToVerticalOffset(0);
            }

            if (listView.Items.Count > 0)
            {
                try
                {
                    listView.ScrollIntoView(listView.Items[0]);
                }
                catch { }
            }

            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                var scrollViewer = GetScrollViewer(listView);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToTop();
                    scrollViewer.ScrollToVerticalOffset(0);
                }

                if (listView.Items.Count > 0)
                {
                    try
                    {
                        listView.ScrollIntoView(listView.Items[0]);
                    }
                    catch { }
                }
            }));
        }

        private void ResetSearchAndScrollToLatest()
        {
            if (_viewModel != null)
            {
                _viewModel.ResetSearch();
                if (_viewModel.IsSelectionMode)
                {
                    _viewModel.IsSelectionMode = false;
                }
            }

            if (SearchBox != null && !string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchBox.Text = string.Empty;
            }

            ScrollToLatestItem();
        }

        private void FilterScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var scrollViewer = (System.Windows.Controls.ScrollViewer)sender;
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        private async void ClipboardMonitor_ClipboardChanged(object? sender, EventArgs e)
        {
            if (_isPasting)
            {
                _isPasting = false;
                return;
            }

            // If Service is paused or stopped, do not capture new clipboard content
            if (!_viewModel.IsServiceRunning)
            {
                return;
            }

            // Debounce to prevent rapid double-trigger events
            if ((DateTime.Now - _lastClipboardEventTime).TotalMilliseconds < 150)
            {
                return;
            }
            _lastClipboardEventTime = DateTime.Now;

            // Incremental backoff retry loop for external app clipboard contention (Excel, Word, Browsers)
            int[] retryDelays = new int[] { 40, 80, 120, 160, 200 };
            foreach (int delay in retryDelays)
            {
                await System.Threading.Tasks.Task.Delay(delay);
                if (TryProcessClipboard())
                {
                    return;
                }
            }
        }

        private bool TryProcessClipboard()
        {
            try
            {
                var dataObj = System.Windows.Clipboard.GetDataObject();
                if (dataObj == null) return false;

                if (dataObj.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
                {
                    if (dataObj.GetData(System.Windows.DataFormats.FileDrop, true) is string[] files && files.Length > 0)
                    {
                        var item = new ClipboardItem();
                        CustomClipboardManager.Services.FileHelper.PopulateFileDetails(item, files);
                        _viewModel.AddItem(item);
                        return true;
                    }
                }
                else if (dataObj.GetDataPresent(System.Windows.DataFormats.Bitmap, true) || System.Windows.Clipboard.ContainsImage())
                {
                    var image = System.Windows.Clipboard.GetImage();
                    if (image != null)
                    {
                        if (image.CanFreeze && !image.IsFrozen)
                        {
                            image.Freeze();
                        }
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.Image,
                            ImageContent = image,
                            Category = SmartCategory.Image
                        };
                        _viewModel.AddItem(item);
                        return true;
                    }
                }
                else if (dataObj.GetDataPresent(System.Windows.DataFormats.UnicodeText, true) || dataObj.GetDataPresent(System.Windows.DataFormats.Text, true))
                {
                    string? text = dataObj.GetData(System.Windows.DataFormats.UnicodeText, true) as string ?? dataObj.GetData(System.Windows.DataFormats.Text, true) as string;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.Text,
                            TextContent = text,
                            Category = DetermineCategory(text)
                        };
                        _viewModel.AddItem(item);
                        return true;
                    }
                }
                else if (dataObj.GetDataPresent(System.Windows.DataFormats.WaveAudio, true))
                {
                    using var stream = System.Windows.Clipboard.GetAudioStream();
                    if (stream != null)
                    {
                        using var ms = new System.IO.MemoryStream();
                        stream.CopyTo(ms);
                        byte[] audioBytes = ms.ToArray();
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.Audio,
                            TextContent = $"[Audio {audioBytes.Length / 1024} KB]",
                            Category = SmartCategory.Audio,
                            RawData = audioBytes
                        };
                        _viewModel.AddItem(item);
                        return true;
                    }
                }
                else
                {
                    // Fallback to "Other"
                    var formats = dataObj.GetFormats();
                    if (formats != null && formats.Length > 0)
                    {
                        var formatsStr = string.Join(", ", System.Linq.Enumerable.Take(formats, 5));
                        if (formats.Length > 5) formatsStr += "...";

                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.Other,
                            TextContent = formatsStr,
                            Category = SmartCategory.Others,
                            RawData = dataObj
                        };
                        _viewModel.AddItem(item);
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool IsPureEmoji(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string trimmed = text.Trim();
            if (trimmed.Length == 0 || trimmed.Length > 30) return false;

            int emojiCount = 0;
            for (int i = 0; i < trimmed.Length; i++)
            {
                int codePoint = char.ConvertToUtf32(trimmed, i);
                if (char.IsSurrogatePair(trimmed, i)) i++;

                if (codePoint == 0x200D || codePoint == 0xFE0E || codePoint == 0xFE0F || (codePoint >= 0x1F3FB && codePoint <= 0x1F3FF))
                {
                    continue;
                }

                if (IsEmojiCodePoint(codePoint))
                {
                    emojiCount++;
                }
                else if (!char.IsWhiteSpace((char)codePoint))
                {
                    return false;
                }
            }

            return emojiCount > 0 && emojiCount <= 8;
        }

        public static bool IsEmojiCodePoint(int cp)
        {
            return (cp >= 0x1F600 && cp <= 0x1F64F) ||
                   (cp >= 0x1F300 && cp <= 0x1F5FF) ||
                   (cp >= 0x1F680 && cp <= 0x1F6FF) ||
                   (cp >= 0x1F900 && cp <= 0x1F9FF) ||
                   (cp >= 0x1FA70 && cp <= 0x1FAFF) ||
                   (cp >= 0x2600 && cp <= 0x26FF)   ||
                   (cp >= 0x2700 && cp <= 0x27BF)   ||
                   (cp >= 0x1F1E6 && cp <= 0x1F1FF) ||
                   (cp >= 0x2300 && cp <= 0x23FF);
        }

        private SmartCategory DetermineCategory(string text)
        {
            string trimmed = text.Trim();
            if (IsPureEmoji(trimmed)) return SmartCategory.Emoji;

            if (Uri.IsWellFormedUriString(trimmed, UriKind.Absolute)) return SmartCategory.Link;

            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$"))
                return SmartCategory.ColorCode;

            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^(?:rgb|hsl)a?\s*\([\d\s.,%]+\)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return SmartCategory.ColorCode;
            
            int codeScore = 0;
            if (trimmed.StartsWith("```")) codeScore += 3;
            if (trimmed.Contains("{") && trimmed.Contains("}")) codeScore++;
            if (trimmed.Contains(";") && trimmed.Contains("(")) codeScore++;
            if (trimmed.Contains("using ") || trimmed.Contains("import ") || trimmed.Contains("require(")) codeScore += 2;
            if (trimmed.Contains("public ") || trimmed.Contains("private ") || trimmed.Contains("class ")) codeScore++;
            if (trimmed.Contains("function ") || trimmed.Contains("const ") || trimmed.Contains("let ") || trimmed.Contains("var ")) codeScore++;
            if (trimmed.Contains("=>") || trimmed.Contains("==") || trimmed.Contains("===") || trimmed.Contains("!=")) codeScore++;
            if ((trimmed.Contains("<") && trimmed.Contains("/>")) || trimmed.Contains("</")) codeScore++;
            
            if (codeScore >= 3) return SmartCategory.Code;

            return SmartCategory.Text;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _isDisposed = true;
            try
            {
                _hotkeyWatcher?.Dispose();
                _hotkeyWatcher = null;
            }
            catch { }
            try
            {
                _showEvent?.Set();
                _showEvent?.Dispose();
            }
            catch { }

            if (_foregroundHook != IntPtr.Zero)
            {
                UnhookWinEvent(_foregroundHook);
                _foregroundHook = IntPtr.Zero;
            }

            if (_isHotKeyRegistered)
            {
                IntPtr hwnd = _windowHwnd != IntPtr.Zero ? _windowHwnd : new System.Windows.Interop.WindowInteropHelper(this).Handle;
                UnregisterHotKey(hwnd, HOTKEY_ID);
                _isHotKeyRegistered = false;
            }

            if (_windowHwndSource != null)
            {
                _windowHwndSource.RemoveHook(HwndMessageHook);
                _windowHwndSource = null;
            }

            _keyboardHook?.Dispose();
            _clipboardMonitor?.Dispose();

            if (_notifyIcon != null)
            {
                try
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                catch { }
                _notifyIcon = null;
            }

            try
            {
                _viewModel?.SavePersistedData();
            }
            catch { }

            try
            {
                CustomClipboardManager.Services.ServiceManager.StopServiceSync();
            }
            catch { }
        }

        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private System.Windows.Forms.ToolStripMenuItem? _trayWindowMenuItem;
        private System.Windows.Forms.ToolStripMenuItem? _trayServiceMenuItem;

        private void InitializeSystemTrayIcon()
        {
            try
            {
                var contextMenu = new System.Windows.Forms.ContextMenuStrip();

                _trayWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem("Show Clipboard Manager", null, (s, e) =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (this.IsVisible && MainBorder.Opacity > 0.5)
                        {
                            HideWindowAnimated();
                        }
                        else
                        {
                            ShowAtCursor();
                        }
                    }));
                });
                _trayWindowMenuItem.Font = new System.Drawing.Font(_trayWindowMenuItem.Font, System.Drawing.FontStyle.Bold);

                _trayServiceMenuItem = new System.Windows.Forms.ToolStripMenuItem("Pause Service (Active)", null, (s, e) =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _viewModel.ToggleServiceCommand.Execute(null);
                    }));
                });

                var exitMenuItem = new System.Windows.Forms.ToolStripMenuItem("Exit Application", null, (s, e) =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _viewModel.ExitApplicationCommand.Execute(null);
                    }));
                });

                contextMenu.Items.Add(_trayWindowMenuItem);
                contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                contextMenu.Items.Add(_trayServiceMenuItem);
                contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                contextMenu.Items.Add(exitMenuItem);

                contextMenu.Opening += (s, e) =>
                {
                    bool isVisible = this.IsVisible && MainBorder.Opacity > 0.5;
                    _trayWindowMenuItem.Text = isVisible ? "Hide Clipboard Manager" : "Show Clipboard Manager";
                    _trayServiceMenuItem.Text = _viewModel.IsServiceRunning ? "Pause Service (Active)" : "Resume Service (Paused)";
                    _trayServiceMenuItem.Checked = _viewModel.IsServiceRunning;
                };

                _notifyIcon = new System.Windows.Forms.NotifyIcon
                {
                    Text = "Custom Clipboard Manager",
                    ContextMenuStrip = contextMenu,
                    Visible = true,
                    Icon = LoadAppIcon()
                };

                _notifyIcon.DoubleClick += (s, e) =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (this.IsVisible && MainBorder.Opacity > 0.5)
                        {
                            HideWindowAnimated();
                        }
                        else
                        {
                            ShowAtCursor();
                        }
                    }));
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize NotifyIcon: {ex.Message}");
            }
        }

        private System.Drawing.Icon LoadAppIcon()
        {
            try
            {
                var iconUri = new Uri("pack://application:,,,/Clipboard.ico");
                var streamInfo = System.Windows.Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    using var s = streamInfo.Stream;
                    return new System.Drawing.Icon(s);
                }
            }
            catch { }

            try
            {
                string icoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Clipboard.ico");
                if (System.IO.File.Exists(icoPath))
                {
                    return new System.Drawing.Icon(icoPath);
                }
            }
            catch { }

            try
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                {
                    var ico = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                    if (ico != null) return ico;
                }
            }
            catch { }

            return System.Drawing.SystemIcons.Application;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_viewModel.IsSelectionMode)
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    _viewModel.IsSelectionMode = false;
                    return;
                }
                if (e.Key == Key.Delete && !SearchBox.IsKeyboardFocusWithin)
                {
                    if (_viewModel.HasSelectedItems)
                    {
                        e.Handled = true;
                        _viewModel.DeleteSelectedCommand.Execute(null);
                        return;
                    }
                }
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.A && !SearchBox.IsKeyboardFocusWithin)
                {
                    e.Handled = true;
                    _viewModel.IsSelectAll = (_viewModel.IsSelectAll != true);
                    return;
                }
                if ((e.Key == Key.Space || e.Key == Key.Enter) && !SearchBox.IsKeyboardFocusWithin)
                {
                    if (ItemsListView.SelectedItem is ClipboardItem currentItem)
                    {
                        e.Handled = true;
                        currentItem.IsSelected = !currentItem.IsSelected;
                        return;
                    }
                }
            }

            var confirmGrid = (FrameworkElement?)this.FindName("ConfirmGrid");
            if (confirmGrid != null && confirmGrid.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    HideClearConfirm(() => SearchBox.Focus());
                    return;
                }
                if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    e.Handled = true;
                    HideClearConfirm(() =>
                    {
                        _viewModel.ConfirmClearAll();
                        SearchBox.Focus();
                    });
                    return;
                }
                // Suppress hotkey pasting and other keys while confirm dialog is shown
                e.Handled = true;
                return;
            }

            var previewGrid = (FrameworkElement?)this.FindName("PreviewGrid");
            if (previewGrid != null && previewGrid.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Escape || e.Key == Key.Space)
                {
                    e.Handled = true;
                    _viewModel.ClosePreviewCommand.Execute(null);
                    return;
                }
                if (e.Key == Key.Tab)
                {
                    e.Handled = true;
                    int nextTab = _viewModel.PreviewInfo?.SelectedTabIndex == 0 ? 1 : 0;
                    _viewModel.SelectPreviewTabCommand.Execute(nextTab);
                    return;
                }
                if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    if (_viewModel.PreviewInfo?.SourceItem != null)
                    {
                        e.Handled = true;
                        _viewModel.PasteItemCommand.Execute(_viewModel.PreviewInfo.SourceItem);
                        return;
                    }
                }
                // Suppress hotkey pasting and other list interactions while preview modal is open
                e.Handled = true;
                return;
            }

            if (_isRecordingAppHotkey)
            {
                e.Handled = true;
                Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
                int vk = KeyInterop.VirtualKeyFromKey(key);
                ProcessRecordedKey(vk);
                return;
            }

            var settingsGrid = (FrameworkElement?)this.FindName("HotkeySettingsGrid");
            if (settingsGrid != null && settingsGrid.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    HideHotkeySettings();
                    return;
                }
                // Suppress other list interactions while settings modal is open
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape || ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && (e.SystemKey == Key.F4 || e.Key == Key.F4)))
            {
                e.Handled = true;
                HideWindowAnimated();
            }
            else if (e.Key == Key.Space && !SearchBox.IsFocused)
            {
                e.Handled = true;
                var selectedItem = ItemsListView.SelectedItem as ClipboardItem ?? _viewModel.ClipboardItemsView.Cast<ClipboardItem>().FirstOrDefault();
                if (selectedItem != null)
                {
                    _viewModel.PreviewItemCommand.Execute(selectedItem);
                }
            }
        }

        private void ProcessRecordedKey(int vkCode)
        {
            if (!_isRecordingAppHotkey) return;

            // 1. Cancel on Escape
            if (vkCode == 0x1B) // VK_ESCAPE
            {
                CancelRecordingHotkey();
                return;
            }

            // 2. Check if modifier key
            bool isMod = (vkCode == 0x11 || vkCode == 0xA2 || vkCode == 0xA3 || // Ctrl
                          vkCode == 0x12 || vkCode == 0xA4 || vkCode == 0xA5 || // Alt
                          vkCode == 0x10 || vkCode == 0xA0 || vkCode == 0xA1 || // Shift
                          vkCode == 0x5B || vkCode == 0x5C);                     // Win

            bool ctrl = (GetAsyncKeyState(0x11) & 0x8000) != 0;
            bool alt = (GetAsyncKeyState(0x12) & 0x8000) != 0;
            bool shift = (GetAsyncKeyState(0x10) & 0x8000) != 0;
            bool win = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;

            if (isMod)
            {
                var parts = new List<string>();
                if (ctrl) parts.Add("Ctrl");
                if (alt) parts.Add("Alt");
                if (shift) parts.Add("Shift");
                if (win) parts.Add("Win");
                parts.Add("...");

                if (AppHotkeyDisplayText != null)
                {
                    AppHotkeyDisplayText.Text = string.Join(" + ", parts);
                }

                _isUpdatingCheckboxes = true;
                try
                {
                    if (ModCtrlCheckBox != null) ModCtrlCheckBox.IsChecked = ctrl;
                    if (ModAltCheckBox != null) ModAltCheckBox.IsChecked = alt;
                    if (ModShiftCheckBox != null) ModShiftCheckBox.IsChecked = shift;
                    if (ModWinCheckBox != null) ModWinCheckBox.IsChecked = win;
                }
                finally
                {
                    _isUpdatingCheckboxes = false;
                }
                return;
            }

            // 3. User pressed a trigger key
            if (!ctrl && !alt && !shift && !win)
            {
                ctrl = ModCtrlCheckBox?.IsChecked == true;
                alt = ModAltCheckBox?.IsChecked == true;
                shift = ModShiftCheckBox?.IsChecked == true;
                win = ModWinCheckBox?.IsChecked == true;
            }

            if (!ctrl && !alt && !shift && !win && !(vkCode >= 0x70 && vkCode <= 0x7B))
            {
                ctrl = true;
            }

            string friendlyName = HotkeyConfig.GetFriendlyKeyName(vkCode);
            var newConfig = new HotkeyConfig
            {
                Control = ctrl,
                Alt = alt,
                Shift = shift,
                Windows = win,
                VirtualKey = vkCode,
                KeyName = friendlyName
            };
            newConfig.DisplayText = newConfig.BuildDisplayText();

            _hotkeyConfig = newConfig;
            HotkeyManager.Save(newConfig);
            ApplyRegisteredHotkey(_windowHwnd);

            _isRecordingAppHotkey = false;
            UpdateHotkeySettingsUI(isSuccess: true);
        }

        private void CancelRecordingHotkey()
        {
            _isRecordingAppHotkey = false;
            UpdateHotkeySettingsUI(isSuccess: false);
            if (AppHotkeyHintText != null)
            {
                AppHotkeyHintText.Text = I18n.Current.HotkeyCancelled;
                AppHotkeyHintText.Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush");
            }
        }

        private void UpdateHotkeySettingsUI(bool isSuccess = false)
        {
            if (AppRecordHotkeyButton != null)
            {
                AppRecordHotkeyButton.BorderBrush = (System.Windows.Media.Brush)FindResource("CardBorderBrush");
                AppRecordHotkeyButton.Background = (System.Windows.Media.Brush)FindResource("SearchBackgroundBrush");
            }

            if (AppHotkeyDisplayText != null)
            {
                AppHotkeyDisplayText.Text = _hotkeyConfig.DisplayText;
                AppHotkeyDisplayText.Foreground = (System.Windows.Media.Brush)FindResource("TextForegroundBrush");
            }

            _isUpdatingCheckboxes = true;
            try
            {
                if (ModCtrlCheckBox != null) ModCtrlCheckBox.IsChecked = _hotkeyConfig.Control;
                if (ModAltCheckBox != null) ModAltCheckBox.IsChecked = _hotkeyConfig.Alt;
                if (ModShiftCheckBox != null) ModShiftCheckBox.IsChecked = _hotkeyConfig.Shift;
                if (ModWinCheckBox != null) ModWinCheckBox.IsChecked = _hotkeyConfig.Windows;
            }
            finally
            {
                _isUpdatingCheckboxes = false;
            }

            if (AppHotkeyHintText != null)
            {
                if (isSuccess)
                {
                    AppHotkeyHintText.Text = string.Format(I18n.Current.HotkeySavedSuccess, _hotkeyConfig.DisplayText);
                    AppHotkeyHintText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 199, 89));
                }
                else
                {
                    AppHotkeyHintText.Text = I18n.Current.HotkeyInstructionHint;
                    AppHotkeyHintText.Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush");
                }
            }
        }

        private void ModifierCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingCheckboxes) return;

            bool ctrl = ModCtrlCheckBox?.IsChecked == true;
            bool alt = ModAltCheckBox?.IsChecked == true;
            bool shift = ModShiftCheckBox?.IsChecked == true;
            bool win = ModWinCheckBox?.IsChecked == true;

            // Enforce at least one modifier if not F1-F12
            if (!ctrl && !alt && !shift && !win && !(_hotkeyConfig.VirtualKey >= 0x70 && _hotkeyConfig.VirtualKey <= 0x7B))
            {
                ctrl = true;
                _isUpdatingCheckboxes = true;
                try
                {
                    if (ModCtrlCheckBox != null) ModCtrlCheckBox.IsChecked = true;
                }
                finally
                {
                    _isUpdatingCheckboxes = false;
                }
            }

            _hotkeyConfig.Control = ctrl;
            _hotkeyConfig.Alt = alt;
            _hotkeyConfig.Shift = shift;
            _hotkeyConfig.Windows = win;
            _hotkeyConfig.DisplayText = _hotkeyConfig.BuildDisplayText();

            HotkeyManager.Save(_hotkeyConfig);
            ApplyRegisteredHotkey(_windowHwnd);
            UpdateHotkeySettingsUI(isSuccess: true);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHotkeySettings();
        }

        private void ShowHotkeySettings()
        {
            _hotkeyConfig = HotkeyManager.Load();
            _isRecordingAppHotkey = false;
            UpdateHotkeySettingsUI(isSuccess: false);

            var settingsGrid = (FrameworkElement?)this.FindName("HotkeySettingsGrid");
            if (settingsGrid != null)
            {
                settingsGrid.Visibility = Visibility.Visible;
                var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
                {
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                settingsGrid.BeginAnimation(UIElement.OpacityProperty, anim);
            }
        }

        private void HideHotkeySettings()
        {
            _isRecordingAppHotkey = false;
            var settingsGrid = (FrameworkElement?)this.FindName("HotkeySettingsGrid");
            if (settingsGrid != null && settingsGrid.Visibility == Visibility.Visible)
            {
                var anim = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120))
                {
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
                };
                anim.Completed += (s, e) =>
                {
                    settingsGrid.Visibility = Visibility.Collapsed;
                    settingsGrid.Opacity = 0;
                };
                settingsGrid.BeginAnimation(UIElement.OpacityProperty, anim);
            }
        }

        private void CloseHotkeySettings_Click(object sender, RoutedEventArgs e)
        {
            HideHotkeySettings();
        }

        private void AppRecordHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            _isRecordingAppHotkey = true;
            if (AppRecordHotkeyButton != null)
            {
                AppRecordHotkeyButton.BorderBrush = (System.Windows.Media.Brush)FindResource("AccentBrush");
            }
            if (AppHotkeyDisplayText != null)
            {
                AppHotkeyDisplayText.Text = I18n.Current.HotkeyRecordingListening;
                AppHotkeyDisplayText.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
            }
            if (AppHotkeyHintText != null)
            {
                AppHotkeyHintText.Text = I18n.Current.HotkeyRecordingPromptFull;
                AppHotkeyHintText.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
            }
        }

        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string tag)
            {
                var newConfig = HotkeyManager.GetPreset(tag);
                _hotkeyConfig = newConfig;
                HotkeyManager.Save(newConfig);
                ApplyRegisteredHotkey(_windowHwnd);
                _isRecordingAppHotkey = false;
                UpdateHotkeySettingsUI(isSuccess: true);
            }
        }

        private void ResetAppHotkey_Click(object sender, RoutedEventArgs e)
        {
            var defConfig = HotkeyConfig.Default;
            _hotkeyConfig = defConfig;
            HotkeyManager.Save(defConfig);
            ApplyRegisteredHotkey(_windowHwnd);
            _isRecordingAppHotkey = false;
            UpdateHotkeySettingsUI(isSuccess: false);
            if (AppHotkeyHintText != null)
            {
                AppHotkeyHintText.Text = string.Format(I18n.Current.HotkeyResetSuccess, "Ctrl + Shift + V");
                AppHotkeyHintText.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
            }
        }

        private bool _isMovingWindow = false;
        private Win32Point _lastMouseScreenRaw;
        private bool _isSearchBoxActivating = false;

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Check if the click is outside the MainBorder
                var pos = e.GetPosition(MainBorder);
                if (pos.X < 0 || pos.Y < 0 || pos.X > MainBorder.ActualWidth || pos.Y > MainBorder.ActualHeight)
                {
                    if (_viewModel != null && !_viewModel.IsWindowPinned)
                    {
                        HideWindowAnimated();
                        return;
                    }
                }

                // If clicking inside active modal overlays, don't initiate window drag
                var confirmGrid = (FrameworkElement?)this.FindName("ConfirmGrid");
                if (confirmGrid != null && confirmGrid.Visibility == Visibility.Visible)
                {
                    var p = e.GetPosition(confirmGrid);
                    if (p.X >= 0 && p.Y >= 0 && p.X <= confirmGrid.ActualWidth && p.Y <= confirmGrid.ActualHeight) return;
                }

                var settingsGrid = (FrameworkElement?)this.FindName("HotkeySettingsGrid");
                if (settingsGrid != null && settingsGrid.Visibility == Visibility.Visible)
                {
                    var p = e.GetPosition(settingsGrid);
                    if (p.X >= 0 && p.Y >= 0 && p.X <= settingsGrid.ActualWidth && p.Y <= settingsGrid.ActualHeight) return;
                }

                var previewGrid = (FrameworkElement?)this.FindName("PreviewGrid");
                if (previewGrid != null && previewGrid.Visibility == Visibility.Visible)
                {
                    var p = e.GetPosition(previewGrid);
                    if (p.X >= 0 && p.Y >= 0 && p.X <= previewGrid.ActualWidth && p.Y <= previewGrid.ActualHeight) return;
                }

                // If clicking inside the item list, search box, or category filter bar, don't initiate window drag
                var listPos = e.GetPosition(ItemsListView);
                if (listPos.X >= 0 && listPos.Y >= 0 && listPos.X <= ItemsListView.ActualWidth && listPos.Y <= ItemsListView.ActualHeight)
                {
                    return;
                }

                var searchPos = e.GetPosition(SearchBox);
                if (searchPos.X >= 0 && searchPos.Y >= 0 && searchPos.X <= SearchBox.ActualWidth && searchPos.Y <= SearchBox.ActualHeight)
                {
                    return;
                }

                var filterPos = e.GetPosition(FilterScrollViewer);
                if (filterPos.X >= 0 && filterPos.Y >= 0 && filterPos.X <= FilterScrollViewer.ActualWidth && filterPos.Y <= FilterScrollViewer.ActualHeight)
                {
                    return;
                }

                // If clicking any button, toggle, or interactive control, do not initiate window drag
                DependencyObject? dep = e.OriginalSource as DependencyObject;
                while (dep != null && dep != this)
                {
                    if (dep is System.Windows.Controls.Primitives.ButtonBase ||
                        dep is System.Windows.Controls.TextBox ||
                        dep is System.Windows.Controls.ListBox ||
                        dep is System.Windows.Controls.ScrollViewer)
                    {
                        return;
                    }
                    dep = System.Windows.Media.VisualTreeHelper.GetParent(dep) ?? LogicalTreeHelper.GetParent(dep);
                }

                // Initiate smooth, non-activating window drag without stealing focus or disrupting active text highlights
                _isMovingWindow = true;
                var mousePt = new Win32Point();
                GetCursorPos(ref mousePt);
                _lastMouseScreenRaw = mousePt;

                this.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isMovingWindow && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentMouseRaw = new Win32Point();
                GetCursorPos(ref currentMouseRaw);

                int deltaPixelX = currentMouseRaw.X - _lastMouseScreenRaw.X;
                int deltaPixelY = currentMouseRaw.Y - _lastMouseScreenRaw.Y;

                if (deltaPixelX != 0 || deltaPixelY != 0)
                {
                    var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
                    double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                    double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

                    this.Left += deltaPixelX / scaleX;
                    this.Top += deltaPixelY / scaleY;

                    if (_windowHwnd != IntPtr.Zero)
                    {
                        SetWindowPos(_windowHwnd, IntPtr.Zero, 0, 0, 0, 0, 
                            SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
                    }

                    _lastMouseScreenRaw = currentMouseRaw;
                }
                e.Handled = true;
            }
            else if (_isMovingWindow && e.LeftButton != MouseButtonState.Pressed)
            {
                _isMovingWindow = false;
                this.ReleaseMouseCapture();
                CheckAndDockToEdge();
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isMovingWindow)
            {
                _isMovingWindow = false;
                this.ReleaseMouseCapture();
                CheckAndDockToEdge();
                e.Handled = true;
            }
        }

        private void MainWindow_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isMovingWindow)
            {
                _isMovingWindow = false;
                CheckAndDockToEdge();
            }
        }

        private void SearchBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isSearchBoxActivating = true;
            try
            {
                this.Activate();
                SearchBox.Focus();
            }
            finally
            {
                _isSearchBoxActivating = false;
            }
        }

        private void TrafficRed_Click(object sender, RoutedEventArgs e)
        {
            HideWindowAnimated();
        }

        private void SearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_viewModel.IsSelectionMode)
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    _viewModel.IsSelectionMode = false;
                    return;
                }
            }

            var confirmGrid = (FrameworkElement?)this.FindName("ConfirmGrid");
            if (confirmGrid != null && confirmGrid.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    HideClearConfirm(() => SearchBox.Focus());
                    return;
                }
                if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    e.Handled = true;
                    HideClearConfirm(() =>
                    {
                        _viewModel.ConfirmClearAll();
                        SearchBox.Focus();
                    });
                    return;
                }
                e.Handled = true;
                return;
            }

            var previewGrid = (FrameworkElement?)this.FindName("PreviewGrid");
            if (previewGrid != null && previewGrid.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Escape || e.Key == Key.Space)
                {
                    e.Handled = true;
                    _viewModel.ClosePreviewCommand.Execute(null);
                    return;
                }
                e.Handled = true;
                return;
            }

            var listView = (System.Windows.Controls.ListView?)this.FindName("ItemsListView");
            if (listView == null) return;

            if (e.Key == Key.Up)
            {
                e.Handled = true;
                if (listView.SelectedIndex > 0)
                {
                    listView.SelectedIndex--;
                    listView.ScrollIntoView(listView.SelectedItem);
                }
                else if (listView.SelectedIndex == -1 && listView.Items.Count > 0)
                {
                    listView.SelectedIndex = listView.Items.Count - 1;
                    listView.ScrollIntoView(listView.SelectedItem);
                }
            }
            else if (e.Key == Key.Down)
            {
                e.Handled = true;
                if (listView.SelectedIndex < listView.Items.Count - 1)
                {
                    listView.SelectedIndex++;
                    listView.ScrollIntoView(listView.SelectedItem);
                }
                else if (listView.SelectedIndex == -1 && listView.Items.Count > 0)
                {
                    listView.SelectedIndex = 0;
                    listView.ScrollIntoView(listView.SelectedItem);
                }
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                if (_viewModel.IsSelectionMode)
                {
                    if (listView.SelectedItem is ClipboardItem selItem)
                    {
                        selItem.IsSelected = !selItem.IsSelected;
                    }
                    return;
                }
                if (listView.SelectedItem is ClipboardItem item)
                {
                    _viewModel.PasteItemCommand.Execute(item);
                }
                else if (listView.Items.Count > 0)
                {
                    if (listView.Items[0] is ClipboardItem firstItem)
                    {
                        _viewModel.PasteItemCommand.Execute(firstItem);
                    }
                }
            }
        }

        private System.Windows.Point? _dragStartPoint = null;
        private bool _isDragging = false;
        private ClipboardItem? _dragTargetItem = null;

        private void ItemsListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ClearPressedListViewItem();
            _dragStartPoint = null;
            _isDragging = false;
            _dragTargetItem = null;
            
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while (dep != null && dep != ItemsListView)
            {
                if (dep is System.Windows.Controls.Primitives.ButtonBase ||
                    dep is System.Windows.Controls.Primitives.Thumb ||
                    dep is System.Windows.Controls.Primitives.ScrollBar)
                {
                    return;
                }
                
                if (dep is System.Windows.Controls.ListViewItem lvi && lvi.DataContext is ClipboardItem item)
                {
                    _dragStartPoint = e.GetPosition(null);
                    _dragTargetItem = item;
                    _pressedListViewItem = lvi;
                    lvi.SetValue(IsItemPressedProperty, true);
                    return;
                }

                dep = System.Windows.Media.VisualTreeHelper.GetParent(dep) ?? LogicalTreeHelper.GetParent(dep);
            }
        }

        private void ItemsListView_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
        {
            if (_viewModel.IsSelectionMode)
            {
                e.Handled = true;
            }
        }

        private void ItemsListView_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_viewModel.IsSelectionMode) return;

            if (e.LeftButton == MouseButtonState.Pressed && _dragStartPoint.HasValue && _dragTargetItem != null && !_isDragging)
            {
                System.Windows.Point mousePos = e.GetPosition(null);
                System.Windows.Vector diff = _dragStartPoint.Value - mousePos;

                double minDragX = Math.Max(SystemParameters.MinimumHorizontalDragDistance * 3.0, 16.0);
                double minDragY = Math.Max(SystemParameters.MinimumVerticalDragDistance * 3.0, 16.0);

                if (Math.Abs(diff.X) > minDragX || Math.Abs(diff.Y) > minDragY)
                {
                    ClearPressedListViewItem();
                    _isDragging = true;
                    var item = _dragTargetItem;
                    var dataObject = CustomClipboardManager.Services.DragDropHelper.CreateUniversalDataObject(item);

                    bool wasCancelled = false;
                    QueryContinueDragEventHandler qcHandler = (s, qe) =>
                    {
                        if (qe.EscapePressed)
                        {
                            wasCancelled = true;
                        }
                    };

                    ItemsListView.QueryContinueDrag += qcHandler;
                    try
                    {
                        var effect = System.Windows.DragDrop.DoDragDrop(
                            ItemsListView, 
                            dataObject, 
                            System.Windows.DragDropEffects.Copy | System.Windows.DragDropEffects.Move | System.Windows.DragDropEffects.Link);

                        // If not cancelled and not pinned, check where the drop occurred
                        if (!wasCancelled && !_viewModel.IsWindowPinned)
                        {
                            var dropPt = GetMousePosition();
                            var winRect = new System.Windows.Rect(this.Left, this.Top, this.ActualWidth, this.ActualHeight);
                            if (!winRect.Contains(dropPt))
                            {
                                this.Dispatcher.BeginInvoke(new Action(() => HideWindowAnimated()));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"DragDrop failed: {ex.Message}");
                    }
                    finally
                    {
                        ItemsListView.QueryContinueDrag -= qcHandler;
                        _isDragging = false;
                        _dragStartPoint = null;
                        _dragTargetItem = null;
                    }
                }
            }
        }

        private void SearchBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.UnicodeText) ||
                e.Data.GetDataPresent(DataFormats.Text) ||
                e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetDataPresent(DataFormats.Bitmap) ||
                e.Data.GetDataPresent("PNG"))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void SearchBox_PreviewDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            // 1. File drop
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    SearchBox.Text = System.IO.Path.GetFileName(files[0]);
                    SearchBox.Focus();
                    SearchBox.CaretIndex = SearchBox.Text.Length;
                    return;
                }
            }

            // 2. UnicodeText (also contains image file path if image was dragged)
            if (e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                string? text = e.Data.GetData(DataFormats.UnicodeText) as string;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (System.IO.File.Exists(text) || System.IO.Directory.Exists(text))
                    {
                        SearchBox.Text = System.IO.Path.GetFileName(text);
                    }
                    else
                    {
                        SearchBox.Text = text.Trim();
                    }
                    SearchBox.Focus();
                    SearchBox.CaretIndex = SearchBox.Text.Length;
                    return;
                }
            }

            // 3. Text fallback
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                string? text = e.Data.GetData(DataFormats.Text) as string;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    SearchBox.Text = text.Trim();
                    SearchBox.Focus();
                    SearchBox.CaretIndex = SearchBox.Text.Length;
                }
            }
        }

        private void ItemsListView_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Do not accept drop back onto list if currently dragging an item from this list
            if (_isDragging)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetDataPresent(DataFormats.UnicodeText) ||
                e.Data.GetDataPresent(DataFormats.Text) ||
                e.Data.GetDataPresent(DataFormats.Bitmap))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void ItemsListView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (_isDragging) return;
            e.Handled = true;

            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                    {
                        var item = new ClipboardItem();
                        CustomClipboardManager.Services.FileHelper.PopulateFileDetails(item, files);
                        _viewModel.AddItem(item);
                        ShowToast();
                        return;
                    }
                }
                else if (e.Data.GetDataPresent(DataFormats.Bitmap))
                {
                    if (e.Data.GetData(DataFormats.Bitmap) is BitmapSource bmp)
                    {
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.Image,
                            ImageContent = bmp,
                            Category = SmartCategory.Image
                        };
                        _viewModel.AddItem(item);
                        ShowToast();
                        return;
                    }
                }
                else if (e.Data.GetDataPresent(DataFormats.UnicodeText))
                {
                    string? text = e.Data.GetData(DataFormats.UnicodeText) as string;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.Text,
                            TextContent = text,
                            Category = DetermineCategory(text)
                        };
                        _viewModel.AddItem(item);
                        ShowToast();
                        return;
                    }
                }
            }
            catch { }
        }

        private void ItemsListView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ClearPressedListViewItem();

            if (!_isDragging && _dragTargetItem != null)
            {
                var itemToPaste = _dragTargetItem;
                _dragStartPoint = null;
                _dragTargetItem = null;
                _isDragging = false;

                if (_viewModel.IsSelectionMode)
                {
                    itemToPaste.IsSelected = !itemToPaste.IsSelected;
                    return;
                }

                _viewModel.PasteItemCommand.Execute(itemToPaste);
                return;
            }

            _dragStartPoint = null;
            _dragTargetItem = null;
            _isDragging = false;
        }

        private bool _isDocked;
        private string _dockedEdge = "";

        private void CheckAndDockToEdge()
        {
            if (!_viewModel.IsWindowPinned) return;

            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var workArea = screen.WorkingArea;

            double threshold = 35;
            double borderMargin = 0;

            if (this.Left + borderMargin <= workArea.Left + threshold)
            {
                _isDocked = true;
                _dockedEdge = "Left";
                SlideToDock();
            }
            else if (this.Left + this.Width - borderMargin >= workArea.Right - threshold)
            {
                _isDocked = true;
                _dockedEdge = "Right";
                SlideToDock();
            }
            else if (this.Top + borderMargin <= workArea.Top + threshold)
            {
                _isDocked = true;
                _dockedEdge = "Top";
                SlideToDock();
            }
            else
            {
                _isDocked = false;
                _dockedEdge = "";
            }
        }

        private void SlideToDock()
        {
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var workArea = screen.WorkingArea;
            double borderMargin = 0;
            double visibleCardSliver = 24;

            if (_dockedEdge == "Left")
            {
                double targetLeft = workArea.Left - this.Width + borderMargin + visibleCardSliver;
                AnimateWindowDouble(LeftProperty, targetLeft);
            }
            else if (_dockedEdge == "Right")
            {
                double targetLeft = workArea.Right - borderMargin - visibleCardSliver;
                AnimateWindowDouble(LeftProperty, targetLeft);
            }
            else if (_dockedEdge == "Top")
            {
                double targetTop = workArea.Top - this.Height + borderMargin + visibleCardSliver;
                AnimateWindowDouble(TopProperty, targetTop);
            }
        }

        private void SlideOutFromDock()
        {
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var workArea = screen.WorkingArea;
            double borderMargin = 0;

            if (_dockedEdge == "Left")
            {
                double targetLeft = workArea.Left - borderMargin;
                AnimateWindowDouble(LeftProperty, targetLeft);
            }
            else if (_dockedEdge == "Right")
            {
                double targetLeft = workArea.Right - this.Width + borderMargin;
                AnimateWindowDouble(LeftProperty, targetLeft);
            }
            else if (_dockedEdge == "Top")
            {
                double targetTop = workArea.Top - borderMargin;
                AnimateWindowDouble(TopProperty, targetTop);
            }
        }

        private void AnimateWindowDouble(DependencyProperty property, double toValue)
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            anim.Completed += (s, e) =>
            {
                this.BeginAnimation(property, null);
                this.SetValue(property, toValue);
            };
            this.BeginAnimation(property, anim);
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDocked)
            {
                SlideOutFromDock();
            }
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ClearPressedListViewItem();
            if (_isDocked)
            {
                SlideToDock();
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        public static System.Windows.Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new System.Windows.Point(w32Mouse.X, w32Mouse.Y);
        }
    }
}
