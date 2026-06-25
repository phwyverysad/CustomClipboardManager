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
        private ObservableCollection<ClipboardItem> _clipboardItems;
        private ICollectionView _clipboardItemsView;
        private string _searchQuery;
        private bool _isWindowPinned;
        private string? _lastPastedText;
        private bool _isDarkMode = true;

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

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged(nameof(SearchQuery));
                ClipboardItemsView?.Refresh();
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

        public ICommand DeleteItemCommand { get; }
        public ICommand PinItemCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand PasteItemCommand { get; }
        public ICommand CopyOnlyCommand { get; }
        public ICommand TogglePinWindowCommand { get; }
        public ICommand FilterTypeCommand { get; }
        public ICommand RequestConfirmClearAllCommand { get; }
        public ICommand TransformUpperCommand { get; }
        public ICommand TransformLowerCommand { get; }
        public ICommand TransformStripCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public Action? RequestClose { get; set; }
        public Action ShowToastNotification;
        public Action RequestConfirmClearAll;

        public MainViewModel()
        {
            RequestConfirmClearAllCommand = new RelayCommand(_ => RequestConfirmClearAll?.Invoke());
            
            ClipboardItems = new ObservableCollection<ClipboardItem>();
            LoadPersistedData();

            ClipboardItemsView = CollectionViewSource.GetDefaultView(ClipboardItems);
            ClipboardItemsView.Filter = FilterItems;
            
            // Removed Grouping so Pinned doesn't show up with a group header in All
            
            ClipboardItemsView.SortDescriptions.Add(new SortDescription("Timestamp", ListSortDirection.Descending));

            DeleteItemCommand = new RelayCommand(ExecuteDeleteItem);
            PinItemCommand = new RelayCommand(ExecutePinItem);
            ClearAllCommand = new RelayCommand(ExecuteClearAll);
            PasteItemCommand = new RelayCommand(ExecutePasteItem);
            CopyOnlyCommand = new RelayCommand(ExecuteCopyOnly);
            TogglePinWindowCommand = new RelayCommand(ExecuteTogglePinWindow);
            FilterTypeCommand = new RelayCommand(ExecuteFilterType);
            TransformUpperCommand = new RelayCommand(ExecuteTransformUpper);
            TransformLowerCommand = new RelayCommand(ExecuteTransformLower);
            TransformStripCommand = new RelayCommand(ExecuteTransformStrip);
            ToggleThemeCommand = new RelayCommand(ExecuteToggleTheme);

            ClipboardItems.CollectionChanged += (s, e) => SavePersistedData();
        }

        private void LoadPersistedData()
        {
            var data = ClipboardDataService.LoadData();
            IsWindowPinned = data.IsWindowPinned;
            foreach (var item in data.Items)
            {
                // Reconstruct ClipboardItem
                var ci = new ClipboardItem
                {
                    ContentType = item.ContentType,
                    Category = item.Category,
                    TextContent = item.TextContent,
                    IsPinned = item.IsPinned,
                    // Note: Timestamp and Id might be reset here or we can add setters if needed.
                    // Let's just use what's generated for now, it's fine for simple history.
                };
                ClipboardItems.Add(ci);
            }
        }

        public void SavePersistedData()
        {
            ClipboardDataService.SaveData(IsWindowPinned, ClipboardItems);
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
                return item.DisplayText?.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return false;
        }

        private void ExecuteFilterType(object obj)
        {
            if (obj is string filterType)
            {
                CurrentFilterType = filterType;
            }
        }

        private void ExecuteTransformUpper(object obj)
        {
            if (obj is ClipboardItem item && item.TextContent != null)
            {
                item.TextContent = item.TextContent.ToUpper();
                SavePersistedData();
            }
        }

        private void ExecuteTransformLower(object obj)
        {
            if (obj is ClipboardItem item && item.TextContent != null)
            {
                item.TextContent = item.TextContent.ToLower();
                SavePersistedData();
            }
        }

        private void ExecuteTransformStrip(object obj)
        {
            if (obj is ClipboardItem item && item.TextContent != null)
            {
                item.TextContent = System.Text.RegularExpressions.Regex.Replace(item.TextContent, @"\s+", " ").Trim();
                SavePersistedData();
            }
        }

        public void AddItem(ClipboardItem item)
        {
            if (item.ContentType == ClipboardContentType.Text && item.TextContent == _lastPastedText)
            {
                _lastPastedText = null;
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Remove old duplicate if it exists
                if (item.ContentType == ClipboardContentType.Text)
                {
                    var existing = ClipboardItems.FirstOrDefault(x => x.ContentType == ClipboardContentType.Text && x.TextContent == item.TextContent);
                    if (existing != null)
                    {
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
                
                ClipboardItems.Insert(0, item);
                ClipboardItemsView.Refresh();
            });
        }

        private void ExecuteDeleteItem(object parameter)
        {
            if (parameter is ClipboardItem item)
            {
                ClipboardItems.Remove(item);
                ClipboardItemsView.Refresh();
            }
        }

        private void ExecuteTogglePinWindow(object parameter)
        {
            IsWindowPinned = !IsWindowPinned;
            SavePersistedData();
        }

        private void ExecuteToggleTheme(object obj)
        {
            IsDarkMode = !IsDarkMode;
            
            var app = System.Windows.Application.Current;
            app.Resources.MergedDictionaries.Clear();
            
            var newTheme = new System.Windows.ResourceDictionary();
            newTheme.Source = new Uri(IsDarkMode ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative);
            app.Resources.MergedDictionaries.Add(newTheme);
        }

        private void ExecutePinItem(object parameter)
        {
            if (parameter is ClipboardItem item)
            {
                item.IsPinned = !item.IsPinned;
                ClipboardItemsView.Refresh();
                SavePersistedData();
            }
        }

        private void ExecuteClearAll(object parameter)
        {
            RequestConfirmClearAll?.Invoke();
        }

        public void ConfirmClearAll()
        {
            var unpinned = ClipboardItems.Where(x => !x.IsPinned).ToList();
            foreach (var item in unpinned)
            {
                ClipboardItems.Remove(item);
            }
            ClipboardItemsView.Refresh();
        }

        private void ExecuteCopyOnly(object parameter)
        {
            if (parameter is ClipboardItem item)
            {
                if (item.RawData != null)
                {
                    if (item.RawData is System.IO.Stream stream)
                        System.Windows.Clipboard.SetAudio(stream);
                    else
                        System.Windows.Clipboard.SetDataObject(item.RawData);
                }
                else if (item.ContentType == ClipboardContentType.Text)
                {
                    System.Windows.Clipboard.SetText(item.TextContent);
                }
                else if (item.ContentType == ClipboardContentType.Image && item.ImageContent != null)
                {
                    System.Windows.Clipboard.SetImage(item.ImageContent);
                }
                
                ShowToastNotification?.Invoke();
                
                if (!IsWindowPinned)
                {
                    RequestClose?.Invoke();
                }
            }
        }

        private async void ExecutePasteItem(object parameter)
        {
            if (parameter is ClipboardItem item)
            {
                // Hide window first to return focus, which also helps prevent focus stealing race conditions
                RequestClose?.Invoke();

                if (item.RawData != null)
                {
                    if (item.RawData is System.IO.Stream stream)
                        System.Windows.Clipboard.SetAudio(stream);
                    else
                        System.Windows.Clipboard.SetDataObject(item.RawData);
                }
                else if (item.ContentType == ClipboardContentType.Text)
                {
                    _lastPastedText = item.TextContent;
                    System.Windows.Clipboard.SetText(item.TextContent);
                }
                else if (item.ContentType == ClipboardContentType.Image && item.ImageContent != null)
                {
                    System.Windows.Clipboard.SetImage(item.ImageContent);
                }

                await AutoPasteService.PasteAsync();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
