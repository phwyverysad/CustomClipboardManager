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
        Others
    }

    public class ClipboardItem : INotifyPropertyChanged
    {
        private string? _textContent;
        private BitmapSource? _imageContent;
        private bool _isPinned;

        public Guid Id { get; } = Guid.NewGuid();
        public DateTime Timestamp { get; } = DateTime.Now;

        public ClipboardContentType ContentType { get; set; }
        public SmartCategory Category { get; set; }

        public object? RawData { get; set; }

        public string? TextContent
        {
            get => _textContent;
            set
            {
                _textContent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MetadataText));
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

        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                _isPinned = value;
                OnPropertyChanged();
            }
        }

        // Display string for the UI (shortened)
        public string DisplayText
        {
            get
            {
                if (ContentType == ClipboardContentType.Image)
                    return "[Image]";
                if (ContentType == ClipboardContentType.FileDropList)
                    return "[Files]";
                if (ContentType == ClipboardContentType.Audio)
                    return "[Audio]";
                if (ContentType == ClipboardContentType.Other)
                    return "[Other Data]";

                if (string.IsNullOrEmpty(TextContent)) return "";
                
                var str = TextContent.Replace("\n", " ").Replace("\r", "");
                return str.Length > 200 ? str.Substring(0, 200) + "..." : str;
            }
        }

        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - Timestamp;
                if (span.TotalMinutes < 1) return "Just now";
                if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
                return $"{(int)span.TotalDays}d ago";
            }
        }

        public string MetadataText
        {
            get
            {
                if (ContentType == ClipboardContentType.Text)
                {
                    return _textContent != null ? $"{_textContent.Length} chars" : "0 chars";
                }
                if (ContentType == ClipboardContentType.Image && _imageContent != null)
                {
                    return $"{_imageContent.PixelWidth}x{_imageContent.PixelHeight} px";
                }
                return string.Empty;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
