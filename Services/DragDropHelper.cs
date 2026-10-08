using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using CustomClipboardManager.Models;
using DataObject = System.Windows.DataObject;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;

namespace CustomClipboardManager.Services
{
    public static class DragDropHelper
    {
        /// <summary>
        /// Creates a universal, multi-format DataObject that supports dropping onto ANY text box,
        /// chat input (Discord, Slack, Teams), rich editor (Word, Outlook, OneNote), browser, or folder.
        /// </summary>
        public static DataObject CreateUniversalDataObject(ClipboardItem item)
        {
            var dataObject = new DataObject();
            if (item == null) return dataObject;

            // 1. Text content
            if (!string.IsNullOrEmpty(item.TextContent))
            {
                PopulateTextFormats(dataObject, item.TextContent, item.Category);
            }

            // 2. Image content
            if (item.ContentType == ClipboardContentType.Image && item.ImageContent != null)
            {
                PopulateImageFormats(dataObject, item.ImageContent, item.Id);
            }

            // 3. File drop list
            if (item.ContentType == ClipboardContentType.FileDropList || (item.FilePaths != null && item.FilePaths.Count > 0))
            {
                var paths = item.FilePaths != null && item.FilePaths.Count > 0 ? item.FilePaths : new List<string>();
                if (paths.Count == 0 && !string.IsNullOrEmpty(item.FilePath))
                {
                    paths = new List<string> { item.FilePath };
                }
                if (paths.Count > 0)
                {
                    PopulateFileFormats(dataObject, paths);
                }
            }

            // 4. Raw fallback if present and not already handled
            if (item.RawData != null && !(item.RawData is string) && !(item.RawData is BitmapSource))
            {
                try
                {
                    dataObject.SetData(item.RawData);
                }
                catch { }
            }

            return dataObject;
        }

        public static void PopulateTextFormats(DataObject dataObject, string text, SmartCategory category)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Standard text formats
            dataObject.SetData(DataFormats.UnicodeText, text);
            dataObject.SetData(DataFormats.Text, text);
            dataObject.SetData(DataFormats.StringFormat, text);
            dataObject.SetData(DataFormats.OemText, text);

            // URL format if valid web URL
            if (category == SmartCategory.Link || 
                (Uri.TryCreate(text, UriKind.Absolute, out var uri) && 
                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            {
                try
                {
                    var urlBytes = Encoding.ASCII.GetBytes(text);
                    dataObject.SetData("UniformResourceLocator", new MemoryStream(urlBytes));
                    var urlWBytes = Encoding.Unicode.GetBytes(text);
                    dataObject.SetData("UniformResourceLocatorW", new MemoryStream(urlWBytes));
                    dataObject.SetData(DataFormats.Html, BuildHtmlFormat($"<a href=\"{text}\">{text}</a>"));
                }
                catch { }
            }
        }

        public static void PopulateImageFormats(DataObject dataObject, BitmapSource image, Guid itemId)
        {
            if (image == null) return;

            // 1. Native Bitmap
            dataObject.SetData(DataFormats.Bitmap, image);

            // 2. CF_DIB (Device Independent Bitmap) for Word, Outlook, Paint, RichEdit
            var dibStream = CreateDibStream(image);
            if (dibStream != null)
            {
                dataObject.SetData(DataFormats.Dib, dibStream);
                dataObject.SetData("DeviceIndependentBitmap", dibStream);
            }

            // 3. PNG Stream for Chromium, Discord, Slack, Teams, Telegram, Web
            var pngStream = CreatePngStream(image);
            if (pngStream != null)
            {
                dataObject.SetData("PNG", pngStream);
                dataObject.SetData("image/png", pngStream);
            }

            // 4. Temporary PNG file on disk for FileDrop and plain text boxes
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "CustomClipboardManager");
                Directory.CreateDirectory(tempDir);
                string tempImg = Path.Combine(tempDir, $"clip_{itemId:N}.png");
                if (!File.Exists(tempImg))
                {
                    using var fs = new FileStream(tempImg, FileMode.Create);
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(image));
                    enc.Save(fs);
                }

                // FileDrop for Explorer, Discord upload, Desktop
                dataObject.SetData(DataFormats.FileDrop, new string[] { tempImg });
                dataObject.SetData("FileNameW", new string[] { tempImg });
                dataObject.SetData("FileName", tempImg);

