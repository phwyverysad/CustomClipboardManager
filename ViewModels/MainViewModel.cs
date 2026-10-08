using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CustomClipboardManager.Core;
using CustomClipboardManager.Models;
using System.Windows.Media.Imaging;
using CustomClipboardManager.Services;

namespace CustomClipboardManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ClipboardItem> _clipboardItems = new ObservableCollection<ClipboardItem>();
        private ICollectionView _clipboardItemsView;
        private string _searchQuery = string.Empty;
        private bool _isWindowPinned;
        private bool _isDarkMode = true;

        // Multi-type anti-loop state
        private ClipboardContentType? _lastPastedType;
        private string? _lastPastedText;
        private string? _lastPastedFilePath;
        private int _lastPastedFileCount;
        private int _lastPastedImageW;
        private int _lastPastedImageH;
        private DateTime _lastPastedUtc = DateTime.MinValue;
        private System.Windows.Threading.DispatcherTimer? _timeAgoTimer;
        private bool _isServiceRunning = true;

        public bool IsServiceRunning
        {
            get => _isServiceRunning;
            set
            {
                if (_isServiceRunning != value)
                {
                    _isServiceRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ServiceStatusText));
                    OnPropertyChanged(nameof(ServiceTooltipText));
                    OnPropertyChanged(nameof(ServiceStatusColor));
                    OnPropertyChanged(nameof(ServiceStatusBrush));
                }
            }
        }

        public string ServiceStatusText => _isServiceRunning ? Strings.StatusCaptureActive : Strings.StatusCapturePaused;

        public string ServiceTooltipText => _isServiceRunning
            ? Strings.TooltipCaptureActive
            : Strings.TooltipCapturePaused;

        public string ServiceStatusColor => _isServiceRunning ? "#30D158" : "#FF9F0A";

        public System.Windows.Media.Brush ServiceStatusBrush => _isServiceRunning
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0xD1, 0x58))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x9F, 0x0A));

        // Screen Capture Protection
        private bool _isScreenCaptureProtectionEnabled = false;
        public bool IsScreenCaptureProtectionEnabled
        {
            get => _isScreenCaptureProtectionEnabled;
            set
            {
                if (_isScreenCaptureProtectionEnabled != value)
                {
                    _isScreenCaptureProtectionEnabled = value;
                    OnPropertyChanged();
                    RequestApplyScreenCaptureProtection?.Invoke(value);
                    SavePersistedData();
                }
            }
        }
        public Action<bool>? RequestApplyScreenCaptureProtection { get; set; }
        public ICommand ToggleScreenCaptureProtectionCommand { get; }

        // Efficiency Mode
        private bool _isEfficiencyModeEnabled = true;
        public bool IsEfficiencyModeEnabled
        {
            get => _isEfficiencyModeEnabled;
            set
            {
                if (_isEfficiencyModeEnabled != value)
                {
                    _isEfficiencyModeEnabled = value;
                    OnPropertyChanged();
                    if (value)
                    {
                        Core.EfficiencyModeHelper.EnableEfficiencyMode();
                    }
                    else
                    {
                        Core.EfficiencyModeHelper.DisableEfficiencyMode();
                    }
                    SavePersistedData();
                }
            }
        }
        public ICommand ToggleEfficiencyModeCommand { get; }

        // Windows Clipboard History (Win+V)
        private bool _isWindowsClipboardHistoryEnabled = true;
        public bool IsWindowsClipboardHistoryEnabled
        {
            get => _isWindowsClipboardHistoryEnabled;
            set
            {
                if (_isWindowsClipboardHistoryEnabled != value)
                {
                    _isWindowsClipboardHistoryEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowsClipboardStatusHint));
                }
            }
        }

        private bool? _userWindowsClipboardHistoryOverride = null;
        public bool? UserWindowsClipboardHistoryOverride
        {
            get => _userWindowsClipboardHistoryOverride;
            set
            {
                _userWindowsClipboardHistoryOverride = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WindowsClipboardStatusHint));
                SavePersistedData();
            }
        }

        public string WindowsClipboardStatusHint => _isWindowsClipboardHistoryEnabled 
            ? (_userWindowsClipboardHistoryOverride.HasValue ? "Manual: Enabled" : "Auto: Enabled")
            : (_userWindowsClipboardHistoryOverride.HasValue ? "Manual: Disabled" : "Auto-disabled for Win+V");

        public ICommand ToggleWindowsClipboardHistoryCommand { get; }
        public Action? RequestPreparePaste { get; set; }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ClipboardItem> ClipboardItems
        {
            get => _clipboardItems;
            set
            {
                _clipboardItems = value;
                OnPropertyChanged();
            }
        }

        public ICollectionView ClipboardItemsView
        {
            get => _clipboardItemsView;
            set
            {
                _clipboardItemsView = value;
                OnPropertyChanged();
            }
        }

        private string _currentFilterType = "All";

        private System.Windows.Threading.DispatcherTimer? _searchDebounceTimer;

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged(nameof(SearchQuery));

                    _searchDebounceTimer?.Stop();
                    _searchDebounceTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(40)
                    };
                    _searchDebounceTimer.Tick += (s, e) =>
                    {
                        _searchDebounceTimer?.Stop();
                        ClipboardItemsView?.Refresh();
                        UpdateSelectionState();
                    };
                    _searchDebounceTimer.Start();
                }
            }
        }

        public void ResetSearch()
        {
            _searchDebounceTimer?.Stop();
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                _searchQuery = string.Empty;
                OnPropertyChanged(nameof(SearchQuery));
                ClipboardItemsView?.Refresh();
                UpdateSelectionState();
            }
        }

        public string CurrentFilterType
        {
            get => _currentFilterType;
            set
            {
                _currentFilterType = value;
                OnPropertyChanged(nameof(CurrentFilterType));
                ClipboardItemsView?.Refresh();
                UpdateSelectionState();
            }
        }

        public bool IsWindowPinned
        {
            get => _isWindowPinned;
            set
            {
                _isWindowPinned = value;
                OnPropertyChanged();
            }
        }

        private ClipboardPreviewInfo? _previewInfo;
        public ClipboardPreviewInfo? PreviewInfo
        {
            get => _previewInfo;
            set
            {
                _previewInfo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPreviewOpen));
            }
        }

        public bool IsPreviewOpen => _previewInfo != null;

        public CustomClipboardManager.Services.AppStrings Strings => CustomClipboardManager.Services.I18n.Current;

        // Selection Mode (Batch Delete) properties
        private bool _isBatchUpdating = false;
        private bool _isSelectionMode;
        public bool IsSelectionMode
        {
            get => _isSelectionMode;
            set
            {
                if (_isSelectionMode != value)
                {
                    _isSelectionMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectionModeHeaderTooltip));
                    if (!_isSelectionMode)
                    {
                        _isBatchUpdating = true;
                        try
                        {
                            foreach (var item in _clipboardItems)
                            {
                                item.IsSelected = false;
                            }
                        }
                        finally
                        {
                            _isBatchUpdating = false;
                        }
                    }
                    UpdateSelectionState();
                }
            }
        }

        public bool? IsSelectAll
        {
            get
            {
                var visibleItems = ClipboardItemsView?.Cast<ClipboardItem>().ToList();
                if (visibleItems == null || visibleItems.Count == 0) return false;

                int selectedCount = visibleItems.Count(x => x.IsSelected);
                if (selectedCount == 0) return false;
                if (selectedCount == visibleItems.Count) return true;
                return null;
            }
            set
            {
                var visibleItems = ClipboardItemsView?.Cast<ClipboardItem>().ToList();
                if (visibleItems == null || visibleItems.Count == 0) return;

                bool targetState = value == true;
                _isBatchUpdating = true;
                try
                {
                    foreach (var item in visibleItems)
                    {
                        item.IsSelected = targetState;
                    }
                }
                finally
                {
                    _isBatchUpdating = false;
                }
                UpdateSelectionState();
            }
        }

        public int SelectedCount => _clipboardItems.Count(x => x.IsSelected);
        public bool HasSelectedItems => SelectedCount > 0;
        public string DeleteButtonText => SelectedCount > 0 ? $"{Strings.DeleteSelected} ({SelectedCount})" : Strings.DeleteSelected;
        public string SelectionModeHeaderTooltip => IsSelectionMode ? $"{Strings.Cancel} (Esc)" : Strings.MenuSelect;
        public string SelectedCountSummary
        {
            get
            {
                var visibleItems = ClipboardItemsView?.Cast<ClipboardItem>().ToList();
                int selected = visibleItems?.Count(x => x.IsSelected) ?? 0;
                return string.Format(Strings.SelectionCountFormat, selected);
            }
        }

        private string _toastMessage = "Copied to clipboard!";
        public string ToastMessage
        {
            get => _toastMessage;
            set
            {
                if (_toastMessage != value)
                {
                    _toastMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand DeleteItemCommand { get; }
        public ICommand PinItemCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand PasteItemCommand { get; }
        public ICommand CopyOnlyCommand { get; }
        public ICommand TogglePinWindowCommand { get; }
        public ICommand FilterTypeCommand { get; }
        public ICommand RequestConfirmClearAllCommand { get; }
        public ICommand ToggleSelectionModeCommand { get; }
        public ICommand ExitSelectionModeCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand TransformUpperCommand { get; }
        public ICommand TransformLowerCommand { get; }
        public ICommand TransformStripCommand { get; }
        public ICommand TransformResetCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ToggleServiceCommand { get; }
        public ICommand ExitApplicationCommand { get; }
        public ICommand PreviewItemCommand { get; }
        public ICommand ClosePreviewCommand { get; }
        public ICommand SelectPreviewTabCommand { get; }
        public ICommand OpenPreviewTargetCommand { get; }
        public ICommand RevealPreviewFolderCommand { get; }
        public ICommand SavePreviewImageCommand { get; }
        public ICommand CopyPreviewContentCommand { get; }
        public ICommand CopyDetailValueCommand { get; }
        public ICommand SetTextFormatModeCommand { get; }
        public ICommand OrganizeComponentsCommand { get; }

        public Action? RequestClose { get; set; }
        public Action? ShowToastNotification { get; set; }
        public Action? RequestConfirmClearAll { get; set; }
        public Func<IntPtr>? GetTargetWindowHwnd { get; set; }
        public Func<IntPtr>? GetTargetFocusHwnd { get; set; }
        public Action<ClipboardPreviewInfo>? RequestShowPreview { get; set; }
        public Action? RequestHidePreview { get; set; }
        public Action<BitmapSource>? RequestSavePreviewImage { get; set; }

        public MainViewModel()
        {
            RequestConfirmClearAllCommand = new RelayCommand(_ => RequestConfirmClearAll?.Invoke());

            LoadPersistedData();
            foreach (var ci in ClipboardItems)
            {
                HookItemEvents(ci);
            }
            UpdateSelectionState();

            CustomClipboardManager.Services.I18n.LanguageChanged += () =>
            {
                OnPropertyChanged(nameof(Strings));
                OnPropertyChanged(nameof(DeleteButtonText));
                OnPropertyChanged(nameof(SelectionModeHeaderTooltip));
                OnPropertyChanged(nameof(SelectedCountSummary));
                foreach (var item in ClipboardItems)
                {
                    item.NotifyLanguageChanged();
                }
            };

            _clipboardItemsView = CollectionViewSource.GetDefaultView(ClipboardItems);
            _clipboardItemsView.Filter = FilterItems;
            _clipboardItemsView.SortDescriptions.Add(new SortDescription("Timestamp", ListSortDirection.Descending));

            DeleteItemCommand = new RelayCommand(ExecuteDeleteItem);
            PinItemCommand = new RelayCommand(ExecutePinItem);
            ClearAllCommand = new RelayCommand(ExecuteClearAll);
            ToggleSelectionModeCommand = new RelayCommand(_ => ToggleSelectionMode(), _ => ClipboardItems.Count > 0 || IsSelectionMode);
            ExitSelectionModeCommand = new RelayCommand(_ => IsSelectionMode = false);
            DeleteSelectedCommand = new RelayCommand(ExecuteDeleteSelected, _ => HasSelectedItems);
            SelectAllCommand = new RelayCommand(ExecuteSelectAll);
            PasteItemCommand = new RelayCommand(ExecutePasteItem);
            CopyOnlyCommand = new RelayCommand(ExecuteCopyOnly);
            TogglePinWindowCommand = new RelayCommand(ExecuteTogglePinWindow);
            FilterTypeCommand = new RelayCommand(ExecuteFilterType);
            TransformUpperCommand = new RelayCommand(ExecuteTransformUpper, CanExecuteTransform);
            TransformLowerCommand = new RelayCommand(ExecuteTransformLower, CanExecuteTransform);
            TransformStripCommand = new RelayCommand(ExecuteTransformStrip, CanExecuteTransform);
            TransformResetCommand = new RelayCommand(ExecuteTransformReset, CanExecuteTransform);
            ToggleThemeCommand = new RelayCommand(ExecuteToggleTheme);
            ToggleServiceCommand = new RelayCommand(async _ => await ExecuteToggleService());
            ExitApplicationCommand = new RelayCommand(_ => ExecuteExitApplication());
            PreviewItemCommand = new RelayCommand(ExecutePreviewItem);
            ClosePreviewCommand = new RelayCommand(ExecuteClosePreview);
            SelectPreviewTabCommand = new RelayCommand(ExecuteSelectPreviewTab);
            OpenPreviewTargetCommand = new RelayCommand(ExecuteOpenPreviewTarget);
            RevealPreviewFolderCommand = new RelayCommand(ExecuteRevealPreviewFolder);
            SavePreviewImageCommand = new RelayCommand(ExecuteSavePreviewImage);
            CopyPreviewContentCommand = new RelayCommand(ExecuteCopyPreviewContent);
            CopyDetailValueCommand = new RelayCommand(ExecuteCopyDetailValue);
            SetTextFormatModeCommand = new RelayCommand(ExecuteSetTextFormatMode);
            OrganizeComponentsCommand = new RelayCommand(ExecuteOrganizeComponents, CanExecuteTransform);
            ToggleScreenCaptureProtectionCommand = new RelayCommand(_ => IsScreenCaptureProtectionEnabled = !IsScreenCaptureProtectionEnabled);
            ToggleEfficiencyModeCommand = new RelayCommand(_ => IsEfficiencyModeEnabled = !IsEfficiencyModeEnabled);
            ToggleWindowsClipboardHistoryCommand = new RelayCommand(_ => SetWindowsClipboardHistoryPreference(!IsWindowsClipboardHistoryEnabled));

            // Timer to dynamically update relative timestamps
            _timeAgoTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _timeAgoTimer.Tick += (s, e) =>
            {
                foreach (var ci in ClipboardItems)
                {
                    ci.RefreshTimeAgo();
                }
            };
            _timeAgoTimer.Start();

            ClipboardItems.CollectionChanged += (s, e) =>
            {
                if (_isBatchUpdating) return;

                if (e.NewItems != null)
                {
                    foreach (ClipboardItem item in e.NewItems)
                    {
                        HookItemEvents(item);
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (ClipboardItem item in e.OldItems)
                    {
                        UnhookItemEvents(item);
                    }
                }
                SavePersistedData();
                UpdateSelectionState();
            };
        }

        private void LoadPersistedData()
        {
            var data = ClipboardDataService.LoadData();
            IsWindowPinned = data.IsWindowPinned;
            IsDarkMode = data.IsDarkMode ?? (!WindowsColorHelper.GetIsWindowsLightTheme());
            ApplyTheme(IsDarkMode);

            _isScreenCaptureProtectionEnabled = data.IsScreenCaptureProtectionEnabled;
            _isEfficiencyModeEnabled = data.IsEfficiencyModeEnabled;
            _userWindowsClipboardHistoryOverride = data.UserWindowsClipboardHistoryOverride;

            if (_userWindowsClipboardHistoryOverride.HasValue)
            {
                _isWindowsClipboardHistoryEnabled = _userWindowsClipboardHistoryOverride.Value;
                Core.WindowsClipboardHelper.SetWindowsClipboardHistoryEnabled(_isWindowsClipboardHistoryEnabled);
            }
            else
            {
                _isWindowsClipboardHistoryEnabled = Core.WindowsClipboardHelper.IsWindowsClipboardHistoryEnabled();
            }

            foreach (var item in data.Items)
            {
                var ci = new ClipboardItem
                {
                    Id = item.Id != Guid.Empty ? item.Id : Guid.NewGuid(),
                    Timestamp = item.Timestamp != default ? item.Timestamp : DateTime.Now,
                    ContentType = item.ContentType,
                    Category = item.Category,
                    TextContent = item.TextContent,
                    OriginalTextContent = item.OriginalTextContent ?? item.TextContent,
                    IsPinned = item.IsPinned,
                };

                if (item.ContentType == ClipboardContentType.Image)
                {
                    ci.ImageContent = ClipboardDataService.LoadCachedImage(item.ImageFileName);
                }
                else if (item.ContentType == ClipboardContentType.FileDropList && item.FilePaths != null && item.FilePaths.Count > 0)
                {
                    FileHelper.PopulateFileDetails(ci, item.FilePaths);
                }

                HookItemEvents(ci);
                ClipboardItems.Add(ci);
            }
        }

        public void SavePersistedData(bool synchronous = false)
        {
            if (_isBatchUpdating) return;
            var itemsSnapshot = ClipboardItems.ToList();
            if (synchronous)
            {
                ClipboardDataService.SaveData(
                    IsWindowPinned, 
                    IsDarkMode, 
                    itemsSnapshot,
                    IsScreenCaptureProtectionEnabled,
                    IsEfficiencyModeEnabled,
                    UserWindowsClipboardHistoryOverride);
            }
            else
            {
                Task.Run(() =>
                {
                    ClipboardDataService.SaveData(
                        IsWindowPinned, 
                        IsDarkMode, 
                        itemsSnapshot,
                        IsScreenCaptureProtectionEnabled,
                        IsEfficiencyModeEnabled,
                        UserWindowsClipboardHistoryOverride);
                });
            }
        }

        public void SetWindowsClipboardHistoryPreference(bool enabled)
        {
            UserWindowsClipboardHistoryOverride = enabled;
            Core.WindowsClipboardHelper.SetWindowsClipboardHistoryEnabled(enabled);
            IsWindowsClipboardHistoryEnabled = enabled;
        }

        public void SyncWindowsClipboardWithHotkey(Core.HotkeyConfig config)
        {
            bool active = Core.WindowsClipboardHelper.SyncWithHotkey(config, UserWindowsClipboardHistoryOverride);
            IsWindowsClipboardHistoryEnabled = active;
        }

        public void StartTimeAgoTimer()
        {
            _timeAgoTimer?.Start();
        }

        public void StopTimeAgoTimer()
        {
            _timeAgoTimer?.Stop();
        }

        private bool FilterItems(object obj)
        {
            if (obj is ClipboardItem item)
            {
                bool typeMatch = true;
                if (_currentFilterType == "Text") typeMatch = item.ContentType == ClipboardContentType.Text;
                else if (_currentFilterType == "Image") typeMatch = item.ContentType == ClipboardContentType.Image;
                else if (_currentFilterType == "Link") typeMatch = item.Category == SmartCategory.Link;
                else if (_currentFilterType == "Pinned") typeMatch = item.IsPinned;
                else if (_currentFilterType == "Files") typeMatch = item.ContentType == ClipboardContentType.FileDropList;
                else if (_currentFilterType == "ColorCode") typeMatch = item.Category == SmartCategory.ColorCode;
                else if (_currentFilterType == "Code") typeMatch = item.Category == SmartCategory.Code;
                else if (_currentFilterType == "Others") typeMatch = item.Category == SmartCategory.Others || item.Category == SmartCategory.Audio;

                if (!typeMatch) return false;

                if (string.IsNullOrWhiteSpace(SearchQuery)) return true;

                if (item.ContentType == ClipboardContentType.FileDropList)
                {
                    if (item.FileName?.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (item.FilePath?.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (item.ShortenedPath?.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }

                return item.DisplayText?.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return false;
        }

        private void ExecuteFilterType(object? obj)
        {
            if (obj is string filterType)
            {
                CurrentFilterType = filterType;
            }
        }

        private void ExecuteTransformUpper(object? obj)
        {
            if (obj is ClipboardItem item && item.TextContent != null)
            {
                if (string.IsNullOrEmpty(item.OriginalTextContent))
                {
                    item.OriginalTextContent = item.TextContent;
                }
                bool wasPinned = item.IsPinned;
                string? original = item.OriginalTextContent;

                item.TextContent = item.TextContent.ToUpper();
                item.IsPinned = wasPinned;
                if (!string.IsNullOrEmpty(original))
                {
                    item.OriginalTextContent = original;
                }

                SetItemToClipboard(item);
                ClipboardItemsView.Refresh();
                SavePersistedData();
            }
        }

        private void ExecuteTransformLower(object? obj)
        {
            if (obj is ClipboardItem item && item.TextContent != null)
            {
                if (string.IsNullOrEmpty(item.OriginalTextContent))
                {
                    item.OriginalTextContent = item.TextContent;
                }
                bool wasPinned = item.IsPinned;
                string? original = item.OriginalTextContent;

                item.TextContent = item.TextContent.ToLower();
                item.IsPinned = wasPinned;
                if (!string.IsNullOrEmpty(original))
                {
                    item.OriginalTextContent = original;
                }

                SetItemToClipboard(item);
                ClipboardItemsView.Refresh();
                SavePersistedData();
            }
        }

        private void ExecuteTransformStrip(object? obj)
        {
            if (obj is ClipboardItem item && item.TextContent != null)
            {
                if (string.IsNullOrEmpty(item.OriginalTextContent))
                {
                    item.OriginalTextContent = item.TextContent;
                }
                bool wasPinned = item.IsPinned;
                string? original = item.OriginalTextContent;

                string text = item.TextContent;
                if (text.Contains("\n"))
                {
                    var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                    .Select(l => System.Text.RegularExpressions.Regex.Replace(l.Trim(), @"[ \t]+", " "));
                    item.TextContent = string.Join(Environment.NewLine, lines).Trim();
                }
                else
                {
                    item.TextContent = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
                }

                item.IsPinned = wasPinned;
                if (!string.IsNullOrEmpty(original))
                {
                    item.OriginalTextContent = original;
                }

                SetItemToClipboard(item);
                ClipboardItemsView.Refresh();
                SavePersistedData();
            }
        }

        private void ExecuteTransformReset(object? obj)
        {
            if (obj is ClipboardItem item && item.TextContent != null)
            {
                bool wasPinned = item.IsPinned;
                item.ResetToOriginal();
                item.IsPinned = wasPinned;
                SetItemToClipboard(item);
                ClipboardItemsView.Refresh();
                SavePersistedData();
            }
        }

        private void ExecuteOrganizeComponents(object? obj)
        {
            if (obj is ClipboardItem item && item.TextContent != null)
            {
                if (string.IsNullOrEmpty(item.OriginalTextContent))
                {
                    item.OriginalTextContent = item.TextContent;
                }
                bool wasPinned = item.IsPinned;
                string? original = item.OriginalTextContent;

                var analysis = Services.TextComponentFormatter.Analyze(item.TextContent);
                if (analysis.HasComponents)
                {
                    item.TextContent = analysis.FormattedText;
                }
                else
                {
                    item.TextContent = Services.TextComponentFormatter.Format(item.TextContent);
                }

                item.IsPinned = wasPinned;
                if (!string.IsNullOrEmpty(original))
                {
                    item.OriginalTextContent = original;
                }

                SetItemToClipboard(item);
                ClipboardItemsView.Refresh();
                SavePersistedData();
                ToastMessage = Strings.ToastOrganized;
                ShowToastNotification?.Invoke();
            }
        }

        private bool CanExecuteTransform(object? obj)
        {
            return obj is ClipboardItem item &&
                   item.ContentType == ClipboardContentType.Text &&
                   !string.IsNullOrWhiteSpace(item.TextContent);
        }

        public void AddItem(ClipboardItem item)
        {
            bool isRecentPaste = (DateTime.UtcNow - _lastPastedUtc).TotalSeconds < 2.5;
            if (isRecentPaste)
            {
                if (item.ContentType == ClipboardContentType.Text && item.TextContent == _lastPastedText)
                {
                    _lastPastedText = null;
                    return;
                }
                if (item.ContentType == ClipboardContentType.Image && item.ImageContent != null &&
                    item.ImageContent.PixelWidth == _lastPastedImageW &&
                    item.ImageContent.PixelHeight == _lastPastedImageH)
                {
                    return;
                }
                if (item.ContentType == ClipboardContentType.FileDropList &&
                    item.FilePath == _lastPastedFilePath &&
                    item.FileCount == _lastPastedFileCount)
                {
                    _lastPastedFilePath = null;
                    return;
                }
            }

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                // Remove old duplicate if it exists, preserving pinned state and original content
                if (item.ContentType == ClipboardContentType.Text)
                {
                    var existing = ClipboardItems.FirstOrDefault(x => x.ContentType == ClipboardContentType.Text &&
                        (x.TextContent == item.TextContent || (!string.IsNullOrEmpty(x.OriginalTextContent) && x.OriginalTextContent == item.TextContent)));
                    if (existing != null)
                    {
                        if (existing.IsPinned)
                        {
                            item.IsPinned = true;
                        }
                        if (!string.IsNullOrEmpty(existing.OriginalTextContent) && existing.OriginalTextContent != existing.TextContent)
                        {
                            item.OriginalTextContent = existing.OriginalTextContent;
                        }
                        ClipboardItems.Remove(existing);
                    }
                }
                else if (item.ContentType == ClipboardContentType.Image)
                {
                    var lastItem = ClipboardItems.FirstOrDefault();
                    if (lastItem != null && lastItem.ContentType == ClipboardContentType.Image && lastItem.ImageContent != null && item.ImageContent != null)
                    {
                        if ((item.Timestamp - lastItem.Timestamp).TotalSeconds < 3 && 
                            lastItem.ImageContent.PixelWidth == item.ImageContent.PixelWidth && 
                            lastItem.ImageContent.PixelHeight == item.ImageContent.PixelHeight)
                        {
                            return; // Ignore duplicate image (Snipping Tool bug)
                        }
                    }
                }
                else if (item.ContentType == ClipboardContentType.FileDropList)
                {
                    var existing = ClipboardItems.FirstOrDefault(x => x.ContentType == ClipboardContentType.FileDropList && x.FilePath == item.FilePath && x.FileCount == item.FileCount);
                    if (existing != null)
                    {
                        if (existing.IsPinned)
                        {
                            item.IsPinned = true;
                        }
                        ClipboardItems.Remove(existing);
                    }
                }
                
                HookItemEvents(item);
                ClipboardItems.Insert(0, item);
                if (!string.IsNullOrWhiteSpace(SearchQuery) || _currentFilterType != "All")
                {
                    ClipboardItemsView?.Refresh();
                }
            });
        }

        private void ExecuteDeleteItem(object? parameter)
        {
            if (parameter is ClipboardItem item)
            {
                ClipboardItems.Remove(item);
                SavePersistedData();
                ClipboardItemsView.Refresh();
                ToastMessage = "Deleted 1 item";
                ShowToastNotification?.Invoke();
            }
        }

        private void ExecuteTogglePinWindow(object? parameter)
        {
            IsWindowPinned = !IsWindowPinned;
            SavePersistedData();
        }

        public void ApplyTheme(bool isDark)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            try
            {
                var uiThemes = app.Resources.MergedDictionaries.OfType<Wpf.Ui.Markup.ThemesDictionary>().FirstOrDefault();
                if (uiThemes != null)
                {
                    uiThemes.Theme = isDark ? Wpf.Ui.Appearance.ApplicationTheme.Dark : Wpf.Ui.Appearance.ApplicationTheme.Light;
                }
            }
            catch { }

            if (app.MainWindow != null)
            {
                app.MainWindow.Background = System.Windows.Media.Brushes.Transparent;
            }
            foreach (System.Windows.Window win in app.Windows)
            {
                win.Background = System.Windows.Media.Brushes.Transparent;
            }

            try
            {
                var targetSource = new Uri(isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative);
                var existing = app.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme.xaml"));
                if (existing != null)
                {
                    app.Resources.MergedDictionaries.Remove(existing);
                }
                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = targetSource });

                // Dynamically apply the user's native Windows Accent Color to buttons, borders, and selection highlights
                WindowsColorHelper.ApplyWindowsColors(app.Resources, isDark);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load theme resource: {ex.Message}");
            }
        }

        private void ExecuteToggleTheme(object? obj)
        {
            IsDarkMode = !IsDarkMode;
            ApplyTheme(IsDarkMode);
            SavePersistedData();
        }

        public async void RefreshServiceStatus()
        {
            bool active = await System.Threading.Tasks.Task.Run(() => ServiceManager.IsServiceActive());
            if (_isServiceRunning != active)
            {
                IsServiceRunning = active;
            }
        }

        private async Task ExecuteToggleService()
        {
            await ServiceManager.ToggleServiceAsync();
            RefreshServiceStatus();
        }

        private void ExecuteExitApplication()
        {
            try
            {
                SavePersistedData(synchronous: true);
                ServiceManager.StopServiceSync();
            }
            catch { }
            System.Windows.Application.Current?.Shutdown();
        }

        private void ExecutePinItem(object? parameter)
        {
            if (parameter is ClipboardItem item)
            {
                item.IsPinned = !item.IsPinned;
                ClipboardItemsView.Refresh();
                SavePersistedData();
            }
        }

        private void ExecuteClearAll(object? parameter)
        {
            var unpinned = ClipboardItems.Where(x => !x.IsPinned).ToList();
            int count = unpinned.Count;
            if (count == 0) return;

            _isBatchUpdating = true;
            try
            {
                foreach (var item in unpinned)
                {
                    UnhookItemEvents(item);
                }

                var pinned = ClipboardItems.Where(x => x.IsPinned).ToList();
                ClipboardItems.Clear();
                foreach (var item in pinned)
                {
                    ClipboardItems.Add(item);
                }
            }
            finally
            {
                _isBatchUpdating = false;
            }

            SavePersistedData();
            ClipboardItemsView.Refresh();
            UpdateSelectionState();

            if (count > 0)
            {
                ToastMessage = Strings.ToastAllCleared;
                ShowToastNotification?.Invoke();
            }
        }

        public void ConfirmClearAll()
        {
            ExecuteClearAll(null);
        }

        public void ToggleSelectionMode()
        {
            IsSelectionMode = !IsSelectionMode;
        }

        private void ExecuteSelectAll(object? parameter)
        {
            bool shouldSelect = IsSelectAll != true;
            IsSelectAll = shouldSelect;
        }

        private void ExecuteDeleteSelected(object? parameter)
        {
            var selectedItems = ClipboardItems.Where(x => x.IsSelected).ToList();
            if (selectedItems.Count == 0) return;

            int count = selectedItems.Count;
            _isBatchUpdating = true;
            try
            {
                foreach (var item in selectedItems)
                {
                    UnhookItemEvents(item);
                }

                if (selectedItems.Count == ClipboardItems.Count)
                {
                    ClipboardItems.Clear();
                }
                else
                {
                    var selectedSet = new HashSet<ClipboardItem>(selectedItems);
                    var remaining = ClipboardItems.Where(x => !selectedSet.Contains(x)).ToList();

                    ClipboardItems.Clear();
                    foreach (var item in remaining)
                    {
                        ClipboardItems.Add(item);
                    }
                }
            }
            finally
            {
                _isBatchUpdating = false;
            }

            SavePersistedData();
            ClipboardItemsView.Refresh();
            IsSelectionMode = false;
            UpdateSelectionState();

            ToastMessage = Strings.ToastDeleted;
            ShowToastNotification?.Invoke();
        }

        private void HookItemEvents(ClipboardItem item)
        {
            item.PropertyChanged -= Item_PropertyChanged;
            item.PropertyChanged += Item_PropertyChanged;
        }

        private void UnhookItemEvents(ClipboardItem item)
        {
            item.PropertyChanged -= Item_PropertyChanged;
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isBatchUpdating) return;

            if (e.PropertyName == nameof(ClipboardItem.IsSelected))
            {
                UpdateSelectionState();
            }
            else if (e.PropertyName == nameof(ClipboardItem.IsPinned))
            {
                SavePersistedData();
            }
        }

        public void UpdateSelectionState()
        {
            Action update = () =>
            {
                OnPropertyChanged(nameof(IsSelectAll));
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(HasSelectedItems));
                OnPropertyChanged(nameof(DeleteButtonText));
                OnPropertyChanged(nameof(SelectedCountSummary));
                CommandManager.InvalidateRequerySuggested();
            };

            if (System.Windows.Application.Current != null &&
                !System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(update);
            }
            else
            {
                update();
            }
        }

        private async void ExecuteCopyOnly(object? parameter)
        {
            if (parameter is ClipboardItem item)
            {
                RequestPreparePaste?.Invoke();
                await SetItemToClipboardAsync(item);

                ToastMessage = Strings.ToastCopied;
                ShowToastNotification?.Invoke();

                if (!IsWindowPinned)
                {
                    RequestClose?.Invoke();
                }
            }
        }

        private async void ExecutePasteItem(object? parameter)
        {
            if (parameter is ClipboardItem item)
            {
                // CRITICAL: Signal paste immediately to prevent ClipboardMonitor from self-capturing
                RequestPreparePaste?.Invoke();

                // If active preview is showing this item with custom format mode, use the preview's active text
                if (PreviewInfo != null && PreviewInfo.SourceItem == item && PreviewInfo.HasTextPreview && !string.IsNullOrEmpty(PreviewInfo.ActiveTextContent))
                {
                    _lastPastedType = ClipboardContentType.Text;
                    _lastPastedUtc = DateTime.UtcNow;
                    _lastPastedText = PreviewInfo.ActiveTextContent;
                    await SetClipboardWithRetryAsync(() => System.Windows.Clipboard.SetText(PreviewInfo.ActiveTextContent));
                }
                else
                {
                    // 1. Populate clipboard immediately with async non-blocking retry protection
                    await SetItemToClipboardAsync(item);
                }

                // 2. Hide window (if unpinned) and restore target window focus
                RequestClose?.Invoke();

                // 3. Dispatch auto-paste keystroke (Ctrl+V) with exact target control focus preservation
                IntPtr targetHwnd = GetTargetWindowHwnd?.Invoke() ?? IntPtr.Zero;
                IntPtr focusHwnd = GetTargetFocusHwnd?.Invoke() ?? IntPtr.Zero;
                await AutoPasteService.PasteAsync(targetHwnd, focusHwnd);
            }
        }

        private void ExecutePreviewItem(object? parameter)
        {
            ClipboardItem? target = parameter as ClipboardItem;
            if (target == null && parameter is Guid id)
            {
                target = ClipboardItems.FirstOrDefault(x => x.Id == id);
            }
            if (target == null)
            {
                target = ClipboardItemsView?.CurrentItem as ClipboardItem ?? ClipboardItems.FirstOrDefault();
            }

            if (target != null)
            {
                PreviewInfo = ClipboardPreviewBuilder.Build(target);
                RequestShowPreview?.Invoke(PreviewInfo);
            }
        }

        private void ExecuteClosePreview(object? parameter)
        {
            RequestHidePreview?.Invoke();
            PreviewInfo = null;
        }

        private void ExecuteSelectPreviewTab(object? parameter)
        {
            if (PreviewInfo != null && parameter != null && int.TryParse(parameter.ToString(), out int idx))
            {
                PreviewInfo.SelectedTabIndex = idx;
            }
        }

        private void ExecuteOpenPreviewTarget(object? parameter)
        {
            if (PreviewInfo != null)
            {
                if (PreviewInfo.HasFilePreview && !string.IsNullOrEmpty(PreviewInfo.FilePath))
                {
                    FileHelper.OpenFile(PreviewInfo.FilePath);
                }
                else if (PreviewInfo.HasLinkPreview && !string.IsNullOrEmpty(PreviewInfo.LinkUrl))
                {
                    FileHelper.OpenFile(PreviewInfo.LinkUrl);
                }
            }
        }

        private void ExecuteRevealPreviewFolder(object? parameter)
        {
            if (PreviewInfo != null && !string.IsNullOrEmpty(PreviewInfo.FilePath))
            {
                FileHelper.OpenInExplorer(PreviewInfo.FilePath);
            }
        }

        private void ExecuteSavePreviewImage(object? parameter)
        {
            if (PreviewInfo?.ImageSource != null)
            {
                RequestSavePreviewImage?.Invoke(PreviewInfo.ImageSource);
            }
        }

        private void ExecuteCopyPreviewContent(object? parameter)
        {
            if (PreviewInfo != null)
            {
                if (PreviewInfo.HasTextPreview && !string.IsNullOrEmpty(PreviewInfo.ActiveTextContent))
                {
                    _lastPastedType = ClipboardContentType.Text;
                    _lastPastedUtc = DateTime.UtcNow;
                    _lastPastedText = PreviewInfo.ActiveTextContent;
                    SetClipboardWithRetry(() => System.Windows.Clipboard.SetText(PreviewInfo.ActiveTextContent));
                    ToastMessage = "Copied to clipboard!";
                    ShowToastNotification?.Invoke();
                    return;
                }

                if (PreviewInfo.SourceItem != null)
                {
                    SetItemToClipboard(PreviewInfo.SourceItem);
                    ToastMessage = "Copied to clipboard!";
                    ShowToastNotification?.Invoke();
                }
            }
        }

        private void ExecuteSetTextFormatMode(object? parameter)
        {
            if (PreviewInfo != null && parameter != null && int.TryParse(parameter.ToString(), out int mode))
            {
                PreviewInfo.TextFormatMode = mode;
            }
        }

        private void ExecuteCopyDetailValue(object? parameter)
        {
            if (parameter is DetailProperty prop)
            {
                string textToCopy = prop.CopyValue ?? prop.Value;
                if (!string.IsNullOrEmpty(textToCopy))
                {
                    SetClipboardWithRetry(() => System.Windows.Clipboard.SetText(textToCopy));
                    ToastMessage = "Copied to clipboard!";
                    ShowToastNotification?.Invoke();
                }
            }
            else if (parameter is string text && !string.IsNullOrEmpty(text))
            {
                SetClipboardWithRetry(() => System.Windows.Clipboard.SetText(text));
                ToastMessage = "Copied to clipboard!";
                ShowToastNotification?.Invoke();
            }
        }

        public async Task SetItemToClipboardAsync(ClipboardItem item)
        {
            _lastPastedType = item.ContentType;
            _lastPastedUtc = DateTime.UtcNow;

            if (item.ContentType == ClipboardContentType.FileDropList)
            {
                _lastPastedFilePath = item.FilePath;
                _lastPastedFileCount = item.FileCount;
                var sc = new System.Collections.Specialized.StringCollection();
                if (item.RawData is System.Collections.Specialized.StringCollection existingSc)
                {
                    sc = existingSc;
                }
                else if (item.RawData is System.Collections.Generic.IEnumerable<string> paths)
                {
                    sc.AddRange(paths.ToArray());
                }
                if (sc.Count > 0)
                {
                    await SetClipboardWithRetryAsync(() => System.Windows.Clipboard.SetFileDropList(sc));
                }
            }
            else if (item.ContentType == ClipboardContentType.Text && item.TextContent != null)
            {
                _lastPastedText = item.TextContent;
                await SetClipboardWithRetryAsync(() => System.Windows.Clipboard.SetText(item.TextContent));
            }
            else if (item.ContentType == ClipboardContentType.Image && item.ImageContent != null)
            {
                _lastPastedImageW = item.ImageContent.PixelWidth;
                _lastPastedImageH = item.ImageContent.PixelHeight;
                await SetClipboardWithRetryAsync(() => System.Windows.Clipboard.SetImage(item.ImageContent));
            }
            else if (item.RawData != null)
            {
                if (item.RawData is byte[] audioBytes)
                {
                    using var ms = new System.IO.MemoryStream(audioBytes);
                    await SetClipboardWithRetryAsync(() => System.Windows.Clipboard.SetAudio(ms));
                }
                else if (item.RawData is System.IO.Stream stream)
                {
                    if (stream.CanSeek) stream.Position = 0;
                    await SetClipboardWithRetryAsync(() => System.Windows.Clipboard.SetAudio(stream));
                }
                else
                {
                    await SetClipboardWithRetryAsync(() => System.Windows.Clipboard.SetDataObject(item.RawData, false));
                }
            }
        }

        public void SetItemToClipboard(ClipboardItem item)
        {
            SetItemToClipboardAsync(item).GetAwaiter().GetResult();
        }

        public static async Task<bool> SetClipboardWithRetryAsync(Action action, int maxRetries = 5, int delayMs = 30)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    action();
                    return true;
                }
                catch (System.Runtime.InteropServices.COMException) when (i < maxRetries - 1)
                {
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Clipboard set error: {ex.Message}");
                    if (i == maxRetries - 1) return false;
                    await Task.Delay(delayMs);
                }
            }
            return false;
        }

        public static bool SetClipboardWithRetry(Action action, int maxRetries = 5, int delayMs = 30)
        {
            return SetClipboardWithRetryAsync(action, maxRetries, delayMs).GetAwaiter().GetResult();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
