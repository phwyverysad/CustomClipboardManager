using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using CustomClipboardManager.Core;
using CustomClipboardManager.Models;
using CustomClipboardManager.ViewModels;
using System.Windows.Media.Imaging;

namespace CustomClipboardManager
{
    public partial class MainWindow : Window
    {
        private GlobalKeyboardHook _keyboardHook;
        private ClipboardMonitor _clipboardMonitor;
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

        private WinEventDelegate _foregroundDelegate;
        private IntPtr _foregroundHook;
        private IntPtr _previousHwnd;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            _viewModel.RequestClose = () =>
            {
                _isPasting = true; // prevent clipboard monitor from catching our own paste if applicable
                
                this.Hide(); // Hide instantly so the previous app gets focus back before SendInput fires

                if (_previousHwnd != IntPtr.Zero)
                {
                    SetForegroundWindow(_previousHwnd);
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

            this.DataContext = _viewModel;

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            this.Deactivated += MainWindow_Deactivated;
            this.SourceInitialized += MainWindow_SourceInitialized;
            
            this.MouseEnter += Window_MouseEnter;
            this.MouseLeave += Window_MouseLeave;

            _clipboardMonitor = new ClipboardMonitor(this);
            _clipboardMonitor.ClipboardChanged += ClipboardMonitor_ClipboardChanged;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            _foregroundDelegate = new WinEventDelegate(WinEventProc);
            _foregroundHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _foregroundDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            IntPtr ourHwnd = helper.Handle;

            // If the new foreground window is NOT our window, save it.
            if (hwnd != ourHwnd && hwnd != IntPtr.Zero)
            {
                _previousHwnd = hwnd;
                
                // If our window is currently visible, not pinned, and the foreground window changed, hide it.
                if (this.IsVisible && !_viewModel.IsWindowPinned)
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        HideWindowAnimated();
                    }));
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Hide window on startup
            this.Hide();

            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.OnHotkeyPressed += KeyboardHook_OnHotkeyPressed;
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            // Auto hide when clicking outside, unless pinned
            if (!_viewModel.IsWindowPinned)
            {
                HideWindowAnimated();
            }
        }

