using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace CustomClipboardManager.Models
{
    public class DetailProperty
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsCopyable { get; set; } = true;
        public string? CopyValue { get; set; }
    }

    public class FileListItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string SizeText { get; set; } = string.Empty;
        public BitmapSource? Icon { get; set; }
        public bool IsDirectory { get; set; }
    }

    public class ClipboardPreviewInfo : INotifyPropertyChanged
    {
        public ClipboardItem SourceItem { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string BadgeText { get; set; } = string.Empty;
        public Brush BadgeBackground { get; set; } = new SolidColorBrush(Color.FromArgb(0x26, 0x80, 0x80, 0x80));
        public Brush BadgeBorder { get; set; } = new SolidColorBrush(Color.FromArgb(0x4D, 0x80, 0x80, 0x80));

        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPreviewTabSelected));
                    OnPropertyChanged(nameof(IsDetailsTabSelected));
                }
            }
        }

        public bool IsPreviewTabSelected => _selectedTabIndex == 0;
        public bool IsDetailsTabSelected => _selectedTabIndex == 1;

        // Image Preview
        public bool HasImagePreview { get; set; }
        public BitmapSource? ImageSource { get; set; }
        public string? ImageResolutionText { get; set; }
        public string? ImageAspectRatioText { get; set; }

        // File Preview
        public bool HasFilePreview { get; set; }
        public BitmapSource? LargeFileIcon { get; set; }
        public string? FileName { get; set; }
        public string? FileExtension { get; set; }
        public string? FilePath { get; set; }
        public string? FileSizeText { get; set; }
        public bool HasFileTextContent { get; set; }
        public string? FileTextContent { get; set; }
        public bool IsMultipleFiles { get; set; }
        public List<FileListItem>? MultipleFilesList { get; set; }

        // Color Preview
        public bool HasColorPreview { get; set; }
        public Brush? ColorBrush { get; set; }
        public string? ColorHex { get; set; }
        public string? ColorRgb { get; set; }
        public string? ColorHsl { get; set; }
        public string? ColorHsv { get; set; }

        // Text Preview
        public bool HasTextPreview { get; set; }
        public string? TextContent { get; set; }
        public bool IsCode { get; set; }

        // Structured Components Formatting
        private bool _hasStructuredComponents;
        public bool HasStructuredComponents
        {
            get => _hasStructuredComponents;
            set { _hasStructuredComponents = value; OnPropertyChanged(); }
        }

        private string? _componentSummaryText;
        public string? ComponentSummaryText
        {
            get => _componentSummaryText;
            set { _componentSummaryText = value; OnPropertyChanged(); }
        }

        public string? RawTextContent { get; set; }
        public string? FormattedTextContent { get; set; }
        public string? CleanTextContent { get; set; }
        public bool HasCleanText => !string.IsNullOrEmpty(CleanTextContent) && CleanTextContent != FormattedTextContent;

        private int _textFormatMode = 0; // 0 = Formatted, 1 = Raw, 2 = Clean
        public int TextFormatMode
        {
            get => _textFormatMode;
            set
            {
                if (_textFormatMode != value)
                {
                    _textFormatMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ActiveTextContent));
                    OnPropertyChanged(nameof(IsFormattedMode));
                    OnPropertyChanged(nameof(IsRawMode));
                    OnPropertyChanged(nameof(IsCleanMode));
                }
            }
        }

        public bool IsFormattedMode => _textFormatMode == 0;
        public bool IsRawMode => _textFormatMode == 1;
        public bool IsCleanMode => _textFormatMode == 2;

        public string? ActiveTextContent
        {
            get
            {
                if (!HasStructuredComponents) return TextContent;
                return _textFormatMode switch
                {
                    1 => RawTextContent ?? TextContent,
                    2 => CleanTextContent ?? FormattedTextContent ?? TextContent,
                    _ => FormattedTextContent ?? TextContent
                };
            }
        }

        // Link Preview
        public bool HasLinkPreview { get; set; }
        public string? LinkUrl { get; set; }
        public string? LinkDomain { get; set; }

        // Detailed Metadata Key-Value pairs
        public List<DetailProperty> DetailsList { get; set; } = new List<DetailProperty>();
        public bool HasDetailsTab => DetailsList != null && DetailsList.Count > 0;

        // Action flags
        public bool CanOpenFile { get; set; }
        public bool CanOpenFolder { get; set; }
        public bool CanOpenLink { get; set; }
        public bool CanSaveImage { get; set; }

        public ClipboardPreviewInfo(ClipboardItem sourceItem)
        {
            SourceItem = sourceItem;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
