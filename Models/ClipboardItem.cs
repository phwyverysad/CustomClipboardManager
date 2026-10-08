using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace CustomClipboardManager.Models
{
    public enum ClipboardContentType
    {
        Text,
        Image,
        FileDropList,
        Audio,
        Other
    }

    public enum SmartCategory
    {
        Text,
        Link,
        ColorCode,
        Image,
        Files,
        Code,
        Audio,
        Others,
        Emoji
    }

    public class ClipboardItem : INotifyPropertyChanged
    {
        private string? _textContent;
        private BitmapSource? _imageContent;
        private bool _isPinned;
        private DateTime _timestamp = DateTime.Now;

        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TimeAgo));
                }
            }
        }

        public ClipboardContentType ContentType { get; set; }
        public SmartCategory Category { get; set; }

        public object? RawData { get; set; }

        private string? _originalTextContent;

        public string? OriginalTextContent
        {
            get => _originalTextContent ?? _textContent;
            set
            {
                if (_originalTextContent != value)
                {
                    _originalTextContent = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasTransformedText));
                }
            }
        }

        public bool HasTransformedText => !string.IsNullOrEmpty(_originalTextContent) && _originalTextContent != _textContent;

        public string? TextContent
        {
            get => _textContent;
            set
            {
                if (_textContent != value)
                {
                    if (_originalTextContent == null && _textContent != null)
                    {
                        _originalTextContent = _textContent;
                    }
                    _textContent = value;
                    if (ContentType == ClipboardContentType.Text)
                    {
                        RawData = value;
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayText));
                    OnPropertyChanged(nameof(MetadataText));
                    OnPropertyChanged(nameof(HasTransformedText));
                }
            }
        }

        public void ResetToOriginal()
        {
            if (!string.IsNullOrEmpty(_originalTextContent))
            {
                TextContent = _originalTextContent;
            }
        }

        public BitmapSource? ImageContent
        {
            get => _imageContent;
            set
            {
                _imageContent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MetadataText));
            }
        }

        // File drop list properties
        private string? _fileName;
        private string? _fileExtension;
        private string? _filePath;
        private string? _shortenedPath;
        private string? _fileSizeText;
        private BitmapSource? _fileIcon;
        private int _fileCount = 1;
        private System.Collections.Generic.List<string>? _filePaths;

        public System.Collections.Generic.List<string>? FilePaths
        {
            get => _filePaths;
            set
            {
                _filePaths = value;
                OnPropertyChanged();
            }
        }

        public string? FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public string? FileExtension
        {
            get => _fileExtension;
            set
            {
                _fileExtension = value;
                OnPropertyChanged();
            }
        }

        public string? FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }

        public string? ShortenedPath
        {
            get => _shortenedPath;
            set
            {
                _shortenedPath = value;
                OnPropertyChanged();
            }
        }

        public string? FileSizeText
        {
            get => _fileSizeText;
            set
            {
                _fileSizeText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MetadataText));
            }
        }

        public BitmapSource? FileIcon
        {
            get => _fileIcon;
            set
            {
                _fileIcon = value;
                OnPropertyChanged();
            }
        }

        public int FileCount
        {
            get => _fileCount;
            set
            {
                _fileCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MetadataText));
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                _isPinned = value;
                OnPropertyChanged();
            }
        }

        private bool _isSelected;

        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }


        public string CategoryBadgeText
        {
            get
            {
                var s = CustomClipboardManager.Services.I18n.Current;
                return Category switch
                {
                    SmartCategory.Text => s.BadgeText,
                    SmartCategory.Emoji => s.BadgeEmoji,
                    SmartCategory.Link => s.BadgeLink,
                    SmartCategory.ColorCode => s.BadgeColor,
                    SmartCategory.Image => s.BadgeImage,
                    SmartCategory.Files => s.BadgeFiles,
                    SmartCategory.Code => s.BadgeCode,
                    SmartCategory.Audio => s.BadgeAudio,
                    SmartCategory.Others => s.BadgeOthers,
                    _ => Category.ToString()
                };
            }
        }

        public string PinnedBadgeText => CustomClipboardManager.Services.I18n.Current.BadgePinned;

        // Display string for the UI (shortened)
        public string DisplayText
        {
            get
            {
                var s = CustomClipboardManager.Services.I18n.Current;
                if (ContentType == ClipboardContentType.Image)
                    return $"[{s.BadgeImage}]";
                if (ContentType == ClipboardContentType.FileDropList)
                {
                    if (!string.IsNullOrEmpty(FileName))
                    {
                        return FileName;
                    }
                    return $"[{s.BadgeFiles}]";
                }
                if (ContentType == ClipboardContentType.Audio)
                    return $"[{s.BadgeAudio}]";
                if (ContentType == ClipboardContentType.Other)
                    return $"[{s.BadgeOthers}]";

                if (string.IsNullOrEmpty(TextContent)) return "";
                
                var str = TextContent.Trim();
                return str.Length > 1000 ? str.Substring(0, 1000) + "..." : str;
            }
        }

        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - Timestamp;
                var s = CustomClipboardManager.Services.I18n.Current;
                if (span.TotalMinutes < 1) return s.TimeJustNow;
                if (span.TotalHours < 1) return string.Format(s.TimeMinutesAgoFormat, (int)span.TotalMinutes);
                if (span.TotalDays < 1) return string.Format(s.TimeHoursAgoFormat, (int)span.TotalHours);
                return string.Format(s.TimeDaysAgoFormat, (int)span.TotalDays);
            }
        }

        public string TimeDisplay
        {
            get
            {
                var span = DateTime.Now - Timestamp;
                var s = CustomClipboardManager.Services.I18n.Current;
                var culture = CustomClipboardManager.Services.I18n.Culture;
                if (span.TotalMinutes < 1)
                {
                    return $"{Timestamp:HH:mm} ({s.TimeJustNow})";
                }
                if (span.TotalMinutes < 60)
                {
                    return $"{Timestamp:HH:mm} ({string.Format(s.TimeMinutesAgoFormat, (int)span.TotalMinutes)})";
                }
                if (Timestamp.Date == DateTime.Today)
                {
                    return $"{Timestamp:HH:mm} ({string.Format(s.TimeHoursAgoFormat, (int)span.TotalHours)})";
                }
                if (Timestamp.Date == DateTime.Today.AddDays(-1))
                {
                    return $"{s.TimeYesterday} {Timestamp:HH:mm}";
                }
                if (Timestamp.Year == DateTime.Today.Year)
                {
                    return Timestamp.ToString("d MMM, HH:mm", culture);
                }
                return Timestamp.ToString("dd/MM/yyyy HH:mm", culture);
            }
        }

        public string FullTimestamp => Timestamp.ToString("dddd, MMMM d, yyyy • HH:mm:ss", CustomClipboardManager.Services.I18n.Culture);

        public string MetadataText
        {
            get
            {
                var s = CustomClipboardManager.Services.I18n.Current;
                if (ContentType == ClipboardContentType.Text)
                {
                    int len = _textContent != null ? _textContent.Length : 0;
                    return string.Format(s.MetadataCharsFormat, len);
                }
                if (ContentType == ClipboardContentType.Image && _imageContent != null)
                {
                    return $"{_imageContent.PixelWidth}x{_imageContent.PixelHeight} px";
                }
                if (ContentType == ClipboardContentType.FileDropList)
                {
                    if (FileCount > 1)
                    {
                        string filesText = string.Format(s.MetadataFilesFormat, FileCount);
                        return !string.IsNullOrEmpty(FileSizeText) 
                            ? $"{filesText} • {FileSizeText}" 
                            : filesText;
                    }
                    return FileSizeText ?? s.MetadataSingleFile;
                }
                return string.Empty;
            }
        }

        public void NotifyLanguageChanged()
        {
            OnPropertyChanged(nameof(CategoryBadgeText));
            OnPropertyChanged(nameof(PinnedBadgeText));
            OnPropertyChanged(nameof(MetadataText));
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(TimeAgo));
            OnPropertyChanged(nameof(TimeDisplay));
            OnPropertyChanged(nameof(FullTimestamp));
        }

        public void RefreshTimeAgo()
        {
            OnPropertyChanged(nameof(TimeAgo));
            OnPropertyChanged(nameof(TimeDisplay));
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