                // CRITICAL FOR PLAIN TEXT BOXES:
                // Plain text boxes (Notepad, SearchBox, URL bar, terminals) ONLY accept UnicodeText.
                // Providing the file path in UnicodeText allows dropping an image on ANY text box
                // without showing the forbidden (🚫) cursor!
                dataObject.SetData(DataFormats.UnicodeText, tempImg);
                dataObject.SetData(DataFormats.Text, tempImg);
                dataObject.SetData(DataFormats.StringFormat, tempImg);

                // HTML format for rich text editors (Word, Outlook, Google Docs, etc.)
                string fileUri = new Uri(tempImg).AbsoluteUri;
                dataObject.SetData(DataFormats.Html, BuildHtmlFormat($"<img src=\"{fileUri}\" />"));
            }
            catch { }
        }

        public static void PopulateFileFormats(DataObject dataObject, IList<string> paths)
        {
            if (paths == null || paths.Count == 0) return;

            string[] filesArray = paths.ToArray();
            dataObject.SetData(DataFormats.FileDrop, filesArray);
            dataObject.SetData("FileNameW", filesArray);

            if (filesArray.Length == 1)
            {
                dataObject.SetData("FileName", filesArray[0]);
                dataObject.SetData(DataFormats.UnicodeText, filesArray[0]);
                dataObject.SetData(DataFormats.Text, filesArray[0]);
                dataObject.SetData(DataFormats.StringFormat, filesArray[0]);
            }
            else
            {
                string joined = string.Join(Environment.NewLine, filesArray);
                dataObject.SetData(DataFormats.UnicodeText, joined);
                dataObject.SetData(DataFormats.Text, joined);
                dataObject.SetData(DataFormats.StringFormat, joined);
            }

            // If the first file is an existing image, also supply image streams!
            // This allows dragging an image file into Word/Discord/chat box to be embedded directly!
            if (filesArray.Length > 0 && File.Exists(filesArray[0]))
            {
                string ext = Path.GetExtension(filesArray[0]).ToLowerInvariant();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" || ext == ".webp")
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(filesArray[0]);
                        bmp.EndInit();
                        bmp.Freeze();

                        dataObject.SetData(DataFormats.Bitmap, bmp);

                        var dib = CreateDibStream(bmp);
                        if (dib != null)
                        {
                            dataObject.SetData(DataFormats.Dib, dib);
                            dataObject.SetData("DeviceIndependentBitmap", dib);
                        }

                        var png = CreatePngStream(bmp);
                        if (png != null)
                        {
                            dataObject.SetData("PNG", png);
                            dataObject.SetData("image/png", png);
                        }

                        string fileUri = new Uri(filesArray[0]).AbsoluteUri;
                        dataObject.SetData(DataFormats.Html, BuildHtmlFormat($"<img src=\"{fileUri}\" />"));
                    }
                    catch { }
                }
            }
        }

        public static MemoryStream? CreateDibStream(BitmapSource bitmapSource)
        {
            try
            {
                using var bmpMs = new MemoryStream();
                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapSource));
                enc.Save(bmpMs);
                byte[] bmpBytes = bmpMs.ToArray();
                if (bmpBytes.Length > 14 && bmpBytes[0] == (byte)'B' && bmpBytes[1] == (byte)'M')
                {
                    // Skip 14-byte BITMAPFILEHEADER to get CF_DIB
                    return new MemoryStream(bmpBytes, 14, bmpBytes.Length - 14);
                }
            }
            catch { }
            return null;
        }

        public static MemoryStream? CreatePngStream(BitmapSource bitmapSource)
        {
            try
            {
                var pngMs = new MemoryStream();
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapSource));
                enc.Save(pngMs);
                pngMs.Position = 0;
                return pngMs;
            }
            catch { }
            return null;
        }

        public static string BuildHtmlFormat(string htmlFragment)
        {
            const string header = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
            const string prefix = "<html><body><!--StartFragment-->";
            const string postfix = "<!--EndFragment--></body></html>";

            string dummyHeader = string.Format(header, 0, 0, 0, 0);
            int startHtml = Encoding.UTF8.GetByteCount(dummyHeader);
            int startFragment = startHtml + Encoding.UTF8.GetByteCount(prefix);
            int endFragment = startFragment + Encoding.UTF8.GetByteCount(htmlFragment);
            int endHtml = endFragment + Encoding.UTF8.GetByteCount(postfix);

            return string.Format(header, startHtml, endHtml, startFragment, endFragment) + prefix + htmlFragment + postfix;
        }
    }
}