        private void KeyboardHook_OnHotkeyPressed(object sender, EventArgs e)
        {
            // Capture the currently active window before we show ours
            _previousHwnd = GetForegroundWindow();

            // Get mouse position
            var point = GetMousePosition();
            
            // Get screen bounds containing the mouse
            var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)point.X, (int)point.Y));
            var workArea = screen.WorkingArea;

            // Calculate requested position
            double desiredLeft = point.X - (this.Width / 2);
            double desiredTop = point.Y - (this.Height / 2);

            // Clamp to screen bounds
            if (desiredLeft < workArea.Left) desiredLeft = workArea.Left;
            if (desiredTop < workArea.Top) desiredTop = workArea.Top;
            if (desiredLeft + this.Width > workArea.Right) desiredLeft = workArea.Right - this.Width;
            if (desiredTop + this.Height > workArea.Bottom) desiredTop = workArea.Bottom - this.Height;

            this.Left = desiredLeft;
            this.Top = desiredTop;

            this.Show();
            _isPasting = false;

            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            ForceForegroundWindow(hwnd);

            ShowWindowAnimated();
        }

        private void ForceForegroundWindow(IntPtr hwnd)
        {
            IntPtr foregroundHwnd = GetForegroundWindow();
            if (foregroundHwnd == hwnd) return;

            uint foregroundThreadId = GetWindowThreadProcessId(foregroundHwnd, IntPtr.Zero);
            uint currentThreadId = GetCurrentThreadId();

            if (foregroundThreadId != 0 && currentThreadId != 0 && foregroundThreadId != currentThreadId)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, true);
                SetForegroundWindow(hwnd);
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.Activate();
                    this.Focus();
                    SearchBox.Focus();
                    SearchBox.SelectAll();
                }));
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
            else
            {
                SetForegroundWindow(hwnd);
                this.Activate();
                this.Focus();
                SearchBox.Focus();
                SearchBox.SelectAll();
            }
        }

        private void ShowToast()
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                AutoReverse = true,
                RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(1)
            };
            // ToastGrid is defined in XAML
            var toastGrid = (FrameworkElement)this.FindName("ToastGrid");
            if (toastGrid != null)
            {
                toastGrid.BeginAnimation(UIElement.OpacityProperty, anim);
            }
        }

        private void ShowClearConfirm()
        {
            var confirmGrid = (FrameworkElement)this.FindName("ConfirmGrid");
            if (confirmGrid != null) confirmGrid.Visibility = Visibility.Visible;
        }

        private void ConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            var confirmGrid = (FrameworkElement)this.FindName("ConfirmGrid");
            if (confirmGrid != null) confirmGrid.Visibility = Visibility.Collapsed;
            _viewModel.ConfirmClearAll();
        }

        private void ConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            var confirmGrid = (FrameworkElement)this.FindName("ConfirmGrid");
            if (confirmGrid != null) confirmGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowWindowAnimated()
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };

            var slideAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 15,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };

            var transform = new System.Windows.Media.TranslateTransform();
            MainBorder.RenderTransform = transform;
            
            transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideAnim);
            MainBorder.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void HideWindowAnimated()
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };

            var slideAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 10,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };

            var transform = MainBorder.RenderTransform as System.Windows.Media.TranslateTransform;
            if (transform == null)
            {
                transform = new System.Windows.Media.TranslateTransform();
                MainBorder.RenderTransform = transform;
            }

            transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideAnim);
            anim.Completed += (s, e) => this.Hide();
            MainBorder.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void FilterScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var scrollViewer = (System.Windows.Controls.ScrollViewer)sender;
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        private async void ClipboardMonitor_ClipboardChanged(object sender, EventArgs e)
        {
            if (_isPasting)
            {
                _isPasting = false;
                return;
            }

            // Debounce to prevent double image copy bug
            if ((DateTime.Now - _lastClipboardEventTime).TotalMilliseconds < 200)
            {
                return;
            }
            _lastClipboardEventTime = DateTime.Now;

            // Add a slight delay to ensure the application copying the data has released the clipboard lock
            await System.Threading.Tasks.Task.Delay(50);

            try
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    var text = System.Windows.Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.Text,
                            TextContent = text,
                            Category = DetermineCategory(text)
                        };
                        _viewModel.AddItem(item);
                    }
                }
                else if (System.Windows.Clipboard.ContainsImage())
                {
                    var image = System.Windows.Clipboard.GetImage();
                    if (image != null)
                    {
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.Image,
                            ImageContent = image,
                            Category = SmartCategory.Image
                        };
                        _viewModel.AddItem(item);
                    }
                }
                else if (System.Windows.Clipboard.ContainsFileDropList())
                {
                    var files = System.Windows.Clipboard.GetFileDropList();
                    if (files != null && files.Count > 0)
                    {
                        var sb = new System.Text.StringBuilder();
                        if (files.Count == 1)
                        {
                            string f = files[0] ?? "";
                            bool isDir = System.IO.Directory.Exists(f);
                            if (isDir)
                            {
                                sb.AppendLine($"Folder: {System.IO.Path.GetFileName(f)}");
                                sb.AppendLine($"Path: {f}");
                            }
                            else
                            {
                                var info = new System.IO.FileInfo(f);
                                var sizeStr = info.Exists ? $"{info.Length / 1024} KB" : "Unknown Size";
                                sb.AppendLine($"File: {info.Name}");
                                sb.AppendLine($"Size: {sizeStr}");
                                sb.AppendLine($"Path: {f}");
                            }
                        }
                        else
                        {
                            sb.AppendLine($"{files.Count} items selected");
                            foreach (var f in files)
                            {
                                if (f == null) continue;
                                bool isDir = System.IO.Directory.Exists(f);
                                if (isDir)
                                {
                                    sb.AppendLine($"- [Folder] {System.IO.Path.GetFileName(f)}");
                                }
                                else
                                {
                                    var info = new System.IO.FileInfo(f);
                                    var sizeStr = info.Exists ? $"{info.Length / 1024} KB" : "Unknown";
                                    sb.AppendLine($"- {info.Name} ({sizeStr})");
                                }
                            }
                        }
                        
                        var text = sb.ToString().TrimEnd();
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.FileDropList,
                            TextContent = text,
                            Category = SmartCategory.Files
                        };
                        _viewModel.AddItem(item);
                    }
                }
                else if (System.Windows.Clipboard.ContainsAudio())
                {
                    var stream = System.Windows.Clipboard.GetAudioStream();
                    if (stream != null)
                    {
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.Audio,
                            TextContent = "[Audio Stream]",
                            Category = SmartCategory.Audio,
                            RawData = stream
                        };
                        _viewModel.AddItem(item);
                    }
                }
                else
                {
                    // Fallback to "Other"
                    var dataObj = System.Windows.Clipboard.GetDataObject();
                    if (dataObj != null)
                    {
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
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Clipboard might be locked, wait and try one more time
                await System.Threading.Tasks.Task.Delay(100);
                try
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        var text = System.Windows.Clipboard.GetText();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            _viewModel.AddItem(new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = text, Category = DetermineCategory(text) });
                        }
                    }
                }
                catch { }
            }
        }

        private SmartCategory DetermineCategory(string text)
        {
            if (Uri.IsWellFormedUriString(text, UriKind.Absolute)) return SmartCategory.Link;
            if (text.StartsWith("#") && (text.Length == 4 || text.Length == 7 || text.Length == 9)) return SmartCategory.ColorCode;
            
            int codeScore = 0;
            if (text.StartsWith("```")) codeScore += 3;
            if (text.Contains("{") && text.Contains("}")) codeScore++;
            if (text.Contains(";") && text.Contains("(")) codeScore++;
            if (text.Contains("using ") || text.Contains("import ") || text.Contains("require(")) codeScore += 2;
            if (text.Contains("public ") || text.Contains("private ") || text.Contains("class ")) codeScore++;
            if (text.Contains("function ") || text.Contains("const ") || text.Contains("let ") || text.Contains("var ")) codeScore++;
            if (text.Contains("=>") || text.Contains("==") || text.Contains("===") || text.Contains("!=")) codeScore++;
            if ((text.Contains("<") && text.Contains("/>")) || text.Contains("</")) codeScore++;
            
            if (codeScore >= 3) return SmartCategory.Code;

            return SmartCategory.Text;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_foregroundHook != IntPtr.Zero)
            {
                UnhookWinEvent(_foregroundHook);
            }
            _keyboardHook?.Dispose();
            _clipboardMonitor?.Dispose();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HideWindowAnimated();
            }
            else if (e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                int index = e.Key - Key.D1;
                if (index < _viewModel.ClipboardItemsView.Cast<ClipboardItem>().Count())
                {
                    var item = _viewModel.ClipboardItemsView.Cast<ClipboardItem>().ElementAt(index);
                    _viewModel.PasteItemCommand.Execute(item);
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Check if the click is outside the MainBorder (i.e. on the transparent window margin)
                var pos = e.GetPosition(MainBorder);
                if (pos.X < 0 || pos.Y < 0 || pos.X > MainBorder.ActualWidth || pos.Y > MainBorder.ActualHeight)
                {
                    if (!_viewModel.IsWindowPinned)
                    {
                        HideWindowAnimated();
                        return;
                    }
                }

                try
                {
                    this.DragMove();
                    CheckAndDockToEdge();
                }
                catch (InvalidOperationException)
                {
                    // DragMove can throw if left button is released too quickly
                }
            }
        }

        private void TrafficRed_Click(object sender, RoutedEventArgs e)
        {
            HideWindowAnimated();
        }


        private void SearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var listView = (System.Windows.Controls.ListView)this.FindName("ItemsListView");
            if (listView == null) return;

            if (e.Key == Key.Up)
            {
                e.Handled = true;
                if (listView.SelectedIndex > 0)
                {
                    listView.SelectedIndex--;
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
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (listView.SelectedItem is ClipboardItem item)
                {
                    _viewModel.PasteItemCommand.Execute(item);
                }
            }
        }

        private System.Windows.Point? _dragStartPoint = null;

        private void ItemsListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = null;
            
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while (dep != null && dep != ItemsListView)
            {
                if (dep is System.Windows.Controls.Button || 
                    dep is System.Windows.Controls.Primitives.ButtonBase ||
                    dep is System.Windows.Controls.Primitives.Thumb ||
                    dep is System.Windows.Controls.Primitives.ScrollBar)
                {
                    return;
                }
                
                if (dep is System.Windows.Media.Visual || dep is System.Windows.Media.Media3D.Visual3D)
                {
                    dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
                }
                else
                {
                    dep = LogicalTreeHelper.GetParent(dep);
                }
            }
            
            _dragStartPoint = e.GetPosition(null);
        }

        private void ItemsListView_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _dragStartPoint.HasValue)
            {
                System.Windows.Point mousePos = e.GetPosition(null);
                System.Windows.Vector diff = _dragStartPoint.Value - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var listView = sender as System.Windows.Controls.ListView;
                    if (listView != null && listView.SelectedItem is ClipboardItem item)
                    {
                        var dataObject = new System.Windows.DataObject();
                        if (item.ContentType == ClipboardContentType.Text)
                        {
                            dataObject.SetData(System.Windows.DataFormats.Text, item.TextContent);
                            dataObject.SetData(System.Windows.DataFormats.UnicodeText, item.TextContent);
                        }
                        else if (item.ContentType == ClipboardContentType.Image)
                        {
                            dataObject.SetData(System.Windows.DataFormats.Bitmap, item.ImageContent);
                        }
                        
                        _dragStartPoint = null;
                        
                        try
                        {
                            System.Windows.DragDrop.DoDragDrop(listView, dataObject, System.Windows.DragDropEffects.Copy);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.Message);
                        }
                    }
                }
            }
        }

        private bool _isDocked;
        private string _dockedEdge = "";

        private void CheckAndDockToEdge()
        {
            if (!_viewModel.IsWindowPinned) return;

            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var workArea = screen.WorkingArea;

            double threshold = 20;

            if (this.Left <= workArea.Left + threshold)
            {
                _isDocked = true;
                _dockedEdge = "Left";
                SlideToDock();
            }
            else if (this.Left + this.Width >= workArea.Right - threshold)
            {
                _isDocked = true;
                _dockedEdge = "Right";
                SlideToDock();
            }
            else if (this.Top <= workArea.Top + threshold)
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
            double visibleSliver = 15;

            if (_dockedEdge == "Left") this.Left = workArea.Left - this.Width + visibleSliver;
            else if (_dockedEdge == "Right") this.Left = workArea.Right - visibleSliver;
            else if (_dockedEdge == "Top") this.Top = workArea.Top - this.Height + visibleSliver;
        }

        private void SlideOutFromDock()
        {
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var workArea = screen.WorkingArea;

            if (_dockedEdge == "Left") this.Left = workArea.Left;
            else if (_dockedEdge == "Right") this.Left = workArea.Right - this.Width;
            else if (_dockedEdge == "Top") this.Top = workArea.Top;
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
