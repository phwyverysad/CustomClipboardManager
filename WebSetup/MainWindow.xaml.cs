using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace ClipboardWebSetup
{
    public partial class MainWindow : Window
    {
        private string _targetExePath = string.Empty;
        private bool _isInstalling = false;
        private HotkeyConfig _currentHotkey = HotkeyConfig.Default;
        private bool _isRecordingHotkey = false;

        public MainWindow()
        {
            InitializeComponent();
            ApplyWindowsThemeAndColor();

            // Set up language selector
            LanguageComboBox.ItemsSource = LocalizationManager.SupportedLanguages;
            LanguageComboBox.SelectedValue = LocalizationManager.CurrentLanguageCode;
            LanguageComboBox.SelectedValuePath = "Code";

            LocalizationManager.LanguageChanged += UpdateLocalizedUI;
            UpdateLocalizedUI();

            // Default destination path
            InitDefaultInstallPath();
            CheckExistingInstallation();

            // Initialize configured hotkey
            try
            {
                _currentHotkey = HotkeyManager.Load();
                if (CurrentHotkeyDisplayText != null)
                {
                    CurrentHotkeyDisplayText.Text = _currentHotkey.DisplayText;
                }
                SyncPresetComboBox();
            }
            catch { }

            // Handle CLI flags
            string[] args = Environment.GetCommandLineArgs();
            string previewLang = "th";
            bool isPreview = false;
            bool isUninstall = false;
            bool isSilent = false;
            if (args != null && args.Length > 1)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--preview", StringComparison.OrdinalIgnoreCase))
                    {
                        isPreview = true;
                    }
                    else if (args[i].StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
                    {
                        previewLang = args[i].Substring(7);
                    }
                    else if (string.Equals(args[i], "--uninstall", StringComparison.OrdinalIgnoreCase))
                    {
                        isUninstall = true;
                    }
                    else if (string.Equals(args[i], "--silent", StringComparison.OrdinalIgnoreCase) || string.Equals(args[i], "/S", StringComparison.OrdinalIgnoreCase))
                    {
                        isSilent = true;
                    }
                }
            }

            if (isUninstall)
            {
                this.Loaded += async (s, e) =>
                {
                    if (isSilent)
                    {
                        this.Visibility = Visibility.Hidden;
                    }
                    string targetDir = InstallPathTextBox.Text.Trim();
                    await ExecuteUninstallAsync(targetDir, isSilent);
                };
                return;
            }

            if (isPreview)
            {
                LocalizationManager.SetLanguage(previewLang);
                LanguageComboBox.SelectedValue = previewLang;
                this.Loaded += (s, e) =>
                {
                    try
                    {
                        this.Measure(new Size(560, 440));
                        this.Arrange(new Rect(0, 0, 560, 440));
                        this.UpdateLayout();

                        int w = (int)Math.Max(560, this.ActualWidth);
                        int h = (int)Math.Max(440, this.ActualHeight);
                        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(w, h, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtb.Render(this);
                        var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                        string outPath = $@"C:\Users\woran\.gemini\antigravity\brain\e3acd1bb-b4e2-4be2-ae5c-8677134ae45c\verify_websetup_{previewLang}.png";
                        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                        using (var fs = File.Create(outPath))
                        {
                            enc.Save(fs);
                        }
                    }
                    catch { }
                    Application.Current.Shutdown();
                };
            }
        }

        private void CheckExistingInstallation()
        {
            try
            {
                string runningDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                if (File.Exists(Path.Combine(runningDir, "CustomClipboardManager.exe")))
                {
                    InstallPathTextBox.Text = runningDir;
                }
                else
                {
                    string installedPath = null;
                    try
                    {
                        using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                        using (var key = hklm.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CustomClipboardManager"))
                        {
                            installedPath = key?.GetValue("InstallLocation") as string;
                        }
                    }
                    catch { }

                    if (string.IsNullOrEmpty(installedPath))
                    {
                        try
                        {
                            using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                            using (var key = hkcu.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CustomClipboardManager"))
                            {
                                installedPath = key?.GetValue("InstallLocation") as string;
                            }
                        }
                        catch { }
                    }

                    if (!string.IsNullOrEmpty(installedPath) && Directory.Exists(installedPath))
                    {
                        InstallPathTextBox.Text = installedPath;
                    }
                }

                string currentTargetDir = InstallPathTextBox.Text.Trim();
                bool exeExists = !string.IsNullOrEmpty(currentTargetDir) && File.Exists(Path.Combine(currentTargetDir, "CustomClipboardManager.exe"));

                bool serviceExists = false;
                try
                {
                    using (var sc = new ServiceController("CustomClipboardService"))
                    {
                        var name = sc.ServiceName;
                        serviceExists = true;
                    }
                }
                catch { }

                if (exeExists || serviceExists)
                {
                    UninstallButton.Visibility = Visibility.Visible;
                    InstallButton.Content = LocalizationManager.Current.ReinstallButton;
                }
            }
            catch { }
        }

        private void InitDefaultInstallPath()
        {
            bool isAdmin = IsAdministrator();
            if (isAdmin)
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                InstallPathTextBox.Text = Path.Combine(programFiles, "Clipboard");
            }
            else
            {
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                InstallPathTextBox.Text = Path.Combine(localApp, "Programs", "Clipboard");
            }
        }

        private bool IsAdministrator()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        private void UpdateLocalizedUI()
        {
            var loc = LocalizationManager.Current;

            WindowTitleText.Text = loc.WindowTitle;
            HeaderTitleText.Text = loc.HeaderTitle;
            HeaderSubtitleText.Text = loc.HeaderSubtitle;
            AppVersionText.Text = loc.AppVersion;

            DestinationFolderText.Text = loc.DestinationFolder;
            BrowseButton.Content = loc.Browse;

            DesktopShortcutCheckBox.Content = loc.OptionDesktopShortcut;
            StartMenuCheckBox.Content = loc.OptionStartMenu;
            AutoStartCheckBox.Content = loc.OptionAutoStart;
            InstallServiceCheckBox.Content = loc.OptionInstallService;
            LaunchAppCheckBox.Content = loc.OptionLaunchApp;
            if (UninstallButton.Visibility == Visibility.Visible)
            {
                InstallButton.Content = loc.ReinstallButton;
            }
            else
            {
                InstallButton.Content = loc.InstallButton;
            }
            UninstallButton.Content = loc.UninstallButton;
            CancelButton.Content = loc.CancelButton;

            CompleteTitleText.Text = loc.CompleteTitle;
            CompleteMessageText.Text = loc.CompleteMessage;
            if (HotkeyCardTitleText != null) HotkeyCardTitleText.Text = loc.HotkeyCardTitle;
            if (HotkeyStatusHintText != null && !_isRecordingHotkey) HotkeyStatusHintText.Text = loc.HotkeyChangeHint;
            if (ResetHotkeyButton != null) ResetHotkeyButton.ToolTip = loc.HotkeyResetTooltip;
            if (PresetCustomItem != null) PresetCustomItem.Content = loc.HotkeyCustomItem;
            FinishButton.Content = loc.LaunchFinishButton;

            ErrorTitleText.Text = loc.ErrorTitle;
            ErrorCloseButton.Content = loc.CloseButton;
            RetryButton.Content = loc.RetryButton;
        }

        private void LanguageComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is LanguageInfo selected)
            {
                LocalizationManager.SetLanguage(selected.Code);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInstalling)
            {
                this.Close();
            }
            else
            {
                var loc = LocalizationManager.Current;
                var result = MessageBox.Show(
                    loc.CancelConfirmMessage,
                    loc.WindowTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    this.Close();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = LocalizationManager.Current.DestinationFolder;
                dialog.SelectedPath = InstallPathTextBox.Text;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    InstallPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            string targetDir = InstallPathTextBox.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                MessageBox.Show("Please specify a valid installation folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isInstalling = true;
            ConfigPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            CompletePanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;

            bool createDesktop = DesktopShortcutCheckBox.IsChecked == true;
            bool createStartMenu = StartMenuCheckBox.IsChecked == true;
            bool autoStart = AutoStartCheckBox.IsChecked == true;
            bool installService = InstallServiceCheckBox.IsChecked == true;
            bool launchApp = LaunchAppCheckBox.IsChecked == true;

            try
            {
                await Task.Run(() => PerformInstallation(targetDir, createDesktop, createStartMenu, autoStart, installService, launchApp));

                // Success
                _isInstalling = false;
                ProgressPanel.Visibility = Visibility.Collapsed;
                CompletePanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                _isInstalling = false;
                ProgressPanel.Visibility = Visibility.Collapsed;
                ErrorPanel.Visibility = Visibility.Visible;
                ErrorMessageText.Text = ex.Message;
            }
        }

        private void PerformInstallation(string targetDir, bool createDesktop, bool createStartMenu, bool autoStart, bool installService, bool launchApp)
        {
            var loc = LocalizationManager.Current;

            // 1. Terminate existing running instance if any
            UpdateProgress(loc.StatusExtracting, "Closing running application instances gracefully...", 5, 0, 100);
            try
            {
                foreach (var proc in Process.GetProcessesByName("CustomClipboardManager"))
                {
                    try { proc.CloseMainWindow(); } catch { }
                }
            }
            catch { }

            try
            {
                var procs = Process.GetProcessesByName("CustomClipboardManager");
                foreach (var proc in procs)
                {
                    try
                    {
                        if (!proc.WaitForExit(1500))
                        {
                            proc.Kill();
                            proc.WaitForExit(1000);
                        }
                    }
                    catch { }
                }
            }
            catch { }

            try
            {
                RunCommand("sc.exe", "stop CustomClipboardService");
            }
            catch { }

            // 2. Check and Install .NET 10 Desktop Runtime if missing
            UpdateProgress(loc.StatusCheckingDotnet, "Verifying Microsoft Windows Desktop Runtime 10.x...", 10, 10, 100);
            EnsureDotnet10Runtime();

            // 3. Obtain Package (Try web download from GitHub releases, fallback to embedded payload)
            UpdateProgress(loc.StatusCheckingUpdate, "Checking web release package...", 25, 25, 100);
            byte[] packageBytes = DownloadOrExtractPayload();

            // 3.5 Preserve existing clipboard data and create backups before deploying update
            UpdateProgress(loc.StatusExtracting, "Preserving existing clipboard data & history...", 50, 50, 100);
            PreserveClipboardDataBeforeUpdate(targetDir);

            // 4. Extract Package Files to Target Directory
            UpdateProgress(loc.StatusExtracting, $"Deploying files to {targetDir}...", 55, 55, 100);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            using (var ms = new MemoryStream(packageBytes))
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                int totalEntries = archive.Entries.Count;
                int currentEntry = 0;

                foreach (var entry in archive.Entries)
                {
                    currentEntry++;
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        // Directory entry
                        string dirPath = Path.Combine(targetDir, entry.FullName);
                        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
                        continue;
                    }

                    string fullDestPath = Path.Combine(targetDir, entry.FullName);
                    string entryDir = Path.GetDirectoryName(fullDestPath);
                    if (!string.IsNullOrEmpty(entryDir) && !Directory.Exists(entryDir))
                    {
                        Directory.CreateDirectory(entryDir);
                    }

                    // Protect existing user clipboard data from being overwritten
                    if (entry.Name.Equals("data.json", StringComparison.OrdinalIgnoreCase) && File.Exists(fullDestPath))
                    {
                        continue;
                    }

                    entry.ExtractToFile(fullDestPath, true);
                    int pct = 55 + (int)((currentEntry / (double)totalEntries) * 25.0);
                    UpdateProgress(loc.StatusExtracting, $"Extracting: {entry.Name}", pct, pct, 100);
                }
            }

            _targetExePath = Path.Combine(targetDir, "CustomClipboardManager.exe");

            // 5. Create Shortcuts
            UpdateProgress(loc.StatusShortcuts, "Creating application shortcuts...", 85, 85, 100);
            if (createDesktop)
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                CreateShortcut(Path.Combine(desktopPath, "Custom Clipboard Manager.lnk"), _targetExePath, targetDir);
            }

            if (createStartMenu)
            {
                string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Custom Clipboard Manager");
                if (!Directory.Exists(startMenuPath)) Directory.CreateDirectory(startMenuPath);
                CreateShortcut(Path.Combine(startMenuPath, "Custom Clipboard Manager.lnk"), _targetExePath, targetDir);
            }

            // 6. Auto-start on boot
            if (autoStart)
            {
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        key?.SetValue("CustomClipboardManager", $"\"{_targetExePath}\" --background");
                    }
                }
                catch { }
            }

            // 7. Clean up and purge any legacy CustomClipboardService from services.msc
            try
            {
                RunCommand("sc.exe", "stop CustomClipboardService");
                RunCommand("sc.exe", "delete CustomClipboardService");
            }
            catch { }

            // 7.5 Register Windows Uninstall Entry
            RegisterUninstallInfo(targetDir, _targetExePath);

            // 7.8 Launch application in background immediately so global hotkey is active from millisecond 0
            if (launchApp && File.Exists(_targetExePath))
            {
                try
                {
                    HotkeyManager.Save(_currentHotkey, targetDir);
                }
                catch { }

                try
                {
                    string lang = LocalizationManager.CurrentLanguageCode ?? "en";
                    File.WriteAllText(Path.Combine(targetDir, "language.json"), $"{{\"Language\": \"{lang}\"}}");
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\CustomClipboardManager"))
                    {
                        key?.SetValue("Language", lang);
                    }
                }
                catch { }

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = _targetExePath,
                        UseShellExecute = true,
                        WorkingDirectory = targetDir
                    };
                    Process.Start(psi);
                }
                catch { }
            }

            // 8. Completed
            UpdateProgress(loc.StatusComplete, "Ready!", 100, 100, 100);
        }

        private void EnsureDotnet10Runtime()
        {
            var loc = LocalizationManager.Current;
            // Check if .NET 10 desktop runtime is installed
            bool isDotnet10Installed = false;
            try
            {
                string sharedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "shared", "Microsoft.WindowsDesktop.App");
                if (Directory.Exists(sharedPath))
                {
                    var dirs = Directory.GetDirectories(sharedPath, "10.*");
                    if (dirs.Length > 0) isDotnet10Installed = true;
                }
            }
            catch { }

            if (isDotnet10Installed) return;

            // If not installed, download from Microsoft CDN
            UpdateProgress(loc.StatusDownloadingDotnet, "Downloading .NET 10 Desktop Runtime from Microsoft CDN...", 15, 15, 100);
            string tempInstaller = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime-10-win-x64.exe");

            try
            {
                string downloadUrl = "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe";
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    client.DownloadFile(downloadUrl, tempInstaller);
                }

                UpdateProgress(loc.StatusInstallingDotnet, "Installing .NET 10 Desktop Runtime silently...", 20, 20, 100);
                var psi = new ProcessStartInfo
                {
                    FileName = tempInstaller,
                    Arguments = "/install /quiet /norestart",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                var proc = Process.Start(psi);
                proc?.WaitForExit();
            }
            catch
            {
                // If download fails, continue installation — the app or Windows will prompt if required
            }
            finally
            {
                try { if (File.Exists(tempInstaller)) File.Delete(tempInstaller); } catch { }
            }
        }

        private byte[] DownloadOrExtractPayload()
        {
            var loc = LocalizationManager.Current;

            // 1. Prioritize embedded Payload.zip (packaged with this installer)
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("Payload.zip"))
                {
                    if (stream != null && stream.Length > 1024)
                    {
                        UpdateProgress(loc.StatusExtracting, "Extracting bundled package payload...", 50, 50, 100);
                        using (var ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            return ms.ToArray();
                        }
                    }
                }
            }
            catch
            {
                // Fallback to online download if reading embedded payload failed
            }

            // 2. Fallback: Attempt online download if embedded payload is missing
            string remoteUrl = "https://github.com/phwyverysad/CustomClipboardManager/releases/latest/download/CustomClipboardManager_Payload.zip";
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(remoteUrl);
                request.Method = "GET";
                request.Timeout = 6000;
                request.UserAgent = "Clipboard_WebSetup";

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        long totalBytes = response.ContentLength;
                        using (var responseStream = response.GetResponseStream())
                        using (var memoryStream = new MemoryStream())
                        {
                            byte[] buffer = new byte[65536];
                            int bytesRead;
                            long totalRead = 0;
                            var stopwatch = Stopwatch.StartNew();

                            while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                memoryStream.Write(buffer, 0, bytesRead);
                                totalRead += bytesRead;

                                if (totalBytes > 0)
                                {
                                    int pct = 25 + (int)((totalRead / (double)totalBytes) * 30.0);
                                    double mbRead = totalRead / (1024.0 * 1024.0);
                                    double mbTotal = totalBytes / (1024.0 * 1024.0);
                                    double speed = mbRead / Math.Max(0.1, stopwatch.Elapsed.TotalSeconds);
                                    UpdateProgress(loc.StatusDownloadingPackage, $"{mbRead:F1} MB / {mbTotal:F1} MB ({speed:F1} MB/s)", pct, pct, 100);
                                }
                            }
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
            catch
            {
            }

            throw new InvalidOperationException("Could not obtain installation payload from embedded resources or web.");
        }

        private void PreserveClipboardDataBeforeUpdate(string targetDir)
        {
            try
            {
                var candidateAppDataDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(localApp))
                {
                    candidateAppDataDirs.Add(Path.Combine(localApp, "CustomClipboardManager"));
                }

                // Check other user profiles under C:\Users if running elevated as admin
                try
                {
                    if (Directory.Exists(@"C:\Users"))
                    {
                        foreach (var uDir in Directory.GetDirectories(@"C:\Users"))
                        {
                            string uName = Path.GetFileName(uDir);
                            if (uName.Equals("Public", StringComparison.OrdinalIgnoreCase) ||
                                uName.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                                uName.Equals("All Users", StringComparison.OrdinalIgnoreCase) ||
                                uName.Equals("Default User", StringComparison.OrdinalIgnoreCase))
                                continue;

                            string candidate = Path.Combine(uDir, "AppData", "Local", "CustomClipboardManager");
                            candidateAppDataDirs.Add(candidate);
                        }
                    }
                }
                catch { }

                // 1. For every AppData folder where data.json exists, create safety backups (data.json.bak and Backups/data_pre_update_*.json)
                foreach (var dir in candidateAppDataDirs)
                {
                    try
                    {
                        string dataPath = Path.Combine(dir, "data.json");
                        if (File.Exists(dataPath) && new FileInfo(dataPath).Length > 0)
                        {
                            string bakPath = Path.Combine(dir, "data.json.bak");
                            File.Copy(dataPath, bakPath, true);

                            string backupsDir = Path.Combine(dir, "Backups");
                            if (!Directory.Exists(backupsDir)) Directory.CreateDirectory(backupsDir);
                            string timeBackup = Path.Combine(backupsDir, $"data_pre_update_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                            File.Copy(dataPath, timeBackup, true);
                        }
                    }
                    catch { }
                }

                // 2. If targetDir itself contains legacy data.json, back it up and migrate it to user AppData
                if (!string.IsNullOrEmpty(targetDir) && Directory.Exists(targetDir))
                {
                    string targetData = Path.Combine(targetDir, "data.json");
                    if (File.Exists(targetData) && new FileInfo(targetData).Length > 0)
                    {
                        string targetBak = Path.Combine(targetDir, "data.json.bak");
                        try { File.Copy(targetData, targetBak, true); } catch { }

                        // Migrate to candidate AppData folders if missing
                        foreach (var dir in candidateAppDataDirs)
                        {
                            try
                            {
                                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                                string destData = Path.Combine(dir, "data.json");
                                if (!File.Exists(destData) || new FileInfo(destData).Length == 0)
                                {
                                    File.Copy(targetData, destData, true);
                                }
                            }
                            catch { }
                        }
                    }

                    // Migrate images from targetDir\Images if any
                    string targetImages = Path.Combine(targetDir, "Images");
                    if (Directory.Exists(targetImages))
                    {
                        foreach (var dir in candidateAppDataDirs)
                        {
                            try
                            {
                                string destImgDir = Path.Combine(dir, "Images");
                                if (!Directory.Exists(destImgDir)) Directory.CreateDirectory(destImgDir);

                                foreach (var img in Directory.GetFiles(targetImages, "*.png"))
                                {
                                    string destImg = Path.Combine(destImgDir, Path.GetFileName(img));
                                    if (!File.Exists(destImg))
                                    {
                                        try { File.Copy(img, destImg, true); } catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string workingDir)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = workingDir;
                    shortcut.IconLocation = $"{targetPath},0";
                    shortcut.Description = "Custom Clipboard Manager";
                    shortcut.Save();
                }
            }
            catch { }
        }



        private void ApplyWindowsThemeAndColor()
        {
            // 1. Windows Accent Color
            System.Windows.Media.Color accent = System.Windows.Media.Color.FromRgb(0, 120, 215); // Default Windows Blue
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("AccentColor");
                        if (val != null)
                        {
                            uint abgr = Convert.ToUInt32(val);
                            byte r = (byte)(abgr & 0xFF);
                            byte g = (byte)((abgr >> 8) & 0xFF);
                            byte b = (byte)((abgr >> 16) & 0xFF);
                            accent = System.Windows.Media.Color.FromRgb(r, g, b);
                        }
                    }
                }
            }
            catch { }

            var accentHover = System.Windows.Media.Color.FromRgb(
                (byte)Math.Min(255, accent.R + 25),
                (byte)Math.Min(255, accent.G + 25),
                (byte)Math.Min(255, accent.B + 25));

            var accentPressed = System.Windows.Media.Color.FromRgb(
                (byte)Math.Max(0, accent.R - 25),
                (byte)Math.Max(0, accent.G - 25),
                (byte)Math.Max(0, accent.B - 25));

            // 2. Windows Light vs Dark Theme
            bool isLightTheme = false;
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("AppsUseLightTheme");
                        if (val is int intVal && intVal == 1) isLightTheme = true;
                    }
                }
            }
            catch { }

            // Dynamic resource assignment
            this.Resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(accent);
            this.Resources["AccentHoverBrush"] = new System.Windows.Media.SolidColorBrush(accentHover);
            this.Resources["AccentPressedBrush"] = new System.Windows.Media.SolidColorBrush(accentPressed);

            if (isLightTheme)
            {
                this.Resources["BgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF3, 0xF3, 0xF3));
                this.Resources["CardBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
                this.Resources["CardHoverBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEE, 0xEE, 0xF2));
                this.Resources["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD0, 0xD0, 0xD5));
                this.Resources["TextPrimaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x19, 0x19, 0x19));
                this.Resources["TextSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x5A, 0x5A, 0x62));
                this.Resources["SecondaryButtonBgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEA, 0xEA, 0xEE));
                this.Resources["SecondaryButtonHoverBgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xDD, 0xDD, 0xE2));
            }
            else
            {
                this.Resources["BgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x18, 0x18, 0x1A));
                this.Resources["CardBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x22, 0x22, 0x26));
                this.Resources["CardHoverBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x30));
                this.Resources["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x3A));
                this.Resources["TextPrimaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF2, 0xF2, 0xF7));
                this.Resources["TextSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x98, 0x98, 0xA0));
                this.Resources["SecondaryButtonBgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2B, 0x2B, 0x30));
                this.Resources["SecondaryButtonHoverBgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x35, 0x35, 0x3C));
            }
        }

        private void RunCommand(string command, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var proc = Process.Start(psi);
                proc?.WaitForExit(2000);
            }
            catch { }
        }

        private void UpdateProgress(string title, string detail, int percent, double current, double total)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                ProgressTitleText.Text = title;
                ProgressDetailText.Text = detail;
                InstallProgressBar.Value = percent;
                ProgressPercentText.Text = $"{percent}%";
                ProgressSpeedText.Text = detail;
            }));
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private static void SignalIpcWake(int sessionId)
        {
            // Channel 1: Win32 Message Broadcast
            try
            {
                uint msg = RegisterWindowMessage("CustomClipboardManager_ShowMessage_v1");
                if (msg != 0)
                {
                    PostMessage((IntPtr)0xFFFF, msg, IntPtr.Zero, IntPtr.Zero);
                }
            }
            catch { }

            // Channel 2: EventWaitHandle
            try
            {
                string showEventName = $"Local\\CustomClipboardManager_ShowEvent_{sessionId}";
                using (var showEvent = EventWaitHandle.OpenExisting(showEventName))
                {
                    showEvent.Set();
                }
            }
            catch { }

            // Channel 3: File Signal
            try
            {
                string signalDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CustomClipboardManager");
                if (!Directory.Exists(signalDir)) Directory.CreateDirectory(signalDir);
                string signalFile = Path.Combine(signalDir, "wake.signal");
                File.WriteAllText(signalFile, DateTime.UtcNow.Ticks.ToString());
            }
            catch { }
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            // Always launch or wake up application on finish click
            try
            {
                string targetExe = _targetExePath;
                if (string.IsNullOrEmpty(targetExe) || !File.Exists(targetExe))
                {
                    targetExe = Path.Combine(InstallPathTextBox?.Text?.Trim() ?? "", "CustomClipboardManager.exe");
                }
                if (string.IsNullOrEmpty(targetExe) || !File.Exists(targetExe))
                {
                    targetExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Clipboard", "CustomClipboardManager.exe");
                }

                if (File.Exists(targetExe))
                {
                    // Ensure configured hotkey is saved to targetDir and LocalAppData
                    try
                    {
                        HotkeyManager.Save(_currentHotkey, Path.GetDirectoryName(targetExe));
                    }
                    catch { }

                    int sessionId = Process.GetCurrentProcess().SessionId;

                    // 1. Fire multi-channel IPC signal to wake up running background instance
                    SignalIpcWake(sessionId);

                    // 2. Check if CustomClipboardManager is actively running in this user session
                    bool isProcessRunning = false;
                    try
                    {
                        isProcessRunning = Process.GetProcessesByName("CustomClipboardManager")
                            .Any(p => p.SessionId == sessionId);
                    }
                    catch { }

                    // 3. If not running in this session, launch targetExe
                    if (!isProcessRunning)
                    {
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = targetExe,
                                Arguments = "--show",
                                UseShellExecute = true,
                                WorkingDirectory = Path.GetDirectoryName(targetExe)
                            };
                            Process.Start(psi);
                        }
                        catch
                        {
                            try
                            {
                                Process.Start("explorer.exe", $"\"{targetExe}\"");
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }

            this.Close();
        }

        private void RecordHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            StartRecordingHotkey();
        }

        private void StartRecordingHotkey()
        {
            _isRecordingHotkey = true;
            CurrentHotkeyDisplayText.Text = "...";
            HotkeyStatusHintText.Text = LocalizationManager.Current.HotkeyRecordingPrompt;
            HotkeyStatusHintText.Foreground = new SolidColorBrush(Color.FromRgb(255, 69, 58));
            RecordHotkeyButton.Focus();
        }

        private void StopRecordingHotkey()
        {
            _isRecordingHotkey = false;
            CurrentHotkeyDisplayText.Text = _currentHotkey.DisplayText;
            HotkeyStatusHintText.Text = LocalizationManager.Current.HotkeyChangeHint;
            HotkeyStatusHintText.Foreground = (Brush)FindResource("TextSecondaryBrush");
        }

        private void ResetHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            _currentHotkey = HotkeyConfig.Default;
            _currentHotkey.DisplayText = _currentHotkey.BuildDisplayText();
            StopRecordingHotkey();
            SyncPresetComboBox();
            SaveConfiguredHotkey();
        }

        private void PresetHotkeyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PresetHotkeyComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                if (tag == "Custom")
                {
                    StartRecordingHotkey();
                }
                else
                {
                    _currentHotkey = HotkeyManager.GetPreset(tag);
                    StopRecordingHotkey();
                    SaveConfiguredHotkey();
                }
            }
        }

        private void SyncPresetComboBox()
        {
            if (PresetHotkeyComboBox == null) return;
            string display = _currentHotkey.DisplayText;
            foreach (ComboBoxItem item in PresetHotkeyComboBox.Items)
            {
                if (string.Equals(item.Content?.ToString(), display, StringComparison.OrdinalIgnoreCase))
                {
                    PresetHotkeyComboBox.SelectedItem = item;
                    return;
                }
            }
            foreach (ComboBoxItem item in PresetHotkeyComboBox.Items)
            {
                if (item.Tag?.ToString() == "Custom")
                {
                    PresetHotkeyComboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private void SaveConfiguredHotkey()
        {
            try
            {
                string targetDir = InstallPathTextBox?.Text?.Trim();
                HotkeyManager.Save(_currentHotkey, targetDir);
            }
            catch { }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isRecordingHotkey) return;

            e.Handled = true;

            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            if (key == Key.Escape)
            {
                StopRecordingHotkey();
                return;
            }

            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            bool win = (Keyboard.Modifiers & ModifierKeys.Windows) != 0;

            int vk = KeyInterop.VirtualKeyFromKey(key);
            string keyName = HotkeyConfig.GetFriendlyKeyName(vk);

            var newConfig = new HotkeyConfig
            {
                Control = ctrl,
                Alt = alt,
                Shift = shift,
                Windows = win,
                VirtualKey = vk,
                KeyName = keyName
            };

            if (!ctrl && !alt && !shift && !win && !(vk >= 0x70 && vk <= 0x7B))
            {
                newConfig.Control = true;
            }

            newConfig.DisplayText = newConfig.BuildDisplayText();
            _currentHotkey = newConfig;

            StopRecordingHotkey();
            SyncPresetComboBox();
            SaveConfiguredHotkey();
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigPanel.Visibility = Visibility.Visible;
            ProgressPanel.Visibility = Visibility.Collapsed;
            CompletePanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void RegisterUninstallInfo(string targetDir, string targetExe)
        {
            try
            {
                string currentInstaller = null;
                try
                {
                    currentInstaller = Process.GetCurrentProcess().MainModule?.FileName;
                }
                catch { }

                string installedInstaller = Path.Combine(targetDir, "Clipboard_WebSetup.exe");

                if (!string.IsNullOrEmpty(currentInstaller) && File.Exists(currentInstaller))
                {
                    if (!string.Equals(currentInstaller, installedInstaller, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            File.Copy(currentInstaller, installedInstaller, true);
                        }
                        catch { }
                    }
                }

                string uninstallCmd = $"\"{installedInstaller}\" --uninstall";
                string quietUninstallCmd = $"\"{installedInstaller}\" --uninstall --silent";

                Action<RegistryKey> writeEntries = (key) =>
                {
                    if (key == null) return;
                    using (key)
                    {
                        key.SetValue("DisplayName", "Custom Clipboard Manager");
                        key.SetValue("DisplayVersion", "1.0.0");
                        key.SetValue("Publisher", "CustomClipboard");
                        key.SetValue("DisplayIcon", $"{targetExe},0");
                        key.SetValue("InstallLocation", targetDir);
                        key.SetValue("UninstallString", uninstallCmd);
                        key.SetValue("QuietUninstallString", quietUninstallCmd);
                        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                        key.SetValue("EstimatedSize", 18500, RegistryValueKind.DWord);
                    }
                };

                try
                {
                    using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    using (var key = hklm.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CustomClipboardManager"))
                    {
                        writeEntries(key);
                    }
                }
                catch
                {
                    using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                    using (var key = hkcu.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CustomClipboardManager"))
                    {
                        writeEntries(key);
                    }
                }
            }
            catch { }
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            var loc = LocalizationManager.Current;
            var confirm = MessageBox.Show(
                loc.UninstallConfirmMessage,
                loc.WindowTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            string targetDir = InstallPathTextBox.Text.Trim();
            await ExecuteUninstallAsync(targetDir, isSilent: false);
        }

        private async Task ExecuteUninstallAsync(string targetDir, bool isSilent)
        {
            var loc = LocalizationManager.Current;
            _isInstalling = true;

            if (!isSilent)
            {
                ConfigPanel.Visibility = Visibility.Collapsed;
                ProgressPanel.Visibility = Visibility.Visible;
                CompletePanel.Visibility = Visibility.Collapsed;
                ErrorPanel.Visibility = Visibility.Collapsed;
            }

            try
            {
                await Task.Run(() => PerformUninstallation(targetDir, isSilent));

                if (isSilent)
                {
                    Application.Current.Shutdown(0);
                    return;
                }

                _isInstalling = false;
                ProgressPanel.Visibility = Visibility.Collapsed;
                CompletePanel.Visibility = Visibility.Visible;

                CompleteTitleText.Text = loc.UninstallCompleteTitle;
                CompleteMessageText.Text = loc.UninstallCompleteMessage;
                if (HotkeyCardBorder != null) HotkeyCardBorder.Visibility = Visibility.Collapsed;
                LaunchAppCheckBox.Visibility = Visibility.Collapsed;
                FinishButton.Content = loc.CloseButton;
            }
            catch (Exception ex)
            {
                if (isSilent)
                {
                    Application.Current.Shutdown(-1);
                    return;
                }
                _isInstalling = false;
                ProgressPanel.Visibility = Visibility.Collapsed;
                ErrorPanel.Visibility = Visibility.Visible;
                ErrorMessageText.Text = ex.Message;
            }
        }

        private void PerformUninstallation(string targetDir, bool isSilent)
        {
            var loc = LocalizationManager.Current;

            // 1. Terminate running applications
            UpdateProgress(loc.UninstallingTitle, "Stopping running applications...", 15, 15, 100);
            try
            {
                foreach (var proc in Process.GetProcessesByName("CustomClipboardManager"))
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(1500);
                    }
                    catch { }
                }
            }
            catch { }

            // 2. Stop and delete service
            UpdateProgress(loc.UninstallingTitle, "Stopping and removing CustomClipboardService...", 35, 35, 100);
            try
            {
                RunCommand("sc.exe", "stop CustomClipboardService");
                RunCommand("sc.exe", "delete CustomClipboardService");
            }
            catch { }

            // 3. Remove Startup registry
            UpdateProgress(loc.UninstallingTitle, "Removing startup configuration...", 55, 55, 100);
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    key?.DeleteValue("CustomClipboardManager", false);
                }
            }
            catch { }

            // 4. Remove Uninstall registry
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    hklm.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CustomClipboardManager", false);
                }
            }
            catch { }
            try
            {
                using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                {
                    hkcu.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CustomClipboardManager", false);
                }
            }
            catch { }

            // 5. Remove Shortcuts
            UpdateProgress(loc.UninstallingTitle, "Removing shortcuts...", 75, 75, 100);
            try
            {
                string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string commonDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
                string userStartMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Custom Clipboard Manager");
                string commonStartMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), "Custom Clipboard Manager");

                string lnk1 = Path.Combine(userDesktop, "Custom Clipboard Manager.lnk");
                string lnk2 = Path.Combine(commonDesktop, "Custom Clipboard Manager.lnk");

                if (File.Exists(lnk1)) File.Delete(lnk1);
                if (File.Exists(lnk2)) File.Delete(lnk2);
                if (Directory.Exists(userStartMenu)) Directory.Delete(userStartMenu, true);
                if (Directory.Exists(commonStartMenu)) Directory.Delete(commonStartMenu, true);
            }
            catch { }

            // 6. Delete installation directory files
            UpdateProgress(loc.UninstallingTitle, "Removing application files...", 90, 90, 100);
            string currentProcessExe = null;
            try
            {
                currentProcessExe = Process.GetCurrentProcess().MainModule?.FileName;
            }
            catch { }

            if (!string.IsNullOrEmpty(targetDir) && Directory.Exists(targetDir))
            {
                try
                {
                    var dirInfo = new DirectoryInfo(targetDir);
                    foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                    {
                        if (!string.IsNullOrEmpty(currentProcessExe) && string.Equals(file.FullName, currentProcessExe, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        try
                        {
                            file.Attributes = FileAttributes.Normal;
                            file.Delete();
                        }
                        catch { }
                    }

                    foreach (var subDir in dirInfo.GetDirectories("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            if (subDir.Exists && subDir.GetFiles().Length == 0 && subDir.GetDirectories().Length == 0)
                            {
                                subDir.Delete(true);
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                if (!string.IsNullOrEmpty(currentProcessExe) && currentProcessExe.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/C ping 127.0.0.1 -n 3 > nul & del /F /Q \"{currentProcessExe}\" & rmdir /S /Q \"{targetDir}\"",
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        Directory.Delete(targetDir, true);
                    }
                    catch { }
                }
            }

            UpdateProgress(loc.StatusComplete, "Done", 100, 100, 100);
        }
    }
}
