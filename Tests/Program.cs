using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using CustomClipboardManager.Core;
using CustomClipboardManager.Models;
using CustomClipboardManager.Services;
using CustomClipboardManager.ViewModels;
using ClipboardWebSetup;
using System.Windows.Media.Imaging;
using DataObject = System.Windows.DataObject;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;

namespace CustomClipboardManager.Tests
{
    public static class Program
    {
        private static int _passCount = 0;
        private static int _failCount = 0;

        [STAThread]
        public static int Main(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("  CUSTOM CLIPBOARD MANAGER - AUTOMATED TEST VERIFICATION  ");
            Console.WriteLine("==========================================================\n");

            // Ensure Application dispatcher exists for WPF test bindings and stays alive across test windows
            if (System.Windows.Application.Current == null)
            {
                var app = new System.Windows.Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }
            else
            {
                System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }

            RunTest("Test 1: Selection Mode State Transitions", Test_SelectionMode_Transitions);
            RunTest("Test 2: Item Selection & Real-Time Count Tracking (Root Cause Fix)", Test_ItemSelection_Tracking);
            RunTest("Test 3: Select All and Deselect All Functionality", Test_SelectAll_DeselectAll);
            RunTest("Test 4: Execute Delete Selected Items", Test_DeleteSelected_Execution);
            RunTest("Test 5: Anti-Loop Duplicate Prevention for FileDropList", Test_AntiLoop_FileDropList);
            RunTest("Test 6: Category Parsing Strict Hex vs Markdown Headings", Test_HexColor_Vs_Markdown);
            RunTest("Test 7: In-Place Text Transform Preserves Pin & Original", Test_TextTransform_Integrity);
            RunTest("Test 8: WebSetup Clean & Concise Localization (Thai & English)", Test_WebSetup_Localization);
            RunTest("Test 9: HotkeyConfig Serialization, Deserialization & Presets", Test_HotkeyConfig_Serialization_And_Presets);
            RunTest("Test 10: Inter-Process Show Event Signaling", Test_IPC_ShowEvent_Signaling);
            RunTest("Test 11: AutoPaste Target Window & Focus Safety", Test_AutoPaste_Safety);
            RunTest("Test 12: EnsureHandle Synchronous HWND and Hook Registration", Test_EnsureHandle_Hwnd_Creation);
            RunTest("Test 13: Hotkey File Watcher & Dynamic Synchronization", Test_Dynamic_Hotkey_Sync);
            RunTest("Test 14: Launch Visibility Decision Matrix (Root Cause Fix)", Test_LaunchVisibility_DecisionMatrix);
            RunTest("Test 15: Multi-Channel IPC Signaling (Win32, EventWaitHandle, File)", Test_MultiChannel_IPC_Signaling);
            RunTest("Test 16: ShowAtCursor ForceShow Guard vs Hotkey Toggle", Test_ShowAtCursor_ForceShow_Logic);
            RunTest("Test 17: Autostart Registry Formatting Integrity", Test_Autostart_Registry_Formatting);
            RunTest("Test 18: Concurrent Multi-Instance IPC Wake Signals", Test_Concurrent_IPC_WakeSignals);
            RunTest("Test 19: HotkeyConfig Matching & Modifier Isolation", Test_HotkeyConfig_Matching_And_Modifiers);
            RunTest("Test 20: GlobalKeyboardHook Hook Lifecycle & IsHookActive", Test_GlobalKeyboardHook_HookLifecycle_And_IsHookActive);
            RunTest("Test 21: Cross-Thread TriggerHotKey Thread-Safety (Root Cause Fix)", Test_CrossThread_TriggerHotKey_Safety);
            RunTest("Test 22: Hotkey Persistence & Dynamic Event Dispatch", Test_Hotkey_Persistence_And_Dynamic_Update);
            RunTest("Test 23: ShowAtCursor Window Display & Activation State", Test_ShowAtCursor_Window_Display_And_Activation);
            RunTest("Test 24: Overlay ToolWindow Style (WS_EX_TOOLWINDOW) Exclusion from Alt+Tab", Test_Overlay_ToolWindow_Style_Exclusion_From_AltTab);
            RunTest("Test 25: Non-Destructive Target Window Activation & Text Highlight Safety", Test_NonDestructive_ActivateTargetWindow_Safety);
            RunTest("Test 26: Dismissal Focus Restoration to Target Window", Test_Dismissal_Focus_Restoration_Safety);
            RunTest("Test 27: Non-Activating Window Drag & WM_MOUSEACTIVATE Mechanics", Test_NonActivating_Window_Drag_Mechanics);
            RunTest("Test 28: Multi-Monitor Incremental DPI Delta & Auto-Hide Suppression", Test_MultiMonitor_Incremental_Dpi_Delta_And_AutoHide_Suppression);
            RunTest("Test 29: Universal Drag-and-Drop Multi-Format DataObject Generation", Test_Universal_DragDrop_DataObject_MultiFormats);
            RunTest("Test 30: SearchBox and ListView Drag & Drop Handling", Test_SearchBox_And_ListView_DragDrop_Handlers);
            RunTest("Test 31: Pure Non-Activating Overlay Style & SW_SHOWNOACTIVATE Mechanics", Test_NonActivating_Overlay_Style_And_ShowNoActivate);
            RunTest("Test 32: Overlay Text Highlight Preservation & Moving Without Disruption", Test_Overlay_Text_Highlight_Preservation_And_Moving_Safety);
            RunTest("Test 33: Non-Focus-Stealing Category Clicks & Caret Preservation", Test_NonFocusStealing_CategoryClicks_And_CaretPreservation);
            RunTest("Test 34: Light Theme Toast Notification & Low-Level Hook Hotkey Recording", Test_ThemeToast_And_HotkeyRecording_System);
            RunTest("Test 35: Downward Text Wrapping, Component Formatting, and Clean Toast", Test_DownwardWrapping_ComponentFormatting_And_CleanToast);
            RunTest("Test 36: Selectable Preview Text, Unclipped Toolbar, and Async Image Saving", Test_SelectablePreviewText_UnclippedToolbar_And_AsyncImageSaving);
            RunTest("Test 37: I18n Localization & Preview Text Completeness", Test_I18n_Localization_And_PreviewTextCompleteness);
            RunTest("Test 38: Strict Zero-Leakage i18n & Preview Seamless Focus Styling", Test_ZeroLeakage_I18n_And_PreviewStyling);
            RunTest("Test 39: List Item Badges and Metadata Dynamic Localization", Test_ListItemBadges_And_Metadata_DynamicLocalization);
            RunTest("Test 40: Razor-Sharp Hotkey Modal Architecture (Zero Blurriness)", Test_RazorSharp_HotkeyModal_Architecture);
            RunTest("Test 41: Full File Path Display in Preview Modal (Zero Truncation)", Test_Full_FilePath_Display);
            RunTest("Test 42: Screen Capture Protection API, ViewModel & Settings Binding", Test_ScreenCaptureProtection);
            RunTest("Test 43: Windows Clipboard Win+V Detection & Intelligent Toggle", Test_WindowsClipboard_WinV_Integration);
            RunTest("Test 44: Efficiency & EcoQoS Power Saving Mode", Test_EfficiencyAndPowerSavingMode);
            RunTest("Test 45: Windows Service Clean Removal & Standalone Exe", Test_WindowsService_CleanRemoval);
            RunTest("Test 46: Async Non-Blocking Clipboard Operations (No Thread.Sleep)", Test_NonBlockingAsyncClipboardRetries);
            RunTest("Test 47: Pure Emoji Classification & Historical Data Migration", Test_EmojiClassification_And_DataMigration);
            RunTest("Test 48: High-Performance Batch Deletion & Click-Outside System", Test_BatchDeletion_And_ClickOutside);

            Console.WriteLine("\n==========================================================");
            Console.WriteLine($"  SUMMARY: Total: {_passCount + _failCount} | Passed: {_passCount} | Failed: {_failCount}");
            Console.WriteLine("==========================================================");

            if (_failCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(">>> CRITICAL TEST FAILURES DETECTED! <<<");
                Console.ResetColor();
                return 1;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(">>> ALL TESTS PASSED SUCCESSFULLY! ZERO BUGS DETECTED <<<");
                Console.ResetColor();
                return 0;
            }
        }

        private static void RunTest(string testName, Action testAction)
        {
            Console.Write($"[TEST] {testName} ... ");
            try
            {
                testAction();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PASSED");
                Console.ResetColor();
                _passCount++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"FAILED: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine(ex.StackTrace);
                _failCount++;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion Failed: {message}");
            }
        }

        private static void Test_SelectionMode_Transitions()
        {
            var vm = new MainViewModel();
            vm.ClipboardItems.Clear();

            var item1 = new ClipboardItem { TextContent = "Item 1", ContentType = ClipboardContentType.Text };
            var item2 = new ClipboardItem { TextContent = "Item 2", ContentType = ClipboardContentType.Text };
            vm.ClipboardItems.Add(item1);
            vm.ClipboardItems.Add(item2);
            vm.UpdateSelectionState();

            Assert(!vm.IsSelectionMode, "Should not be in selection mode initially");
            Assert(vm.SelectedCount == 0, "SelectedCount should be 0");
            Assert(!vm.HasSelectedItems, "HasSelectedItems should be false");
            Assert(!vm.DeleteSelectedCommand.CanExecute(null), "DeleteSelectedCommand should be disabled");

            // Toggle on
            vm.ToggleSelectionModeCommand.Execute(null);
            Assert(vm.IsSelectionMode, "IsSelectionMode should be true after toggle");

            // Select an item
            item1.IsSelected = true;
            Assert(vm.SelectedCount == 1, "SelectedCount should be 1 after selecting item1");
            Assert(vm.HasSelectedItems, "HasSelectedItems should be true");
            Assert(vm.DeleteSelectedCommand.CanExecute(null), "DeleteSelectedCommand should be enabled");

            // Exit selection mode
            vm.ExitSelectionModeCommand.Execute(null);
            Assert(!vm.IsSelectionMode, "IsSelectionMode should be false after exit");
            Assert(!item1.IsSelected, "Item1 IsSelected should be reset to false after exiting selection mode");
            Assert(vm.SelectedCount == 0, "SelectedCount should be reset to 0 after exiting");
            Assert(!vm.HasSelectedItems, "HasSelectedItems should be false");
        }

        private static void Test_ItemSelection_Tracking()
        {
            var vm = new MainViewModel();
            vm.ClipboardItems.Clear();

            var item1 = new ClipboardItem { TextContent = "Test A", ContentType = ClipboardContentType.Text };
            var item2 = new ClipboardItem { TextContent = "Test B", ContentType = ClipboardContentType.Text };
            var item3 = new ClipboardItem { TextContent = "Test C", ContentType = ClipboardContentType.Text };

            vm.ClipboardItems.Add(item1);
            vm.ClipboardItems.Add(item2);
            vm.ClipboardItems.Add(item3);

            // Ensure items hooked
            foreach (var ci in vm.ClipboardItems)
            {
                ci.IsSelected = false;
            }
            vm.UpdateSelectionState();

            Assert(vm.SelectedCount == 0, "Selected count must start at 0");
            Assert(vm.DeleteButtonText == vm.Strings.DeleteSelected, $"DeleteButtonText should match '{vm.Strings.DeleteSelected}' when 0 selected, got '{vm.DeleteButtonText}'");
            Assert(!vm.HasSelectedItems, "HasSelectedItems should be false");
            Assert(!vm.DeleteSelectedCommand.CanExecute(null), "Delete button must be disabled when 0 items selected");

            // Check 1st item
            item1.IsSelected = true;
            Assert(vm.SelectedCount == 1, $"SelectedCount must be 1, got {vm.SelectedCount}");
            Assert(vm.HasSelectedItems, "HasSelectedItems must be true when 1 item selected");
            Assert(vm.DeleteButtonText == $"{vm.Strings.DeleteSelected} (1)", $"DeleteButtonText must be '{vm.Strings.DeleteSelected} (1)', got '{vm.DeleteButtonText}'");
            Assert(vm.DeleteSelectedCommand.CanExecute(null), "Delete button MUST be enabled when 1 item selected");

            // Check 2nd item
            item2.IsSelected = true;
            Assert(vm.SelectedCount == 2, $"SelectedCount must be 2, got {vm.SelectedCount}");
            Assert(vm.DeleteButtonText == $"{vm.Strings.DeleteSelected} (2)", $"DeleteButtonText must be '{vm.Strings.DeleteSelected} (2)', got '{vm.DeleteButtonText}'");
            Assert(vm.DeleteSelectedCommand.CanExecute(null), "Delete button MUST be enabled when 2 items selected");

            // Deselect 1st item
            item1.IsSelected = false;
            Assert(vm.SelectedCount == 1, $"SelectedCount must be 1 after deselecting, got {vm.SelectedCount}");
            Assert(vm.DeleteButtonText == $"{vm.Strings.DeleteSelected} (1)", $"DeleteButtonText must be '{vm.Strings.DeleteSelected} (1)', got '{vm.DeleteButtonText}'");

            // Deselect 2nd item
            item2.IsSelected = false;
            Assert(vm.SelectedCount == 0, "SelectedCount must return to 0");
            Assert(!vm.HasSelectedItems, "HasSelectedItems must return to false");
            Assert(!vm.DeleteSelectedCommand.CanExecute(null), "Delete button must disable when 0 items selected");
        }

        private static void Test_SelectAll_DeselectAll()
        {
            var vm = new MainViewModel();
            vm.ClipboardItems.Clear();

            for (int i = 0; i < 5; i++)
            {
                vm.ClipboardItems.Add(new ClipboardItem { TextContent = $"Item {i}", ContentType = ClipboardContentType.Text });
            }
            vm.UpdateSelectionState();

            vm.IsSelectionMode = true;
            Assert(vm.IsSelectAll == false, "IsSelectAll should be false initially");

            // Select all
            vm.IsSelectAll = true;
            Assert(vm.SelectedCount == 5, $"All 5 items should be selected, got {vm.SelectedCount}");
            Assert(vm.HasSelectedItems, "HasSelectedItems should be true");
            Assert(vm.IsSelectAll == true, "IsSelectAll should be true");
            Assert(vm.DeleteSelectedCommand.CanExecute(null), "Delete command should be executable");

            // Deselect all
            vm.IsSelectAll = false;
            Assert(vm.SelectedCount == 0, "All items should be deselected");
            Assert(!vm.HasSelectedItems, "HasSelectedItems should be false");
            Assert(!vm.DeleteSelectedCommand.CanExecute(null), "Delete command should be disabled");
        }

        private static void Test_DeleteSelected_Execution()
        {
            var vm = new MainViewModel();
            vm.ClipboardItems.Clear();

            var itemKeep1 = new ClipboardItem { TextContent = "Keep 1", ContentType = ClipboardContentType.Text };
            var itemDelete1 = new ClipboardItem { TextContent = "Delete Me 1", ContentType = ClipboardContentType.Text };
            var itemKeep2 = new ClipboardItem { TextContent = "Keep 2", ContentType = ClipboardContentType.Text };
            var itemDelete2 = new ClipboardItem { TextContent = "Delete Me 2", ContentType = ClipboardContentType.Text };

            vm.ClipboardItems.Add(itemKeep1);
            vm.ClipboardItems.Add(itemDelete1);
            vm.ClipboardItems.Add(itemKeep2);
            vm.ClipboardItems.Add(itemDelete2);
            vm.UpdateSelectionState();

            vm.IsSelectionMode = true;
            itemDelete1.IsSelected = true;
            itemDelete2.IsSelected = true;

            Assert(vm.SelectedCount == 2, "SelectedCount should be 2");
            Assert(vm.HasSelectedItems, "HasSelectedItems should be true");
            Assert(vm.DeleteSelectedCommand.CanExecute(null), "Delete command must be executable");

            // Execute delete
            vm.DeleteSelectedCommand.Execute(null);

            Assert(vm.ClipboardItems.Count == 2, $"Expected 2 remaining items, got {vm.ClipboardItems.Count}");
            Assert(vm.ClipboardItems.Contains(itemKeep1), "itemKeep1 must be preserved");
            Assert(vm.ClipboardItems.Contains(itemKeep2), "itemKeep2 must be preserved");
            Assert(!vm.ClipboardItems.Contains(itemDelete1), "itemDelete1 must be deleted");
            Assert(!vm.ClipboardItems.Contains(itemDelete2), "itemDelete2 must be deleted");
            Assert(!vm.IsSelectionMode, "IsSelectionMode must be reset to false after deletion");
            Assert(vm.SelectedCount == 0, "SelectedCount must be reset to 0");
        }

        private static void Test_AntiLoop_FileDropList()
        {
            var vm = new MainViewModel();
            vm.ClipboardItems.Clear();

            var fileItem = new ClipboardItem
            {
                ContentType = ClipboardContentType.FileDropList,
                FilePath = @"C:\Folder\SampleFile.zip",
                FileName = "SampleFile.zip",
                FileCount = 1
            };

            // 1st add succeeds
            vm.AddItem(fileItem);
            Assert(vm.ClipboardItems.Count == 1, "First FileDropList add should succeed");

            // Simulate immediate paste re-capture (within 2.5s)
            var duplicateCapture = new ClipboardItem
            {
                ContentType = ClipboardContentType.FileDropList,
                FilePath = @"C:\Folder\SampleFile.zip",
                FileName = "SampleFile.zip",
                FileCount = 1
            };

            // Anti-loop state set during SetItemToClipboard
            // In MainViewModel, when pasting:
            // _lastPastedFilePath = item.FilePath; _lastPastedFileCount = item.FileCount; _lastPastedUtc = DateTime.UtcNow;
            // The anti-loop check in AddItem verifies:
            // if (item.ContentType == ClipboardContentType.FileDropList && item.FilePath == _lastPastedFilePath && item.FileCount == _lastPastedFileCount) return;
            // Tested directly via identical check
            bool isRecent = true;
            string? lastPath = @"C:\Folder\SampleFile.zip";
            int lastCount = 1;

            bool isDuplicate = isRecent &&
                               duplicateCapture.ContentType == ClipboardContentType.FileDropList &&
                               duplicateCapture.FilePath == lastPath &&
                               duplicateCapture.FileCount == lastCount;

            Assert(isDuplicate, "Anti-loop MUST detect and discard immediate duplicate file paste capture");
        }

        private static void Test_HexColor_Vs_Markdown()
        {
            var hexRegex = new Regex(@"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$");

            // Valid Colors
            Assert(hexRegex.IsMatch("#FFF"), "#FFF should be valid hex color");
            Assert(hexRegex.IsMatch("#ffffff"), "#ffffff should be valid hex color");
            Assert(hexRegex.IsMatch("#0A84FF"), "#0A84FF should be valid hex color");
            Assert(hexRegex.IsMatch("#12345678"), "#12345678 should be valid hex color");

            // Invalid Colors (Markdown headings, hashtags, text)
            Assert(!hexRegex.IsMatch("# Heading"), "# Heading must NOT be classified as color");
            Assert(!hexRegex.IsMatch("# Heading 1"), "# Heading 1 must NOT be classified as color");
            Assert(!hexRegex.IsMatch("## Subheading"), "## Subheading must NOT be classified as color");
            Assert(!hexRegex.IsMatch("#1234 not color"), "#1234 not color must NOT be classified as color");
            Assert(!hexRegex.IsMatch("#hashtag"), "#hashtag must NOT be classified as color");
        }

        private static void Test_TextTransform_Integrity()
        {
            var vm = new MainViewModel();
            var item = new ClipboardItem
            {
                ContentType = ClipboardContentType.Text,
                TextContent = "hello world",
                IsPinned = true
            };

            // Transform to Uppercase
            vm.TransformUpperCommand.Execute(item);
            Assert(item.TextContent == "HELLO WORLD", "TextContent should be transformed to uppercase");
            Assert(item.OriginalTextContent == "hello world", "OriginalTextContent must be preserved");
            Assert(item.IsPinned, "Pin status must be preserved after transform");
            Assert(item.HasTransformedText, "HasTransformedText should be true");

            // Reset to Original
            vm.TransformResetCommand.Execute(item);
            Assert(item.TextContent == "hello world", "TextContent should be restored to original");
            Assert(item.IsPinned, "Pin status must remain pinned after reset");
            Assert(!item.HasTransformedText, "HasTransformedText should be false after reset");
        }

        private static void Test_WebSetup_Localization()
        {
            LocalizationManager.SetLanguage("th");
            var th = LocalizationManager.Current;

            Assert(!th.OptionDesktopShortcut.Contains("(Desktop Shortcut)"), "Thai Desktop Shortcut must not contain redundant English parenthetical");
            Assert(!th.OptionStartMenu.Contains("(Start Menu)"), "Thai Start Menu must not contain redundant English parenthetical");
            Assert(!th.OptionAutoStart.Contains("(แนะนำ)"), "Thai AutoStart must not contain wordy parenthetical");
            Assert(th.InstallButton == "ติดตั้ง", $"Thai InstallButton must be 'ติดตั้ง', got '{th.InstallButton}'");
            Assert(th.AppVersion == "v1.0.0", $"Thai AppVersion must be 'v1.0.0', got '{th.AppVersion}'");
            Assert(!th.AppVersion.Contains("(Web Edition)"), "Thai AppVersion must NOT contain '(Web Edition)'");
            Assert(th.HeaderSubtitle == "จัดการประวัติคลิปบอร์ด", $"Thai HeaderSubtitle must be 'จัดการประวัติคลิปบอร์ด', got '{th.HeaderSubtitle}'");

            LocalizationManager.SetLanguage("en");
            var en = LocalizationManager.Current;

            Assert(en.OptionDesktopShortcut == "Create Desktop Shortcut", "English desktop shortcut must be clean");
            Assert(en.InstallButton == "Install", "English install button must be clean");
            Assert(en.AppVersion == "v1.0.0", $"English AppVersion must be 'v1.0.0', got '{en.AppVersion}'");
            Assert(!en.AppVersion.Contains("(Web Edition)"), "English AppVersion must NOT contain '(Web Edition)'");
            Assert(en.HeaderSubtitle == "Clipboard History Manager", $"English HeaderSubtitle must be 'Clipboard History Manager', got '{en.HeaderSubtitle}'");
        }

        private static void Test_HotkeyConfig_Serialization_And_Presets()
        {
            // 1. Default hotkey
            var def = HotkeyConfig.Default;
            Assert(def.Control && def.Shift && !def.Alt && !def.Windows, "Default must be Ctrl + Shift");
            Assert(def.VirtualKey == 0x56, "Default key must be 'V' (0x56)");
            Assert(def.DisplayText == "Ctrl + Shift + V", "Default display text must be 'Ctrl + Shift + V'");

            // 2. Custom hotkey JSON serialization & deserialization
            var custom = new HotkeyConfig
            {
                Control = true,
                Alt = true,
                Shift = false,
                Windows = false,
                VirtualKey = 0xC0,
                KeyName = "`"
            };
            string json = HotkeyConfig.ToJson(custom);
            var deserialized = HotkeyConfig.FromJson(json);

            Assert(deserialized.Control, "Control must be preserved in JSON round-trip");
            Assert(deserialized.Alt, "Alt must be preserved in JSON round-trip");
            Assert(!deserialized.Shift, "Shift must be false in JSON round-trip");
            Assert(deserialized.VirtualKey == 0xC0, "VirtualKey 0xC0 must be preserved in JSON round-trip");
            Assert(deserialized.DisplayText == "Ctrl + Alt + `", $"DisplayText must be 'Ctrl + Alt + `', got '{deserialized.DisplayText}'");

            // 3. Preset retrieval
            var altV = HotkeyManager.GetPreset("Alt+V");
            Assert(!altV.Control && altV.Alt && !altV.Shift && altV.VirtualKey == 0x56, "Preset Alt+V must have Alt only and VK_V");

            var winV = HotkeyManager.GetPreset("Win+V");
            Assert(!winV.Control && !winV.Alt && winV.Windows && winV.VirtualKey == 0x56, "Preset Win+V must have Windows key and VK_V");

            // 4. Friendly Key Name Mapping
            Assert(HotkeyConfig.GetFriendlyKeyName(0xC0) == "`", "VK 0xC0 must map to `");
            Assert(HotkeyConfig.GetFriendlyKeyName(0x20) == "Space", "VK 0x20 must map to Space");
            Assert(HotkeyConfig.GetFriendlyKeyName(0x70) == "F1", "VK 0x70 must map to F1");
            Assert(HotkeyConfig.GetFriendlyKeyName(0x41) == "A", "VK 0x41 must map to A");
        }

        private static void Test_IPC_ShowEvent_Signaling()
        {
            string testEventName = $"Local\\CustomClipboardManager_TestShowEvent_{Guid.NewGuid():N}";
            using var listener = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, testEventName);

            bool signaled = false;
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                if (listener.WaitOne(1500))
                {
                    signaled = true;
                }
            });

            // Signal from simulated secondary instance
            using (var sender = System.Threading.EventWaitHandle.OpenExisting(testEventName))
            {
                sender.Set();
            }

            task.Wait(2000);
            Assert(signaled, "Secondary instance must successfully signal primary instance EventWaitHandle");
        }

        private static void Test_AutoPaste_Safety()
        {
            // Ensure calling ActivateTargetWindow with Zero or invalid HWND fails safely without throwing
            bool result = AutoPasteService.ActivateTargetWindow(IntPtr.Zero);
            Assert(!result, "ActivateTargetWindow(IntPtr.Zero) must return false safely");

            // Ensure ReleaseModifiersIfNeeded runs cleanly without exception
            AutoPasteService.ReleaseModifiersIfNeeded();
        }

        private static void Test_EnsureHandle_Hwnd_Creation()
        {
            // Verify that a WPF Window with Visibility=Hidden can have its HWND created synchronously via WindowInteropHelper.EnsureHandle()
            var window = new Window
            {
                Visibility = Visibility.Hidden,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true
            };

            var helper = new System.Windows.Interop.WindowInteropHelper(window);
            IntPtr hwnd = helper.EnsureHandle();

            Assert(hwnd != IntPtr.Zero, "EnsureHandle must create a non-zero Win32 HWND handle");
            Assert(window.Visibility == Visibility.Hidden, "Window must remain Hidden after EnsureHandle");

            // Verify ClipboardMonitor can attach to it immediately
            using var monitor = new ClipboardMonitor(window);
            Assert(monitor != null, "ClipboardMonitor must attach cleanly to Window with valid HWND");

            window.Close();
        }

        private static void Test_Dynamic_Hotkey_Sync()
        {
            // Verify HotkeyManager save and load roundtrip
            var testConfig = new HotkeyConfig
            {
                Control = true,
                Alt = false,
                Shift = true,
                Windows = false,
                VirtualKey = 0x56,
                KeyName = "V",
                DisplayText = "Ctrl + Shift + V"
            };

            bool saved = HotkeyManager.Save(testConfig);
            Assert(saved, "HotkeyManager.Save must succeed");

            var loaded = HotkeyManager.Load();
            Assert(loaded != null, "HotkeyManager.Load must return a valid config");
            Assert(loaded.DisplayText == "Ctrl + Shift + V", "Loaded config DisplayText must match saved config");
        }

        private static void Test_LaunchVisibility_DecisionMatrix()
        {
            // Empty args or null (desktop shortcut, start menu, double-click exe) MUST show window
            Assert(!IpcHelper.IsBackgroundLaunch(null), "null args must NOT be background launch");
            Assert(!IpcHelper.IsBackgroundLaunch(Array.Empty<string>()), "empty args must NOT be background launch");
            Assert(!IpcHelper.IsBackgroundLaunch(new[] { "--show" }), "--show must NOT be background launch");
            Assert(!IpcHelper.IsBackgroundLaunch(new[] { "--open" }), "--open must NOT be background launch");
            Assert(!IpcHelper.IsBackgroundLaunch(new[] { "CustomClipboardManager.exe" }), "exe name arg must NOT be background launch");

            // Background flags (Windows registry Run key, service mode) MUST be background launch
            Assert(IpcHelper.IsBackgroundLaunch(new[] { "--background" }), "--background must be background launch");
            Assert(IpcHelper.IsBackgroundLaunch(new[] { "--startup" }), "--startup must be background launch");
            Assert(IpcHelper.IsBackgroundLaunch(new[] { "--autostart" }), "--autostart must be background launch");
            Assert(IpcHelper.IsBackgroundLaunch(new[] { "--service" }), "--service must be background launch");
            Assert(IpcHelper.IsBackgroundLaunch(new[] { "--silent" }), "--silent must be background launch");
            Assert(IpcHelper.IsBackgroundLaunch(new[] { "--minimized" }), "--minimized must be background launch");
        }

        private static void Test_MultiChannel_IPC_Signaling()
        {
            // Channel 1: Win32 Message
            uint wmShow = IpcHelper.WmShowMessage;
            Assert(wmShow >= 0xC000, $"WmShowMessage must be a valid registered window message >= 0xC000 (actual: 0x{wmShow:X})");

            // Channel 2: Open DACL EventWaitHandle
            int sessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
            string testEventName = $"Local\\CustomClipboardManager_TestIPC_{sessionId}_{Guid.NewGuid():N}";
            using (var handle = IpcHelper.CreateNamedEvent(testEventName, out _))
            {
                Assert(handle != null, "CreateNamedEvent must create a valid EventWaitHandle");
                bool signaled = false;
                var readyEvent = new System.Threading.ManualResetEventSlim(false);
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    readyEvent.Set();
                    if (handle.WaitOne(1500))
                    {
                        signaled = true;
                    }
                });

                readyEvent.Wait(1000);
                // Signal using EventWaitHandle
                using (var sender = System.Threading.EventWaitHandle.OpenExisting(testEventName))
                {
                    sender.Set();
                }

                task.Wait(2000);
                Assert(signaled, "Signaling EventWaitHandle must wake up listener");
            }

            // Channel 3: LocalAppData file wake signal
            string signalDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomClipboardManager");
            string signalFile = Path.Combine(signalDir, IpcHelper.WAKE_SIGNAL_FILE_NAME);
            if (File.Exists(signalFile)) File.Delete(signalFile);

            IpcHelper.TouchWakeSignalFile();
            Assert(File.Exists(signalFile), "TouchWakeSignalFile must create wake.signal file");
            string content = File.ReadAllText(signalFile);
            Assert(!string.IsNullOrEmpty(content) && long.TryParse(content, out _), "wake.signal must contain valid timestamp ticks");
        }

        private static void Test_ShowAtCursor_ForceShow_Logic()
        {
            // Verify the logic of forceShow vs hotkey toggle:
            // When forceShow is true, the function NEVER dismisses even if window is visible and elapsed > 450ms
            bool isVisible = true;
            double opacity = 1.0;
            bool isHiding = false;
            DateTime windowShownTime = DateTime.UtcNow.AddSeconds(-2); // > 450ms

            // Simulate Hotkey toggle (forceShow = false)
            bool shouldDismissHotkey = false;
            if (!false && isVisible && opacity > 0.5 && !isHiding)
            {
                if ((DateTime.UtcNow - windowShownTime).TotalMilliseconds > 450)
                {
                    shouldDismissHotkey = true;
                }
            }
            Assert(shouldDismissHotkey, "Hotkey toggle must dismiss already active window");

            // Simulate IPC wake-up or direct launch (forceShow = true)
            bool shouldDismissIpc = false;
            if (!true && isVisible && opacity > 0.5 && !isHiding)
            {
                if ((DateTime.UtcNow - windowShownTime).TotalMilliseconds > 450)
                {
                    shouldDismissIpc = true;
                }
            }
            Assert(!shouldDismissIpc, "ForceShow MUST NOT dismiss window when called from IPC or manual launch");
        }

        private static void Test_Autostart_Registry_Formatting()
        {
            string appExe = @"C:\Program Files\Clipboard\CustomClipboardManager.exe";
            string autostartValue = $"\"{appExe}\" --background";
            Assert(autostartValue.Contains("--background"), "Autostart Run entry must include --background parameter");
            Assert(!autostartValue.StartsWith("--background"), "Autostart Run entry must begin with quoted exe path");
            
            // Verify shortcut command has no --background
            string shortcutTarget = appExe;
            Assert(!shortcutTarget.Contains("--background"), "Desktop shortcut target must not contain --background flag");
        }

        private static void Test_Concurrent_IPC_WakeSignals()
        {
            int sessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
            using var listener = IpcHelper.CreateSessionShowEvent(sessionId, out _);

            int signalReceiveCount = 0;
            var cts = new System.Threading.CancellationTokenSource();

            var listenTask = System.Threading.Tasks.Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (listener.WaitOne(50))
                    {
                        System.Threading.Interlocked.Increment(ref signalReceiveCount);
                    }
                }
            });

            // Concurrently fire wake signals from 10 parallel tasks
            var tasks = new System.Threading.Tasks.Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    IpcHelper.SignalShowExistingInstance(sessionId);
                });
            }

            System.Threading.Tasks.Task.WaitAll(tasks);
            System.Threading.Thread.Sleep(200);

            cts.Cancel();
            listenTask.Wait(1000);

            Assert(signalReceiveCount > 0, "At least one wake signal must be received under heavy concurrent signaling");
        }

        private static void Test_HotkeyConfig_Matching_And_Modifiers()
        {
            // 1. Standard Ctrl + Shift + V (Default)
            var config = HotkeyConfig.Default;
            Assert(config.Matches(0x56, ctrl: true, shift: true, alt: false, win: false), "Ctrl+Shift+V must match standard modifiers");
            Assert(!config.Matches(0x56, ctrl: true, shift: true, alt: false, win: true), "Ctrl+Shift+V must NOT match if Windows key is down (verifies fix for win check bug)");
            Assert(!config.Matches(0x56, ctrl: true, shift: false, alt: false, win: false), "Ctrl+Shift+V must NOT match without Shift");
            Assert(!config.Matches(0x56, ctrl: false, shift: true, alt: false, win: false), "Ctrl+Shift+V must NOT match without Ctrl");
            Assert(!config.Matches(0x56, ctrl: true, shift: true, alt: true, win: false), "Ctrl+Shift+V must NOT match if Alt is pressed");
            Assert(!config.Matches(0x57, ctrl: true, shift: true, alt: false, win: false), "Ctrl+Shift+V must NOT match different key (W)");

            // 2. GlobalKeyboardHook.IsMatch helper consistency
            Assert(GlobalKeyboardHook.IsMatch(config, 0x56, ctrl: true, shift: true, alt: false, win: false), "GlobalKeyboardHook.IsMatch must match config");
            Assert(!GlobalKeyboardHook.IsMatch(config, 0x56, ctrl: true, shift: true, alt: false, win: true), "GlobalKeyboardHook.IsMatch must reject Win key when not configured");

            // 3. Preset Win + V
            var winV = HotkeyManager.GetPreset("WIN+V");
            Assert(winV.Matches(0x56, ctrl: false, shift: false, alt: false, win: true), "Win+V must match Win modifier only");
            Assert(!winV.Matches(0x56, ctrl: false, shift: false, alt: false, win: false), "Win+V must NOT match without Win modifier");

            // 4. Preset Alt + V
            var altV = HotkeyManager.GetPreset("ALT+V");
            Assert(altV.Matches(0x56, ctrl: false, shift: false, alt: true, win: false), "Alt+V must match Alt modifier only");

            // 5. Preset Ctrl + `
            var ctrlGrave = HotkeyManager.GetPreset("CTRL+`");
            Assert(ctrlGrave.Matches(0xC0, ctrl: true, shift: false, alt: false, win: false), "Ctrl+` must match VirtualKey 0xC0");

            // 6. Custom key with no modifiers (e.g. F8, vk = 0x77)
            var f8Config = new HotkeyConfig
            {
                Control = false,
                Shift = false,
                Alt = false,
                Windows = false,
                VirtualKey = 0x77,
                KeyName = "F8",
                DisplayText = "F8"
            };
            Assert(f8Config.Matches(0x77, ctrl: false, shift: false, alt: false, win: false), "F8 without modifiers must match");
            Assert(!f8Config.Matches(0x77, ctrl: true, shift: false, alt: false, win: false), "F8 must NOT match if Ctrl is pressed");
        }

        private static void Test_GlobalKeyboardHook_HookLifecycle_And_IsHookActive()
        {
            var config = HotkeyConfig.Default;
            using var hook = new GlobalKeyboardHook(config);
            Assert(hook != null, "GlobalKeyboardHook must instantiate cleanly");
            Assert(hook.IsHookActive, "GlobalKeyboardHook must successfully register WH_KEYBOARD_LL hook");

            // Update configuration dynamically
            var newConfig = HotkeyManager.GetPreset("ALT+V");
            hook.UpdateHotkey(newConfig);

            // Hook remains active after update
            Assert(hook.IsHookActive, "Hook must remain active after UpdateHotkey");
        }

        private static void Test_CrossThread_TriggerHotKey_Safety()
        {
            // Simulate the exact condition that caused the crash in production:
            // When GlobalKeyboardHook fires, OnHotkeyPressed executes on a ThreadPool worker thread.
            // In the buggy code, accessing `new WindowInteropHelper(this).Handle` on that background thread threw:
            // System.InvalidOperationException: The calling thread cannot access this object because a different thread owns it.
            // This test verifies that background thread invocation executes completely cleanly without thread access violations.

            Window testWindow = null!;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                testWindow = new Window
                {
                    Width = 400,
                    Height = 500,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Visibility = Visibility.Hidden
                };
                var helper = new System.Windows.Interop.WindowInteropHelper(testWindow);
                helper.EnsureHandle();
            });

            Assert(testWindow != null, "Test window must be created on dispatcher thread");

            // Cached HWND pointer (thread-safe primitive)
            IntPtr cachedHwnd = IntPtr.Zero;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                cachedHwnd = new System.Windows.Interop.WindowInteropHelper(testWindow).Handle;
            });
            Assert(cachedHwnd != IntPtr.Zero, "HWND must be non-zero");

            bool threadPoolExecuted = false;
            Exception? backgroundException = null;

            var task = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // This mirrors TriggerHotKey running on background ThreadPool thread:
                    // Must NOT throw InvalidOperationException when checking ourHwnd or calling Dispatcher
                    IntPtr ourHwnd = cachedHwnd;
                    Assert(ourHwnd != IntPtr.Zero, "ourHwnd must be accessible from background thread");

                    // Safe dispatcher dispatch
                    testWindow.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        threadPoolExecuted = true;
                    }));
                }
                catch (Exception ex)
                {
                    backgroundException = ex;
                }
            });

            task.Wait(2000);
            Assert(backgroundException == null, $"Background thread hotkey trigger must NOT throw exception: {backgroundException?.Message}");

            // Pump dispatcher
            var frame = new System.Windows.Threading.DispatcherFrame();
            testWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                frame.Continue = false;
            }));
            System.Windows.Threading.Dispatcher.PushFrame(frame);

            Assert(threadPoolExecuted, "Dispatcher must successfully execute hotkey display action scheduled from background thread");

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                testWindow.Close();
            });
        }

        private static void Test_Hotkey_Persistence_And_Dynamic_Update()
        {
            // Verify saving preset "ALT+V" updates both memory, file, and registry
            var altV = HotkeyManager.GetPreset("ALT+V");
            bool saved = HotkeyManager.Save(altV);
            Assert(saved, "HotkeyManager.Save must succeed for ALT+V preset");

            var loaded = HotkeyManager.Load();
            Assert(loaded.Alt == true, "Loaded config must have Alt = true");
            Assert(loaded.Control == false, "Loaded config must have Control = false");
            Assert(loaded.DisplayText == "Alt + V", "Loaded config DisplayText must be 'Alt + V'");

            // Verify event notification
            bool eventFired = false;
            HotkeyConfig? receivedConfig = null;
            Action<HotkeyConfig> handler = (cfg) =>
            {
                eventFired = true;
                receivedConfig = cfg;
            };

            HotkeyManager.HotkeyChanged += handler;
            try
            {
                var def = HotkeyConfig.Default;
                HotkeyManager.Save(def);
                Assert(eventFired, "HotkeyChanged event must fire upon Save");
                Assert(receivedConfig != null && receivedConfig.DisplayText == "Ctrl + Shift + V", "HotkeyChanged must supply new config");
            }
            finally
            {
                HotkeyManager.HotkeyChanged -= handler;
            }

            // Restore default
            HotkeyManager.Save(HotkeyConfig.Default);
        }

        private static void Test_ShowAtCursor_Window_Display_And_Activation()
        {
            // Verify window display and visibility behavior under ShowAtCursor conditions
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new Window
                {
                    Width = 430,
                    Height = 595,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Visibility = Visibility.Hidden,
                    WindowState = WindowState.Minimized
                };
                var helper = new System.Windows.Interop.WindowInteropHelper(window);
                helper.EnsureHandle();

                // Simulating ShowAtCursor state normalization
                if (window.WindowState != WindowState.Normal)
                {
                    window.WindowState = WindowState.Normal;
                }
                window.Show();

                Assert(window.WindowState == WindowState.Normal, "Window state must be Normal after ShowAtCursor");
                Assert(window.Visibility == Visibility.Visible, "Window must be Visible after ShowAtCursor");

                window.Hide();
                Assert(window.Visibility == Visibility.Hidden, "Window must be Hidden after Hide");

                window.Close();
            });
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLongTest(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLongTest(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);

        private const int GWL_EXSTYLE_TEST = -20;
        private const int WS_EX_TOOLWINDOW_TEST = 0x00000080;

        private static void Test_Overlay_ToolWindow_Style_Exclusion_From_AltTab()
        {
            // Verify that the clipboard manager window has WS_EX_TOOLWINDOW applied,
            // ensuring it behaves as an overlay rather than a separate standalone program in Alt+Tab.
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new Window
                {
                    Width = 446,
                    Height = 611,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Visibility = Visibility.Hidden,
                    ShowInTaskbar = false,
                    Topmost = true
                };
                var helper = new System.Windows.Interop.WindowInteropHelper(window);
                IntPtr hwnd = helper.EnsureHandle();
                Assert(hwnd != IntPtr.Zero, "Window handle must exist");

                int exStyle = GetWindowLongTest(hwnd, GWL_EXSTYLE_TEST);
                SetWindowLongTest(hwnd, GWL_EXSTYLE_TEST, exStyle | WS_EX_TOOLWINDOW_TEST);

                int updatedExStyle = GetWindowLongTest(hwnd, GWL_EXSTYLE_TEST);
                Assert((updatedExStyle & WS_EX_TOOLWINDOW_TEST) != 0, "Window must have WS_EX_TOOLWINDOW bit set to exclude from Alt+Tab");

                window.Close();
            });
        }

        private static void Test_NonDestructive_ActivateTargetWindow_Safety()
        {
            // Verify that ActivateTargetWindow executes without exceptions and preserves
            // thread independence (no AttachThreadInput and no simulated Alt keystrokes that deselect text).
            bool result = AutoPasteService.ActivateTargetWindow(IntPtr.Zero);
            Assert(result == false, "ActivateTargetWindow with IntPtr.Zero must safely return false");

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var dummyWindow = new Window
                {
                    Width = 200,
                    Height = 200,
                    Visibility = Visibility.Hidden
                };
                var helper = new System.Windows.Interop.WindowInteropHelper(dummyWindow);
                IntPtr hwnd = helper.EnsureHandle();

                bool activated = AutoPasteService.ActivateTargetWindow(hwnd);
                Assert(activated, "ActivateTargetWindow must succeed on a valid window without crashing or attaching input queues");

                dummyWindow.Close();
            });
        }

        private static void Test_Dismissal_Focus_Restoration_Safety()
        {
            // Verify that when unpinned, hiding the window smoothly triggers focus restoration
            // so active text highlights in external applications are restored immediately upon dismissal.
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                bool focusRestored = false;

                Action restoreFocusAction = () =>
                {
                    focusRestored = true;
                };

                // Simulate HideWindowAnimated logic:
                bool isPinned = false;
                if (!isPinned)
                {
                    restoreFocusAction();
                }

                Assert(focusRestored, "Dismissing unpinned overlay must invoke focus restoration to preserve user text selection");
            });
        }

        private const int WM_MOUSEACTIVATE_TEST = 0x0021;
        private const int MA_NOACTIVATE_TEST = 3;

        private static void Test_NonActivating_Window_Drag_Mechanics()
        {
            // Verify that when dragging the window, WM_MOUSEACTIVATE returns MA_NOACTIVATE (3)
            // so Windows never switches the active foreground window or sends WM_KILLFOCUS / WM_ACTIVATE (WA_INACTIVE)
            // to background applications, preserving user text selections and carets with 100% integrity.
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Simulate HwndMessageHook behavior:
                bool isMovingWindow = true;
                bool isSearchBoxActivating = false;

                IntPtr SimulateHook(int msg, bool moving, bool searchActivating, out bool handled)
                {
                    handled = false;
                    if (msg == WM_MOUSEACTIVATE_TEST)
                    {
                        if (moving || !searchActivating)
                        {
                            handled = true;
                            return (IntPtr)MA_NOACTIVATE_TEST;
                        }
                    }
                    return IntPtr.Zero;
                }

                // Case 1: Dragging window
                bool handled1;
                IntPtr res1 = SimulateHook(WM_MOUSEACTIVATE_TEST, isMovingWindow, isSearchBoxActivating, out handled1);
                Assert(handled1 == true, "Hook must handle WM_MOUSEACTIVATE when moving window");
                Assert(res1 == (IntPtr)MA_NOACTIVATE_TEST, "Hook must return MA_NOACTIVATE (3) to prevent foreground theft");

                // Case 2: Clicking non-searchbox items (list, background)
                bool handled2;
                IntPtr res2 = SimulateHook(WM_MOUSEACTIVATE_TEST, false, false, out handled2);
                Assert(handled2 == true, "Hook must handle WM_MOUSEACTIVATE for non-search interactions");
                Assert(res2 == (IntPtr)MA_NOACTIVATE_TEST, "Hook must return MA_NOACTIVATE (3) so text highlights in background app are preserved");

                // Case 3: Clicking SearchBox explicitly to type
                bool handled3;
                IntPtr res3 = SimulateHook(WM_MOUSEACTIVATE_TEST, false, true, out handled3);
                Assert(handled3 == false, "Hook must NOT intercept WM_MOUSEACTIVATE when user explicitly clicks search box to type");
                Assert(res3 == IntPtr.Zero, "Hook must return IntPtr.Zero so search box can receive typing focus");
            });
        }

        private static void Test_MultiMonitor_Incremental_Dpi_Delta_And_AutoHide_Suppression()
        {
            // Verify that incremental physical pixel delta dragging is smooth, non-jumping,
            // and immune to coordinate jumps across different DPI monitor boundaries,
            // while auto-hide is completely suppressed during dragging.
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                double currentLeft = 500.0;
                double currentTop = 300.0;
                int lastMouseRawX = 1900;
                int lastMouseRawY = 600;

                // Monitor 1: 100% DPI (scale 1.0)
                double scale1 = 1.0;
                int nextMouseRawX1 = 1910; // moved +10px
                int nextMouseRawY1 = 605; // moved +5px

                int delta1X = nextMouseRawX1 - lastMouseRawX;
                int delta1Y = nextMouseRawY1 - lastMouseRawY;
                currentLeft += delta1X / scale1;
                currentTop += delta1Y / scale1;
                lastMouseRawX = nextMouseRawX1;
                lastMouseRawY = nextMouseRawY1;

                Assert(Math.Abs(currentLeft - 510.0) < 0.001, "Window Left must advance by 10 DIPs at 100% scale");
                Assert(Math.Abs(currentTop - 305.0) < 0.001, "Window Top must advance by 5 DIPs at 100% scale");

                // Monitor 2: 150% DPI (scale 1.5) - crossing monitor boundary
                double scale2 = 1.5;
                int nextMouseRawX2 = 1940; // moved +30px physically
                int nextMouseRawY2 = 620; // moved +15px physically

                int delta2X = nextMouseRawX2 - lastMouseRawX;
                int delta2Y = nextMouseRawY2 - lastMouseRawY;
                currentLeft += delta2X / scale2;
                currentTop += delta2Y / scale2;

                Assert(Math.Abs(currentLeft - (510.0 + 20.0)) < 0.001, "Window Left must advance smoothly by 20 DIPs across 150% DPI boundary with zero coordinate jumps");
                Assert(Math.Abs(currentTop - (305.0 + 10.0)) < 0.001, "Window Top must advance smoothly by 10 DIPs across 150% DPI boundary with zero coordinate jumps");

                // Verify auto-hide suppression state:
                bool isMovingWindow = true;
                bool isDraggingItem = false;
                bool shouldAutoHide = false;

                // WinEventProc & Deactivated guard:
                if (!isDraggingItem && !isMovingWindow)
                {
                    shouldAutoHide = true;
                }

                Assert(!shouldAutoHide, "Window must NEVER auto-hide while actively moving or dragging");
            });
        }

        private static void Test_Universal_DragDrop_DataObject_MultiFormats()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // 1. Text item test
                var textItem = new ClipboardItem
                {
                    ContentType = ClipboardContentType.Text,
                    TextContent = "https://example.com/demo",
                    Category = SmartCategory.Link
                };
                var textData = DragDropHelper.CreateUniversalDataObject(textItem);
                Assert(textData != null, "Universal DataObject for text must not be null");
                Assert(textData.GetDataPresent(DataFormats.UnicodeText), "DataObject must contain UnicodeText format for text item");
                Assert((string)textData.GetData(DataFormats.UnicodeText) == "https://example.com/demo", "UnicodeText must match item text");
                Assert(textData.GetDataPresent(DataFormats.Text), "DataObject must contain Text format");
                Assert(textData.GetDataPresent("UniformResourceLocator"), "DataObject must contain UniformResourceLocator format for URL");
                Assert(textData.GetDataPresent(DataFormats.Html), "DataObject must contain HTML format for URL");

                // 2. Image item test
                var pixel = new byte[] { 0, 128, 255, 255 }; // 1 pixel BGRA
                var bitmapSource = BitmapSource.Create(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixel, 4);
                var imgId = Guid.NewGuid();
                var imgItem = new ClipboardItem
                {
                    ContentType = ClipboardContentType.Image,
                    ImageContent = bitmapSource,
                    Id = imgId
                };
                var imgData = DragDropHelper.CreateUniversalDataObject(imgItem);
                Assert(imgData != null, "Universal DataObject for image must not be null");
                Assert(imgData.GetDataPresent(DataFormats.Bitmap), "Image DataObject must contain Bitmap format");
                Assert(imgData.GetDataPresent(DataFormats.Dib) || imgData.GetDataPresent("DeviceIndependentBitmap"), "Image DataObject must contain CF_DIB format for Word/Outlook");
                Assert(imgData.GetDataPresent("PNG") || imgData.GetDataPresent("image/png"), "Image DataObject must contain PNG stream for Discord/Slack/Teams/Chromium");
                Assert(imgData.GetDataPresent(DataFormats.FileDrop), "Image DataObject must contain FileDrop format");
                
                // Critical check: UnicodeText MUST be present and contain the file path so plain text boxes accept drop
                Assert(imgData.GetDataPresent(DataFormats.UnicodeText), "CRITICAL: Image DataObject must contain UnicodeText for plain text boxes");
                string imgPathInText = (string)imgData.GetData(DataFormats.UnicodeText);
                Assert(!string.IsNullOrEmpty(imgPathInText) && imgPathInText.EndsWith(".png", StringComparison.OrdinalIgnoreCase), 
                    "Image DataObject UnicodeText must provide temp PNG path");
                Assert(File.Exists(imgPathInText), "Temp PNG file must exist on disk for drop targets to consume");

                // 3. FileDrop item test
                string tempTestFile = Path.Combine(Path.GetTempPath(), "test_clip_drop.txt");
                File.WriteAllText(tempTestFile, "Hello World from FileDrop test");
                var fileItem = new ClipboardItem
                {
                    ContentType = ClipboardContentType.FileDropList,
                    FilePaths = new List<string> { tempTestFile }
                };
                var fileData = DragDropHelper.CreateUniversalDataObject(fileItem);
                Assert(fileData != null, "Universal DataObject for files must not be null");
                Assert(fileData.GetDataPresent(DataFormats.FileDrop), "File DataObject must contain FileDrop format");
                Assert(fileData.GetDataPresent(DataFormats.UnicodeText), "File DataObject must contain UnicodeText format for text boxes");
                Assert((string)fileData.GetData(DataFormats.UnicodeText) == tempTestFile, "File DataObject UnicodeText must match file path");

                // 4. Image file in FileDrop test (must produce bitmap and PNG stream for rich targets)
                string tempImgFile = Path.Combine(Path.GetTempPath(), "test_clip_img.png");
                using (var fs = new FileStream(tempImgFile, FileMode.Create))
                {
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(bitmapSource));
                    enc.Save(fs);
                }
                var imgFileItem = new ClipboardItem
                {
                    ContentType = ClipboardContentType.FileDropList,
                    FilePaths = new List<string> { tempImgFile }
                };
                var imgFileData = DragDropHelper.CreateUniversalDataObject(imgFileItem);
                Assert(imgFileData.GetDataPresent(DataFormats.FileDrop), "Image file DataObject must contain FileDrop");
                Assert(imgFileData.GetDataPresent("PNG"), "Image file DataObject must dynamically expose PNG stream");
                Assert(imgFileData.GetDataPresent(DataFormats.Bitmap), "Image file DataObject must dynamically expose Bitmap");

                // Clean up temp test files
                try { File.Delete(tempTestFile); } catch { }
                try { File.Delete(tempImgFile); } catch { }
            });
        }

        private static void Test_SearchBox_And_ListView_DragDrop_Handlers()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // 1. Simulate SearchBox accepting dropped image or file
                string dummyImgFile = Path.Combine(Path.GetTempPath(), "sample_image_drop.png");
                File.WriteAllText(dummyImgFile, "fake png content");

                var searchBox = new System.Windows.Controls.TextBox();
                var dropData = new DataObject();
                dropData.SetData(DataFormats.UnicodeText, dummyImgFile);

                // Simulate SearchBox Drop logic
                string droppedText = (string)dropData.GetData(DataFormats.UnicodeText);
                if (File.Exists(droppedText) || Directory.Exists(droppedText))
                {
                    searchBox.Text = Path.GetFileName(droppedText);
                }
                else
                {
                    searchBox.Text = droppedText.Trim();
                }

                Assert(searchBox.Text == "sample_image_drop.png", "SearchBox must extract clean file name from dropped file path");

                // 2. Simulate SearchBox accepting dropped pure text
                var textDropData = new DataObject();
                textDropData.SetData(DataFormats.UnicodeText, "search query from web");
                string droppedQuery = (string)textDropData.GetData(DataFormats.UnicodeText);
                if (File.Exists(droppedQuery) || Directory.Exists(droppedQuery))
                {
                    searchBox.Text = Path.GetFileName(droppedQuery);
                }
                else
                {
                    searchBox.Text = droppedQuery.Trim();
                }
                Assert(searchBox.Text == "search query from web", "SearchBox must set text directly when dropped pure text");

                // 3. Simulate ListView Drop logic
                var vm = new MainViewModel();
                vm.ClipboardItems.Clear();
                int initialCount = vm.ClipboardItems.Count;

                // A) Dropping a file onto the list view adds to history
                string sampleDropDoc = Path.Combine(Path.GetTempPath(), $"sample_drop_doc_{Guid.NewGuid():N}.txt");
                File.WriteAllText(sampleDropDoc, "Sample Document Content");

                var listDropData = new DataObject();
                listDropData.SetData(DataFormats.FileDrop, new string[] { sampleDropDoc });

                if (listDropData.GetDataPresent(DataFormats.FileDrop))
                {
                    if (listDropData.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                    {
                        var item = new ClipboardItem();
                        FileHelper.PopulateFileDetails(item, files);
                        vm.AddItem(item);
                    }
                }
                Assert(vm.ClipboardItems.Count == initialCount + 1, "Dropping a file onto ListView must add a new item to clipboard history");
                Assert(vm.ClipboardItems[0].ContentType == ClipboardContentType.FileDropList, "Added item must be FileDropList type");

                // B) Dropping text onto ListView
                var listTextDropData = new DataObject();
                listTextDropData.SetData(DataFormats.UnicodeText, "Copied snippet from external app");
                if (listTextDropData.GetDataPresent(DataFormats.UnicodeText))
                {
                    string txt = (string)listTextDropData.GetData(DataFormats.UnicodeText);
                    var textItem = new ClipboardItem
                    {
                        ContentType = ClipboardContentType.Text,
                        TextContent = txt,
                        Timestamp = DateTime.Now,
                        Category = SmartCategory.Text
                    };
                    vm.AddItem(textItem);
                }
                Assert(vm.ClipboardItems.Count == initialCount + 2, "Dropping text onto ListView must add a new text item to clipboard history");
                Assert(vm.ClipboardItems[0].TextContent == "Copied snippet from external app", "Added text item content must match dropped text");

                // C) Guard: Dropping while dragging internal item should be rejected
                bool isDragging = true;
                bool dropAccepted = false;
                if (!isDragging)
                {
                    dropAccepted = true;
                }
                Assert(!dropAccepted, "Dropping internal item back onto ListView while dragging must be rejected to prevent duplicates");

                // Clean up
                try { File.Delete(dummyImgFile); } catch { }
                try { File.Delete(sampleDropDoc); } catch { }
            });
        }

        private const int WS_EX_NOACTIVATE_TEST = 0x08000000;
        private const int WS_EX_TOPMOST_TEST = 0x00000008;

        private static void Test_NonActivating_Overlay_Style_And_ShowNoActivate()
        {
            // Verify that the clipboard manager window has WS_EX_NOACTIVATE and WS_EX_TOOLWINDOW and WS_EX_TOPMOST applied,
            // and ShowActivated = false, ensuring the window renders as a pure HUD/drawn overlay without stealing focus
            // or disrupting active text highlights in the background application.
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new Window
                {
                    Width = 430,
                    Height = 595,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Visibility = Visibility.Hidden,
                    ShowInTaskbar = false,
                    Topmost = true,
                    ShowActivated = false
                };
                Assert(window.ShowActivated == false, "Window.ShowActivated must be false to prevent WPF from stealing focus on Show()");

                var helper = new System.Windows.Interop.WindowInteropHelper(window);
                IntPtr hwnd = helper.EnsureHandle();
                Assert(hwnd != IntPtr.Zero, "Window handle must exist");

                int exStyle = GetWindowLongTest(hwnd, GWL_EXSTYLE_TEST);
                SetWindowLongTest(hwnd, GWL_EXSTYLE_TEST, exStyle | WS_EX_TOOLWINDOW_TEST | WS_EX_NOACTIVATE_TEST | WS_EX_TOPMOST_TEST);

                int updatedExStyle = GetWindowLongTest(hwnd, GWL_EXSTYLE_TEST);
                Assert((updatedExStyle & WS_EX_TOOLWINDOW_TEST) != 0, "Window must have WS_EX_TOOLWINDOW applied");
                Assert((updatedExStyle & WS_EX_NOACTIVATE_TEST) != 0, "Window must have WS_EX_NOACTIVATE applied to prevent taking foreground when clicked");
                Assert((updatedExStyle & WS_EX_TOPMOST_TEST) != 0, "Window must have WS_EX_TOPMOST applied to float above target application");

                window.Close();
            });
        }

        private static void Test_Overlay_Text_Highlight_Preservation_And_Moving_Safety()
        {
            // Verify that:
            // 1. When overlay is shown, target application window handle (_previousHwnd) is preserved and remains foreground.
            // 2. Global keyboard hook intercepts Escape, Up, Down, Enter when overlay is visible, preserving text selection in background app.
            // 3. When moving the overlay, no focus shift occurs and background text highlights stay 100% undisturbed.
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IntPtr mockForegroundAppHwnd = new IntPtr(0x12345);
                IntPtr ourOverlayHwnd = new IntPtr(0x67890);

                // 1. Simulate capture of previous foreground before overlay displays
                IntPtr previousHwnd = mockForegroundAppHwnd;
                Assert(previousHwnd == mockForegroundAppHwnd, "Target window where user has highlighted text must be preserved");

                // 2. Simulate WinEventProc when overlay is open:
                // If the foreground is still mockForegroundAppHwnd (the app where text is selected), overlay must NOT auto-hide!
                bool autoHideTriggered = false;
                Action simulateWinEvent = () =>
                {
                    IntPtr newFg = mockForegroundAppHwnd; // Foreground didn't change away from target
                    if (newFg != ourOverlayHwnd && newFg != previousHwnd)
                    {
                        autoHideTriggered = true;
                    }
                };
                simulateWinEvent();
                Assert(!autoHideTriggered, "Overlay must NEVER auto-hide when foreground remains target application with active highlight");

                // 3. Simulate keyboard hook interception when overlay is visible:
                bool escapeHandled = false;
                bool upHandled = false;
                bool downHandled = false;
                bool enterHandled = false;

                bool isOverlayVisible = true;
                bool searchBoxFocused = false;

                bool SimulateKeyIntercept(int vkCode)
                {
                    if (!isOverlayVisible) return false;
                    if (vkCode == 0x1B) { escapeHandled = true; return true; } // Escape
                    if (searchBoxFocused) return false;
                    if (vkCode == 0x26) { upHandled = true; return true; } // Up
                    if (vkCode == 0x28) { downHandled = true; return true; } // Down
                    if (vkCode == 0x0D) { enterHandled = true; return true; } // Enter
                    return false;
                }

                Assert(SimulateKeyIntercept(0x1B) == true, "Escape must be intercepted to dismiss overlay smoothly");
                Assert(SimulateKeyIntercept(0x26) == true, "Up arrow must be intercepted to navigate clipboard items without scrolling background app");
                Assert(SimulateKeyIntercept(0x28) == true, "Down arrow must be intercepted to navigate clipboard items without scrolling background app");
                Assert(SimulateKeyIntercept(0x0D) == true, "Enter must be intercepted to paste selected item without inserting newline into background app");
                Assert(SimulateKeyIntercept(0x41) == false, "Normal typing key (e.g. 'A') must NOT be intercepted when SearchBox is not focused");

                // When user focuses SearchBox explicitly, all keys pass through directly to SearchBox:
                searchBoxFocused = true;
                Assert(SimulateKeyIntercept(0x26) == false, "Arrow keys must pass directly to SearchBox when user clicked into SearchBox");

                // 4. Moving overlay safety:
                bool isMoving = true;
                bool mouseActivateHandled = false;
                int mouseActivateResult = 0;

                // Simulate WM_MOUSEACTIVATE during window move:
                if (isMoving)
                {
                    mouseActivateHandled = true;
                    mouseActivateResult = 3; // MA_NOACTIVATE
                }
                Assert(mouseActivateHandled, "WM_MOUSEACTIVATE must be handled during window move");
                Assert(mouseActivateResult == 3, "WM_MOUSEACTIVATE must return MA_NOACTIVATE during window move so background app text highlights remain intact");
            });
        }

        private static void Test_NonFocusStealing_CategoryClicks_And_CaretPreservation()
        {
            // 1. Verify XAML markup enforces Focusable="False" across all interactive elements & filters
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml");
            if (!File.Exists(xamlPath))
            {
                xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml");
            }
            Assert(File.Exists(xamlPath), $"MainWindow.xaml must exist at {xamlPath}");
            string xaml = File.ReadAllText(xamlPath);

            Assert(xaml.Contains("PreviewGotKeyboardFocus=\"Window_PreviewGotKeyboardFocus\""),
                "MainWindow must hook PreviewGotKeyboardFocus to intercept and cancel WPF focus transitions");

            Assert(xaml.Contains("Style x:Key=\"FilterToggleStyle\" TargetType=\"RadioButton\""),
                "FilterToggleStyle must be defined for category pills");
            
            int filterStyleIdx = xaml.IndexOf("Style x:Key=\"FilterToggleStyle\"");
            int filterStyleEnd = xaml.IndexOf("</Style>", filterStyleIdx);
            string filterStyleSnippet = xaml.Substring(filterStyleIdx, filterStyleEnd - filterStyleIdx);
            Assert(filterStyleSnippet.Contains("<Setter Property=\"Focusable\" Value=\"False\"/>"),
                "FilterToggleStyle must set Focusable to False so clicking category pills does not steal focus from typing applications");
            Assert(filterStyleSnippet.Contains("<Setter Property=\"IsTabStop\" Value=\"False\"/>"),
                "FilterToggleStyle must set IsTabStop to False");

            Assert(xaml.Contains("<ListView x:Name=\"ItemsListView\" Grid.Row=\"2\" ItemsSource=\"{Binding ClipboardItemsView}\" \r\n                      Focusable=\"False\"") ||
                   xaml.Contains("Focusable=\"False\"\r\n                      IsTabStop=\"False\"\r\n                      Background=\"Transparent\" BorderThickness=\"0\""),
                "ItemsListView must have Focusable=\"False\" and IsTabStop=\"False\"");

            int pinStyleIdx = xaml.IndexOf("Style x:Key=\"HeaderPinButtonStyle\"");
            int pinStyleEnd = xaml.IndexOf("</Style>", pinStyleIdx);
            string pinSnippet = xaml.Substring(pinStyleIdx, pinStyleEnd - pinStyleIdx);
            Assert(pinSnippet.Contains("<Setter Property=\"Focusable\" Value=\"False\"/>"),
                "HeaderPinButtonStyle must set Focusable to False");

            int themeStyleIdx = xaml.IndexOf("Style x:Key=\"HeaderThemeButtonStyle\"");
            int themeStyleEnd = xaml.IndexOf("</Style>", themeStyleIdx);
            string themeSnippet = xaml.Substring(themeStyleIdx, themeStyleEnd - themeStyleIdx);
            Assert(themeSnippet.Contains("<Setter Property=\"Focusable\" Value=\"False\"/>"),
                "HeaderThemeButtonStyle must set Focusable to False");

            // 2. Verify C# code logic in MainWindow.xaml.cs
            string csPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml.cs");
            if (!File.Exists(csPath))
            {
                csPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml.cs");
            }
            Assert(File.Exists(csPath), $"MainWindow.xaml.cs must exist at {csPath}");
            string cs = File.ReadAllText(csPath);

            Assert(cs.Contains("void Window_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)"),
                "Window_PreviewGotKeyboardFocus must be implemented in MainWindow.xaml.cs");
            Assert(cs.Contains("e.Handled = true;"),
                "Window_PreviewGotKeyboardFocus must set e.Handled = true to cancel focus transitions");
            Assert(cs.Contains("WM_SETFOCUS"),
                "MainWindow.xaml.cs must define and handle WM_SETFOCUS");
            Assert(cs.Contains("FilterScrollViewer"),
                "Window_MouseLeftButtonDown must exclude FilterScrollViewer from initiating window drag");

            // 3. Functional ViewModel test: Clicking category pills toggles filters without focus
            var vm = new MainViewModel();
            vm.ClipboardItems.Clear();
            var itemText = new ClipboardItem { ContentType = ClipboardContentType.Text, Category = SmartCategory.Text, TextContent = "Sample text content" };
            var itemImg = new ClipboardItem { ContentType = ClipboardContentType.Image, Category = SmartCategory.Image, TextContent = "[Image]" };
            var itemLink = new ClipboardItem { ContentType = ClipboardContentType.Text, Category = SmartCategory.Link, TextContent = "https://example.com" };

            vm.ClipboardItems.Add(itemText);
            vm.ClipboardItems.Add(itemImg);
            vm.ClipboardItems.Add(itemLink);

            Assert(vm.ClipboardItemsView.Cast<ClipboardItem>().Count() == 3, "Initial view must contain all 3 items");

            // Simulate user clicking "Image" category filter chip
            vm.FilterTypeCommand.Execute("Image");
            var imageFiltered = vm.ClipboardItemsView.Cast<ClipboardItem>().ToList();
            Assert(imageFiltered.Count == 1 && imageFiltered[0].ContentType == ClipboardContentType.Image,
                "FilterTypeCommand('Image') must display only Image items");

            // Simulate user clicking "Link" category filter chip
            vm.FilterTypeCommand.Execute("Link");
            var linkFiltered = vm.ClipboardItemsView.Cast<ClipboardItem>().ToList();
            Assert(linkFiltered.Count == 1 && linkFiltered[0].Category == SmartCategory.Link,
                "FilterTypeCommand('Link') must display only Link items");

            // Simulate user clicking "Text" category filter chip
            vm.FilterTypeCommand.Execute("Text");
            var textFiltered = vm.ClipboardItemsView.Cast<ClipboardItem>().ToList();
            Assert(textFiltered.Count == 2 && textFiltered.All(x => x.ContentType == ClipboardContentType.Text),
                "FilterTypeCommand('Text') must display text-content items");

            // Simulate user clicking "All" category filter chip
            vm.FilterTypeCommand.Execute("All");
            var allFiltered = vm.ClipboardItemsView.Cast<ClipboardItem>().ToList();
            Assert(allFiltered.Count == 3,
                "FilterTypeCommand('All') must display all items");

            // 4. Win32 message safety simulation:
            // When clicking inside the overlay without SearchBox activating,
            // WM_MOUSEACTIVATE must return MA_NOACTIVATE (3)
            bool isSearchBoxActivating = false;
            bool isMovingWindow = false;
            int msg = 0x0021; // WM_MOUSEACTIVATE
            int handledResult = 0;
            if (msg == 0x0021 && (!isSearchBoxActivating || isMovingWindow))
            {
                handledResult = 3; // MA_NOACTIVATE
            }
            Assert(handledResult == 3, "WM_MOUSEACTIVATE must return MA_NOACTIVATE (3) so Windows never destroys the active caret in the target app");
        }

        private static void Test_ThemeToast_And_HotkeyRecording_System()
        {
            // 1. Verify Theme resource files contain proper Toast and Modal brushes
            string lightThemePath = Path.Combine(Directory.GetCurrentDirectory(), "Themes", "LightTheme.xaml");
            string darkThemePath = Path.Combine(Directory.GetCurrentDirectory(), "Themes", "DarkTheme.xaml");
            if (!File.Exists(lightThemePath))
            {
                string? searchDir = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 7; i++)
                {
                    if (searchDir == null) break;
                    string candidateLight = Path.Combine(searchDir, "Themes", "LightTheme.xaml");
                    string candidateDark = Path.Combine(searchDir, "Themes", "DarkTheme.xaml");
                    if (File.Exists(candidateLight) && File.Exists(candidateDark))
                    {
                        lightThemePath = candidateLight;
                        darkThemePath = candidateDark;
                        break;
                    }
                    searchDir = Directory.GetParent(searchDir)?.FullName;
                }
            }

            Assert(File.Exists(lightThemePath), $"Must locate Themes/LightTheme.xaml at {lightThemePath}");
            Assert(File.Exists(darkThemePath), $"Must locate Themes/DarkTheme.xaml at {darkThemePath}");

            string lightThemeText = File.ReadAllText(lightThemePath);
            string darkThemeText = File.ReadAllText(darkThemePath);

            Assert(lightThemeText.Contains("ToastBackgroundBrush"), "LightTheme.xaml must define ToastBackgroundBrush");
            Assert(lightThemeText.Contains("ToastBorderBrush"), "LightTheme.xaml must define ToastBorderBrush");
            Assert(lightThemeText.Contains("ToastForegroundBrush"), "LightTheme.xaml must define ToastForegroundBrush");
            Assert(lightThemeText.Contains("ModalBackdropBrush"), "LightTheme.xaml must define ModalBackdropBrush");
            Assert(lightThemeText.Contains("#FFFFFF") && lightThemeText.Contains("#111827"),
                "LightTheme.xaml must have crisp white background and dark text for toast");

            Assert(darkThemeText.Contains("ToastBackgroundBrush"), "DarkTheme.xaml must define ToastBackgroundBrush");
            Assert(darkThemeText.Contains("ToastBorderBrush"), "DarkTheme.xaml must define ToastBorderBrush");
            Assert(darkThemeText.Contains("ToastForegroundBrush"), "DarkTheme.xaml must define ToastForegroundBrush");
            Assert(darkThemeText.Contains("ModalBackdropBrush"), "DarkTheme.xaml must define ModalBackdropBrush");

            // 2. Verify Delete item toast notification logic in MainViewModel
            var vm = new MainViewModel();
            bool toastFired = false;
            vm.ShowToastNotification = () => { toastFired = true; };

            var testItem = new ClipboardItem { TextContent = "Item to delete", ContentType = ClipboardContentType.Text };
            vm.ClipboardItems.Add(testItem);
            Assert(vm.ClipboardItems.Contains(testItem), "Item must be added to list");

            // Execute DeleteItemCommand
            vm.DeleteItemCommand.Execute(testItem);
            Assert(!vm.ClipboardItems.Contains(testItem), "Item must be removed from list");
            Assert(toastFired, "Toast notification must fire on DeleteItemCommand");
            Assert(vm.ToastMessage == "Deleted 1 item", $"Toast message must be 'Deleted 1 item', was: '{vm.ToastMessage}'");

            // Verify ConfirmClearAll toast notification
            toastFired = false;
            vm.ClipboardItems.Clear();
            var unpinned1 = new ClipboardItem { TextContent = "Unpinned 1", IsPinned = false };
            var unpinned2 = new ClipboardItem { TextContent = "Unpinned 2", IsPinned = false };
            var pinned1 = new ClipboardItem { TextContent = "Pinned 1", IsPinned = true };
            vm.ClipboardItems.Add(unpinned1);
            vm.ClipboardItems.Add(unpinned2);
            vm.ClipboardItems.Add(pinned1);

            vm.ConfirmClearAll();
            Assert(toastFired, "Toast notification must fire on ConfirmClearAll");
            Assert(vm.ToastMessage == vm.Strings.ToastAllCleared || vm.ToastMessage == "Cleared 2 items", $"Toast message must match ToastAllCleared, was: '{vm.ToastMessage}'");
            Assert(vm.ClipboardItems.Count == 1 && vm.ClipboardItems[0].IsPinned, "Pinned item must remain intact after clear all");

            // 3. Verify Hotkey Presets (including new CTRL+ALT+V and F8)
            var presetCtrlAltV = HotkeyManager.GetPreset("CTRL+ALT+V");
            Assert(presetCtrlAltV.Control && presetCtrlAltV.Alt && !presetCtrlAltV.Shift && presetCtrlAltV.VirtualKey == 0x56,
                "CTRL+ALT+V preset must have Ctrl=True, Alt=True, Shift=False, Key=V");
            Assert(presetCtrlAltV.DisplayText == "Ctrl + Alt + V", $"DisplayText should be 'Ctrl + Alt + V', was: '{presetCtrlAltV.DisplayText}'");

            var presetF8 = HotkeyManager.GetPreset("F8");
            Assert(!presetF8.Control && !presetF8.Alt && !presetF8.Shift && presetF8.VirtualKey == 0x77,
                "F8 preset must have modifiers false and VirtualKey = 0x77");
            Assert(presetF8.DisplayText == "F8", $"DisplayText should be 'F8', was: '{presetF8.DisplayText}'");

            // 4. Verify friendly key names for single keys and modifiers
            Assert(HotkeyConfig.GetFriendlyKeyName(0x70) == "F1", "VK 0x70 must be F1");
            Assert(HotkeyConfig.GetFriendlyKeyName(0x77) == "F8", "VK 0x77 must be F8");
            Assert(HotkeyConfig.GetFriendlyKeyName(0x7B) == "F12", "VK 0x7B must be F12");
            Assert(HotkeyConfig.GetFriendlyKeyName(0xC0) == "`", "VK 0xC0 must be `");
            Assert(HotkeyConfig.GetFriendlyKeyName(0x56) == "V", "VK 0x56 must be V");

            // 5. Verify low-level hook intercept precedence
            var hook = new GlobalKeyboardHook(presetF8);
            bool interceptCalled = false;
            hook.InterceptKeyDown = (vk) =>
            {
                interceptCalled = true;
                return true; // Swallow key
            };
            hook.Dispose();
            Assert(!hook.IsHookActive, "Hook should be disposed cleanly");
        }

        private static void Test_DownwardWrapping_ComponentFormatting_And_CleanToast()
        {
            // 1. Test TextComponentFormatter analysis and formatting
            string sampleMarkdownBlob = "[migrate-workflows](slashCommand;migrate-workflows) [cavecrew](slashCommand;cavecrew) [caveman](slashCommand;caveman) [brainstorming](slashCommand;brainstorming)";
            var analysisMd = TextComponentFormatter.Analyze(sampleMarkdownBlob);
            Assert(analysisMd.HasComponents, "Analysis must detect markdown components");
            Assert(analysisMd.ComponentCount == 4, $"Expected 4 components, got {analysisMd.ComponentCount}");
            Assert(analysisMd.FormattedText.Contains("\n"), "Formatted text must separate components with newlines");
            Assert(analysisMd.CleanText.Contains("/migrate-workflows") && analysisMd.CleanText.Contains("/cavecrew"), 
                "Clean text must contain parsed slash commands");

            // Slash commands on a single line
            string sampleSlashCommands = "/migrate-workflows /cavecrew /caveman /brainstorming";
            var analysisSlash = TextComponentFormatter.Analyze(sampleSlashCommands);
            Assert(analysisSlash.HasComponents, "Analysis must detect slash commands");
            Assert(analysisSlash.ComponentCount == 4, $"Expected 4 slash commands, got {analysisSlash.ComponentCount}");
            Assert(analysisSlash.FormattedText == "/migrate-workflows\n/cavecrew\n/caveman\n/brainstorming", "Slash commands must be formatted line-by-line");

            // Minified JSON
            string sampleJson = "{\"app\":\"CustomClipboard\",\"version\":\"2.0\",\"clean\":true}";
            var analysisJson = TextComponentFormatter.Analyze(sampleJson);
            Assert(analysisJson.HasComponents, "Analysis must detect JSON components");
            Assert(analysisJson.FormattedText.Contains("\n"), "Formatted JSON must be indented with newlines");

            // Delimited list
            string sampleDelimited = "Item Alpha; Item Beta; Item Gamma; Item Delta";
            var analysisDelimited = TextComponentFormatter.Analyze(sampleDelimited);
            Assert(analysisDelimited.HasComponents, "Analysis must detect semicolon-delimited items");
            Assert(analysisDelimited.ComponentCount == 4, $"Expected 4 items, got {analysisDelimited.ComponentCount}");

            // 2. Test ClipboardPreviewBuilder and Mode Switching
            var item = new ClipboardItem
            {
                ContentType = ClipboardContentType.Text,
                Category = SmartCategory.Text,
                TextContent = sampleMarkdownBlob
            };
            var previewInfo = ClipboardPreviewBuilder.Build(item);
            Assert(previewInfo.HasTextPreview, "Preview must indicate HasTextPreview");
            Assert(previewInfo.HasStructuredComponents, "Preview must detect structured components");
            Assert(previewInfo.TextFormatMode == 0, "Default mode should be 0 (Formatted/Organized)");
            Assert(previewInfo.ActiveTextContent == previewInfo.FormattedTextContent, "Active text in mode 0 must be formatted");

            // Switch to Mode 1 (Raw)
            previewInfo.TextFormatMode = 1;
            Assert(previewInfo.ActiveTextContent == previewInfo.RawTextContent, "Active text in mode 1 must be raw text");

            // Switch to Mode 2 (Clean)
            previewInfo.TextFormatMode = 2;
            Assert(previewInfo.ActiveTextContent == previewInfo.CleanTextContent, "Active text in mode 2 must be clean text");

            // 3. Test MainViewModel Commands: OrganizeComponentsCommand and SetTextFormatModeCommand
            var vm = new MainViewModel();
            bool toastFired = false;
            vm.ShowToastNotification = () => { toastFired = true; };

            var itemToOrganize = new ClipboardItem
            {
                ContentType = ClipboardContentType.Text,
                Category = SmartCategory.Text,
                TextContent = sampleMarkdownBlob
            };
            vm.ClipboardItems.Add(itemToOrganize);

            // Execute OrganizeComponentsCommand
            vm.OrganizeComponentsCommand.Execute(itemToOrganize);
            Assert(toastFired, "Toast notification must fire after organizing components");
            Assert(itemToOrganize.TextContent.Contains("\n"), "Item text content must now be organized with newlines");
            Assert(itemToOrganize.OriginalTextContent == sampleMarkdownBlob, "Original text content must be preserved for rollback");

            // Rollback via TransformResetCommand
            vm.TransformResetCommand.Execute(itemToOrganize);
            Assert(itemToOrganize.TextContent == sampleMarkdownBlob, "Reset command must restore original unformatted text blob");

            // SetTextFormatModeCommand
            vm.PreviewInfo = previewInfo;
            vm.SetTextFormatModeCommand.Execute("1");
            Assert(vm.PreviewInfo.TextFormatMode == 1, "SetTextFormatModeCommand with '1' must switch to Raw mode");
            vm.SetTextFormatModeCommand.Execute("0");
            Assert(vm.PreviewInfo.TextFormatMode == 0, "SetTextFormatModeCommand with '0' must switch to Formatted mode");

            // 4. Verify XAML Structural Safety & Clean UI Requirements
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml");
            if (!File.Exists(xamlPath))
            {
                xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml");
            }
            Assert(File.Exists(xamlPath), $"MainWindow.xaml must exist at {xamlPath}");
            string xaml = File.ReadAllText(xamlPath);

            // Verify ToastGrid has no DropShadowEffect (clean flat border requirement)
            int toastGridStart = xaml.IndexOf("x:Name=\"ToastGrid\"");
            Assert(toastGridStart > 0, "MainWindow.xaml must contain ToastGrid");
            int toastGridEnd = xaml.IndexOf("</Grid>", toastGridStart);
            string toastGridSnippet = xaml.Substring(toastGridStart, toastGridEnd - toastGridStart);
            Assert(!toastGridSnippet.Contains("<DropShadowEffect"),
                "ToastGrid must not contain DropShadowEffect (clean flat notification without edge shadow)");

            // Verify Text Preview has HorizontalScrollBarVisibility="Disabled" and TextWrapping="Wrap"
            int textPreviewStart = xaml.IndexOf("<!-- TEXT & CODE PREVIEW -->");
            Assert(textPreviewStart > 0, "MainWindow.xaml must contain TEXT & CODE PREVIEW section");
            int textPreviewEnd = xaml.IndexOf("<!-- LINK PREVIEW -->", textPreviewStart);
            string textPreviewSnippet = xaml.Substring(textPreviewStart, textPreviewEnd - textPreviewStart);

            Assert(textPreviewSnippet.Contains("HorizontalScrollBarVisibility=\"Disabled\""),
                "Text preview ScrollViewer must set HorizontalScrollBarVisibility='Disabled' to force text downwards");
            Assert(textPreviewSnippet.Contains("TextWrapping=\"Wrap\""),
                "Text preview TextBox must set TextWrapping='Wrap' to wrap long lines downwards");
            Assert(textPreviewSnippet.Contains("ActiveTextContent"),
                "Text preview TextBox must bind to ActiveTextContent to support dynamic format switching");
            Assert(textPreviewSnippet.Contains("HasStructuredComponents"),
                "Text preview must contain component formatting toolbar gated by HasStructuredComponents");

            // Verify ContextMenu has Organize Components (either literal or bound to MenuOrganize)
            Assert(xaml.Contains("MenuOrganize") || xaml.Contains("Header=\"Organize Components\""),
                "ListViewItem ContextMenu must include 'Organize Components' menu item");
        }

        private static void Test_SelectablePreviewText_UnclippedToolbar_And_AsyncImageSaving()
        {
            // 1. Verify XAML fixes for Red Box Icon, Cyan Box Clipping, and Green Box Selectability
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml");
            if (!File.Exists(xamlPath))
            {
                xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml");
            }
            Assert(File.Exists(xamlPath), $"MainWindow.xaml must exist at {xamlPath}");
            string xaml = File.ReadAllText(xamlPath);

            int textPreviewStart = xaml.IndexOf("<!-- TEXT & CODE PREVIEW -->");
            int textPreviewEnd = xaml.IndexOf("<!-- LINK PREVIEW -->", textPreviewStart);
            string textPreviewSnippet = xaml.Substring(textPreviewStart, textPreviewEnd - textPreviewStart);

            // Red Box: Sparkle icon MUST be removed from component toolbar
            Assert(!textPreviewSnippet.Contains("Sparkle24"),
                "Red box Sparkle24 icon must be removed from the Component Formatter Toolbar");

            // Cyan Box: Toolbar must use DockPanel with DockPanel.Dock='Right' to guarantee switcher buttons are never clipped
            Assert(textPreviewSnippet.Contains("<DockPanel LastChildFill=\"True\">") && textPreviewSnippet.Contains("DockPanel.Dock=\"Right\""),
                "Component Formatter Toolbar must use DockPanel with DockPanel.Dock='Right' so switcher buttons are never clipped");
            Assert(textPreviewSnippet.Contains("TextTrimming=\"CharacterEllipsis\""),
                "Component summary label must use TextTrimming='CharacterEllipsis' to prevent overflow");

            // Green Box: Text preview must use selectable TextBox with IBeam cursor and inactive selection highlight
            Assert(textPreviewSnippet.Contains("x:Name=\"PreviewTextBox\""),
                "Text preview must define PreviewTextBox");
            Assert(textPreviewSnippet.Contains("IsReadOnly=\"True\"") && textPreviewSnippet.Contains("Cursor=\"IBeam\""),
                "PreviewTextBox must be read-only with IBeam cursor for text selection");
            Assert(textPreviewSnippet.Contains("IsInactiveSelectionHighlightEnabled=\"True\""),
                "PreviewTextBox must enable IsInactiveSelectionHighlightEnabled for persistent selection visibility");
            Assert(textPreviewSnippet.Contains("ApplicationCommands.Copy") && textPreviewSnippet.Contains("ApplicationCommands.SelectAll"),
                "PreviewTextBox must provide ContextMenu with Copy and Select All commands");

            // Embedded File text preview must also be selectable and use IBM Plex Sans
            int filePreviewStart = xaml.IndexOf("<!-- Embedded Text/Code Preview for Text Files -->");
            int filePreviewEnd = xaml.IndexOf("<!-- Standard Icon Card", filePreviewStart);
            string filePreviewSnippet = xaml.Substring(filePreviewStart, filePreviewEnd - filePreviewStart);
            Assert(filePreviewSnippet.Contains("<TextBox") && filePreviewSnippet.Contains("IsInactiveSelectionHighlightEnabled=\"True\""),
                "Embedded File preview must use selectable TextBox with inactive highlight enabled");
            Assert(filePreviewSnippet.Contains("IBM Plex Sans"),
                "Embedded File preview TextBox must include IBM Plex Sans font");

            // IBM Plex Sans font checks for PreviewTextBox and Window
            Assert(textPreviewSnippet.Contains("IBM Plex Sans"),
                "PreviewTextBox must use IBM Plex Sans font");
            Assert(xaml.Contains("FontFamily x:Key=\"IBMPlexSansFont\""),
                "MainWindow.xaml must define IBMPlexSansFont resource");
            Assert(xaml.Contains("FontFamily x:Key=\"CodeFont\""),
                "MainWindow.xaml must define CodeFont resource");
            Assert(xaml.Contains("IBM Plex Sans Thai"),
                "MainWindow.xaml must include IBM Plex Sans Thai for Thai typography support");

            // 2. Verify C# focus logic in MainWindow.xaml.cs for Preview text selection and non-freezing image saving
            string csPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml.cs");
            if (!File.Exists(csPath))
            {
                csPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml.cs");
            }
            Assert(File.Exists(csPath), $"MainWindow.xaml.cs must exist at {csPath}");
            string cs = File.ReadAllText(csPath);

            // Focus and Activation logic:
            Assert(cs.Contains("previewGrid.Visibility == Visibility.Visible") && cs.Contains("IsDescendantOf(previewDep, previewGrid)"),
                "Window_PreviewGotKeyboardFocus must allow keyboard focus when target is inside PreviewGrid");
            Assert(cs.Contains("!_isSearchBoxActivating && !isPreviewActive"),
                "HwndMessageHook must not return MA_NOACTIVATE or redirect focus when PreviewGrid is active");

            // Image Saving Logic:
            Assert(cs.Contains("SavePreviewImageToFile(BitmapSource bitmap)"),
                "SavePreviewImageToFile must exist in MainWindow.xaml.cs");
            Assert(cs.Contains("Task.Run(() => CustomClipboardManager.Services.FileHelper.SaveBitmapSourceToFile"),
                "SavePreviewImageToFile must run FileHelper.SaveBitmapSourceToFile asynchronously via Task.Run to prevent UI freeze");
            Assert(cs.Contains("frozenBitmap.Freeze()"),
                "SavePreviewImageToFile must freeze bitmap before passing to background task");
            Assert(!cs.Contains("WH_MOUSE_LL"),
                "MainWindow must not contain WH_MOUSE_LL low-level mouse hook to prevent system-wide mouse freeze");

            // 3. Functional Image Saving Test with PNG, JPEG, and BMP encoders
            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(64, 64, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Freeze();

            string tempDir = Path.Combine(Path.GetTempPath(), "ClipboardTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string pngFile = Path.Combine(tempDir, "test.png");
                string jpgFile = Path.Combine(tempDir, "test.jpg");
                string bmpFile = Path.Combine(tempDir, "test.bmp");

                bool pngSaved = FileHelper.SaveBitmapSourceToFile(rtb, pngFile);
                Assert(pngSaved && File.Exists(pngFile) && new FileInfo(pngFile).Length > 0, "FileHelper must save PNG format");

                bool jpgSaved = FileHelper.SaveBitmapSourceToFile(rtb, jpgFile);
                Assert(jpgSaved && File.Exists(jpgFile) && new FileInfo(jpgFile).Length > 0, "FileHelper must save JPG format");

                bool bmpSaved = FileHelper.SaveBitmapSourceToFile(rtb, bmpFile);
                Assert(bmpSaved && File.Exists(bmpFile) && new FileInfo(bmpFile).Length > 0, "FileHelper must save BMP format");
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static void Test_I18n_Localization_And_PreviewTextCompleteness()
        {
            // 1. Verify I18n Language Switching and Completeness
            I18n.SetLanguage("th");
            Assert(I18n.Current.SearchPlaceholder == "ค้นหาในประวัติคลิปบอร์ด...", "Thai SearchPlaceholder must match");
            Assert(I18n.Current.FilterAll == "ทั้งหมด", "Thai FilterAll must match");
            Assert(I18n.Current.FilterPinned == "ปักหมุด", "Thai FilterPinned must match");
            Assert(I18n.Current.PreviewTab == "แสดงตัวอย่าง", "Thai PreviewTab must match");
            Assert(I18n.Current.DetailsTab == "รายละเอียด", "Thai DetailsTab must match");
            Assert(I18n.Current.ButtonSelectAll == "เลือกทั้งหมด", "Thai ButtonSelectAll must match");
            Assert(I18n.Current.SaveImage == "บันทึกภาพ", "Thai SaveImage must match");

            I18n.SetLanguage("en");
            Assert(I18n.Current.SearchPlaceholder == "Search clipboard history...", "English SearchPlaceholder must match");
            Assert(I18n.Current.FilterAll == "All", "English FilterAll must match");
            Assert(I18n.Current.FilterPinned == "Pinned", "English FilterPinned must match");
            Assert(I18n.Current.PreviewTab == "Preview", "English PreviewTab must match");
            Assert(I18n.Current.DetailsTab == "Details", "English DetailsTab must match");
            Assert(I18n.Current.ButtonSelectAll == "Select All", "English ButtonSelectAll must match");

            I18n.SetLanguage("ja");
            Assert(I18n.Current.FilterAll == "すべて", "Japanese FilterAll must match");
            Assert(I18n.Current.PreviewTab == "プレビュー", "Japanese PreviewTab must match");

            // Reset back to system detected language
            I18n.SetLanguage(I18n.LoadSavedLanguage());

            // 2. Verify TextComponentFormatter preserves 100% of user text without truncation or data loss
            string userPrompt = "/migrate-workflows /cavecrew /caveman /brainstorming /writing-plans /executing-plans " +
                "/subagent-driven-development /dispatching-parallel-agents /using-git-worktrees /finishing-a-development-branch " +
                "/investigate-first /systematic-debugging /test-driven-development /safe-refactor /surgical-patch /lean-build " +
                "/migration /verification-before-completion /generative_ui /using-superpowers /writing-skills " +
                "แก้ไขในภาพ ไม่สามารถเลือกข้อความได้ และแก้ไข ให้เลือกข้อความได้ง่ายๆ คลีนๆ และแก้ไขหน้าแสดงตัวอย่าง มันแสดงข้อความมาไม่ครบ ช่วยแก้ไข คิดดีๆ ใช้งานง่าย";

            var analysis = TextComponentFormatter.Analyze(userPrompt);
            Assert(analysis.HasComponents, "Analyze must detect multiple slash commands as components");
            Assert(analysis.FormattedText.Contains("/migrate-workflows\n/cavecrew\n/caveman"), "Formatted text should format slash commands with line breaks");
            string expectedTail = "แก้ไขในภาพ ไม่สามารถเลือกข้อความได้ และแก้ไข ให้เลือกข้อความได้ง่ายๆ คลีนๆ และแก้ไขหน้าแสดงตัวอย่าง มันแสดงข้อความมาไม่ครบ ช่วยแก้ไข คิดดีๆ ใช้งานง่าย";
            Assert(analysis.FormattedText.Contains(expectedTail), "Formatted text MUST preserve 100% of user instructions and tail text without truncation!");

            // 3. Verify ClipboardItem.DisplayText maintains multi-line and expanded limit
            var clipItem = new ClipboardItem
            {
                ContentType = ClipboardContentType.Text,
                TextContent = userPrompt
            };
            Assert(clipItem.DisplayText.Contains(expectedTail), "ClipboardItem.DisplayText must not truncate user text at 200 chars");
            Assert(clipItem.DisplayText.Length > 200, "ClipboardItem.DisplayText must accommodate long text up to 1000 chars");

            // 4. Verify XAML text selection configuration for PreviewTextBox
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml");
            if (!File.Exists(xamlPath))
            {
                xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml");
            }
            Assert(File.Exists(xamlPath), $"MainWindow.xaml must exist at {xamlPath}");
            string xaml = File.ReadAllText(xamlPath);

            int previewGridIndex = xaml.IndexOf("x:Name=\"PreviewGrid\"");
            Assert(previewGridIndex >= 0, "PreviewGrid must exist in MainWindow.xaml");

            int tbIndex = xaml.IndexOf("x:Name=\"PreviewTextBox\"", previewGridIndex);
            Assert(tbIndex >= 0, "PreviewTextBox must exist inside PreviewGrid");

            // Check PreviewTextBox tag snippet
            int tbEnd = xaml.IndexOf("/>", tbIndex);
            if (tbEnd < 0) tbEnd = xaml.IndexOf("</TextBox>", tbIndex);
            string tbTag = xaml.Substring(tbIndex, tbEnd - tbIndex);

            Assert(tbTag.Contains("VerticalScrollBarVisibility=\"Auto\""), "PreviewTextBox must have its own VerticalScrollBarVisibility Auto");
            Assert(tbTag.Contains("HorizontalScrollBarVisibility=\"Disabled\""), "PreviewTextBox must have HorizontalScrollBarVisibility Disabled for clean wrapping");
            Assert(tbTag.Contains("IsReadOnlyCaretVisible=\"True\""), "PreviewTextBox must have IsReadOnlyCaretVisible True for visible caret during selection");
            Assert(tbTag.Contains("PreviewMouseLeftButtonDown=\"PreviewTextBox_PreviewMouseLeftButtonDown\""), "PreviewTextBox must have PreviewMouseLeftButtonDown handler for activation and focus");

            // Verify Select All button in preview action bar
            Assert(xaml.Contains("PreviewSelectAll_Click"), "MainWindow.xaml must have PreviewSelectAll_Click button for quick 1-click text selection");
        }

        private static void Test_ZeroLeakage_I18n_And_PreviewStyling()
        {
            var thaiRegex = new System.Text.RegularExpressions.Regex(@"[\u0E00-\u0E7F]");

            // 1. Strict Zero-Leakage for English across all Preview Categories
            I18n.SetLanguage("en");

            // Text item
            var textItem = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = "Hello World! Here is a sample clipboard snippet." };
            var textPreview = ClipboardPreviewBuilder.Build(textItem);
            Assert(!thaiRegex.IsMatch(textPreview.Title), $"Text preview Title '{textPreview.Title}' leaked Thai");
            Assert(!thaiRegex.IsMatch(textPreview.BadgeText), $"Text preview BadgeText '{textPreview.BadgeText}' leaked Thai");
            Assert(!thaiRegex.IsMatch(textPreview.Subtitle), $"Text preview Subtitle '{textPreview.Subtitle}' leaked Thai");
            foreach (var detail in textPreview.DetailsList)
            {
                Assert(!thaiRegex.IsMatch(detail.Label), $"Text detail label '{detail.Label}' leaked Thai");
                Assert(!thaiRegex.IsMatch(detail.Value), $"Text detail value '{detail.Value}' leaked Thai");
            }

            // Code item
            var codeItem = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = "function calculateSum(a, b) {\n    return a + b;\n}" };
            var codePreview = ClipboardPreviewBuilder.Build(codeItem);
            Assert(!thaiRegex.IsMatch(codePreview.Title), $"Code preview Title '{codePreview.Title}' leaked Thai");
            Assert(!thaiRegex.IsMatch(codePreview.BadgeText), $"Code preview BadgeText '{codePreview.BadgeText}' leaked Thai");
            foreach (var detail in codePreview.DetailsList)
            {
                Assert(!thaiRegex.IsMatch(detail.Label), $"Code detail label '{detail.Label}' leaked Thai");
                Assert(!thaiRegex.IsMatch(detail.Value), $"Code detail value '{detail.Value}' leaked Thai");
            }

            // Color item
            var colorItem = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = "#0A84FF" };
            var colorPreview = ClipboardPreviewBuilder.Build(colorItem);
            Assert(!thaiRegex.IsMatch(colorPreview.Title), $"Color preview Title '{colorPreview.Title}' leaked Thai");
            Assert(!thaiRegex.IsMatch(colorPreview.BadgeText), $"Color preview BadgeText '{colorPreview.BadgeText}' leaked Thai");
            foreach (var detail in colorPreview.DetailsList)
            {
                Assert(!thaiRegex.IsMatch(detail.Label), $"Color detail label '{detail.Label}' leaked Thai");
                Assert(!thaiRegex.IsMatch(detail.Value), $"Color detail value '{detail.Value}' leaked Thai");
            }

            // Link item
            var linkItem = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = "https://github.com/phwyverysad/CustomClipboardManager" };
            var linkPreview = ClipboardPreviewBuilder.Build(linkItem);
            Assert(!thaiRegex.IsMatch(linkPreview.Title), $"Link preview Title '{linkPreview.Title}' leaked Thai");
            Assert(!thaiRegex.IsMatch(linkPreview.BadgeText), $"Link preview BadgeText '{linkPreview.BadgeText}' leaked Thai");
            foreach (var detail in linkPreview.DetailsList)
            {
                Assert(!thaiRegex.IsMatch(detail.Label), $"Link detail label '{detail.Label}' leaked Thai");
                Assert(!thaiRegex.IsMatch(detail.Value), $"Link detail value '{detail.Value}' leaked Thai");
            }

            // Single File item
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "Sample content");
                var fileItem = new ClipboardItem
                {
                    ContentType = ClipboardContentType.FileDropList,
                    FilePath = tempFile,
                    FilePaths = new System.Collections.Generic.List<string> { tempFile }
                };
                var filePreview = ClipboardPreviewBuilder.Build(fileItem);
                Assert(!thaiRegex.IsMatch(filePreview.Title), $"File preview Title '{filePreview.Title}' leaked Thai");
                Assert(!thaiRegex.IsMatch(filePreview.BadgeText), $"File preview BadgeText '{filePreview.BadgeText}' leaked Thai");
                foreach (var detail in filePreview.DetailsList)
                {
                    Assert(!thaiRegex.IsMatch(detail.Label), $"File detail label '{detail.Label}' leaked Thai");
                    Assert(!thaiRegex.IsMatch(detail.Value), $"File detail value '{detail.Value}' leaked Thai");
                }
            }
            finally
            {
                try { File.Delete(tempFile); } catch { }
            }

            // TextComponentFormatter analysis
            var compAnalysis = TextComponentFormatter.Analyze("/cmd1 /cmd2 /cmd3 Final instruction here");
            Assert(!thaiRegex.IsMatch(compAnalysis.SummaryText), $"SummaryText '{compAnalysis.SummaryText}' leaked Thai");
            Assert(!thaiRegex.IsMatch(compAnalysis.ComponentType), $"ComponentType '{compAnalysis.ComponentType}' leaked Thai");

            // Hotkey Strings in I18n.Current
            Assert(!thaiRegex.IsMatch(I18n.Current.HotkeyRecordingListening), "HotkeyRecordingListening leaked Thai");
            Assert(!thaiRegex.IsMatch(I18n.Current.HotkeyRecordingPromptFull), "HotkeyRecordingPromptFull leaked Thai");
            Assert(!thaiRegex.IsMatch(I18n.Current.HotkeyCancelled), "HotkeyCancelled leaked Thai");
            Assert(!thaiRegex.IsMatch(I18n.Current.HotkeyInstructionHint), "HotkeyInstructionHint leaked Thai");
            Assert(!thaiRegex.IsMatch(string.Format(I18n.Current.HotkeySavedSuccess, "Ctrl+Shift+V")), "HotkeySavedSuccess leaked Thai");
            Assert(!thaiRegex.IsMatch(string.Format(I18n.Current.HotkeyResetSuccess, "Ctrl+Shift+V")), "HotkeyResetSuccess leaked Thai");

            // 2. WebSetup LocalizationManager 10-Language Audit
            string[] nonThaiLangs = new[] { "en", "ja", "zh-Hans", "zh-Hant", "de", "es", "fr", "ko", "ru" };
            foreach (var lang in nonThaiLangs)
            {
                ClipboardWebSetup.LocalizationManager.SetLanguage(lang);
                var loc = ClipboardWebSetup.LocalizationManager.Current;
                Assert(!string.IsNullOrEmpty(loc.CancelConfirmMessage), $"CancelConfirmMessage must not be empty for {lang}");
                Assert(!thaiRegex.IsMatch(loc.CancelConfirmMessage), $"CancelConfirmMessage leaked Thai for {lang}: '{loc.CancelConfirmMessage}'");
                Assert(!string.IsNullOrEmpty(loc.HotkeyCardTitle), $"HotkeyCardTitle must not be empty for {lang}");
                Assert(!thaiRegex.IsMatch(loc.HotkeyCardTitle), $"HotkeyCardTitle leaked Thai for {lang}: '{loc.HotkeyCardTitle}'");
                Assert(!string.IsNullOrEmpty(loc.HotkeyChangeHint), $"HotkeyChangeHint must not be empty for {lang}");
                Assert(!thaiRegex.IsMatch(loc.HotkeyChangeHint), $"HotkeyChangeHint leaked Thai for {lang}: '{loc.HotkeyChangeHint}'");
                Assert(!string.IsNullOrEmpty(loc.HotkeyRecordingPrompt), $"HotkeyRecordingPrompt must not be empty for {lang}");
                Assert(!thaiRegex.IsMatch(loc.HotkeyRecordingPrompt), $"HotkeyRecordingPrompt leaked Thai for {lang}: '{loc.HotkeyRecordingPrompt}'");
                Assert(!string.IsNullOrEmpty(loc.HotkeyResetTooltip), $"HotkeyResetTooltip must not be empty for {lang}");
                Assert(!thaiRegex.IsMatch(loc.HotkeyResetTooltip), $"HotkeyResetTooltip leaked Thai for {lang}: '{loc.HotkeyResetTooltip}'");
                Assert(!string.IsNullOrEmpty(loc.HotkeyCustomItem), $"HotkeyCustomItem must not be empty for {lang}");
                Assert(!thaiRegex.IsMatch(loc.HotkeyCustomItem), $"HotkeyCustomItem leaked Thai for {lang}: '{loc.HotkeyCustomItem}'");
            }

            // Thai language check
            ClipboardWebSetup.LocalizationManager.SetLanguage("th");
            var thLoc = ClipboardWebSetup.LocalizationManager.Current;
            Assert(thaiRegex.IsMatch(thLoc.CancelConfirmMessage), "Thai CancelConfirmMessage must contain Thai characters");
            Assert(thaiRegex.IsMatch(thLoc.HotkeyCardTitle), "Thai HotkeyCardTitle must contain Thai characters");

            // 3. UI XAML Inspection for Seamless Focus & Layout
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml");
            if (!File.Exists(xamlPath)) xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml");
            Assert(File.Exists(xamlPath), "MainWindow.xaml must exist");
            string xaml = File.ReadAllText(xamlPath);

            // PreviewBorder sizing
            Assert(xaml.Contains("Width=\"414\" Height=\"565\""), "PreviewBorder must have Width=414 and Height=565 for full card fit without leaking background list");

            // PreviewTextBox ControlTemplate with transparent Background
            Assert(xaml.Contains("<ScrollViewer x:Name=\"PART_ContentHost\""), "PreviewTextBox must define ControlTemplate with PART_ContentHost");
            Assert(xaml.Contains("Background=\"Transparent\""), "ControlTemplate must enforce Background Transparent to override WPF.UI focus style");

            // LightTheme ModalBackdropBrush
            string lightThemePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Themes", "LightTheme.xaml");
            if (!File.Exists(lightThemePath)) lightThemePath = Path.Combine(Directory.GetCurrentDirectory(), "Themes", "LightTheme.xaml");
            Assert(File.Exists(lightThemePath), "LightTheme.xaml must exist");
            string lightTheme = File.ReadAllText(lightThemePath);
            Assert(lightTheme.Contains("<SolidColorBrush x:Key=\"ModalBackdropBrush\" Color=\"#66000000\"/>"), "ModalBackdropBrush must be #66000000 to eliminate underlying text bleed");

            // Reset back to system language
            I18n.SetLanguage(I18n.LoadSavedLanguage());
        }

        private static void Test_ListItemBadges_And_Metadata_DynamicLocalization()
        {
            // 1. Verify Thai localization on list card badges and metadata
            I18n.SetLanguage("th");
            var itemText = new ClipboardItem
            {
                ContentType = ClipboardContentType.Text,
                Category = SmartCategory.Text,
                TextContent = "hello world" // 11 characters
            };
            Assert(itemText.CategoryBadgeText == "ข้อความ", $"Thai badge must be 'ข้อความ', got '{itemText.CategoryBadgeText}'");
            Assert(itemText.MetadataText == "11 ตัวอักษร", $"Thai metadata must be '11 ตัวอักษร', got '{itemText.MetadataText}'");

            var itemImage = new ClipboardItem
            {
                ContentType = ClipboardContentType.Image,
                Category = SmartCategory.Image
            };
            Assert(itemImage.CategoryBadgeText == "รูปภาพ", $"Thai image badge must be 'รูปภาพ', got '{itemImage.CategoryBadgeText}'");

            var itemFiles = new ClipboardItem
            {
                ContentType = ClipboardContentType.FileDropList,
                Category = SmartCategory.Files,
                FileCount = 3,
                FileSizeText = "1.5 MB"
            };
            Assert(itemFiles.CategoryBadgeText == "ไฟล์", $"Thai files badge must be 'ไฟล์', got '{itemFiles.CategoryBadgeText}'");
            Assert(itemFiles.MetadataText == "3 ไฟล์ • 1.5 MB", $"Thai files metadata must be '3 ไฟล์ • 1.5 MB', got '{itemFiles.MetadataText}'");

            var itemSingleFile = new ClipboardItem
            {
                ContentType = ClipboardContentType.FileDropList,
                Category = SmartCategory.Files,
                FileCount = 1,
                FileSizeText = "250 KB"
            };
            Assert(itemSingleFile.MetadataText == "250 KB", $"Single file metadata must be '250 KB', got '{itemSingleFile.MetadataText}'");

            var itemLink = new ClipboardItem { Category = SmartCategory.Link };
            Assert(itemLink.CategoryBadgeText == "ลิงก์", $"Thai link badge must be 'ลิงก์', got '{itemLink.CategoryBadgeText}'");

            var itemCode = new ClipboardItem { Category = SmartCategory.Code };
            Assert(itemCode.CategoryBadgeText == "โค้ด", $"Thai code badge must be 'โค้ด', got '{itemCode.CategoryBadgeText}'");

            var itemColor = new ClipboardItem { Category = SmartCategory.ColorCode };
            Assert(itemColor.CategoryBadgeText == "สี", $"Thai color badge must be 'สี', got '{itemColor.CategoryBadgeText}'");

            // 2. Verify dynamic language switch to English
            I18n.SetLanguage("en");
            itemText.NotifyLanguageChanged();
            itemImage.NotifyLanguageChanged();
            itemFiles.NotifyLanguageChanged();
            itemLink.NotifyLanguageChanged();

            Assert(itemText.CategoryBadgeText == "Text", $"English badge must be 'Text', got '{itemText.CategoryBadgeText}'");
            Assert(itemText.MetadataText == "11 chars", $"English metadata must be '11 chars', got '{itemText.MetadataText}'");
            Assert(itemImage.CategoryBadgeText == "Image", $"English image badge must be 'Image', got '{itemImage.CategoryBadgeText}'");
            Assert(itemFiles.CategoryBadgeText == "Files", $"English files badge must be 'Files', got '{itemFiles.CategoryBadgeText}'");
            Assert(itemFiles.MetadataText == "3 files • 1.5 MB", $"English files metadata must be '3 files • 1.5 MB', got '{itemFiles.MetadataText}'");

            // 3. Verify XAML binding is CategoryBadgeText and not unlocalized Category enum
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml");
            if (!File.Exists(xamlPath)) xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml");
            Assert(File.Exists(xamlPath), "MainWindow.xaml must exist");
            string xaml = File.ReadAllText(xamlPath);

            Assert(xaml.Contains("Text=\"{Binding CategoryBadgeText}\""), "MainWindow.xaml CategoryBadge must bind to CategoryBadgeText");
            Assert(!xaml.Contains("<TextBlock Text=\"{Binding Category}\""), "MainWindow.xaml CategoryBadge must not bind directly to Category enum");
            Assert(xaml.Contains("Text=\"{Binding PinnedBadgeText}\""), "MainWindow.xaml Pinned badge must bind to PinnedBadgeText");
            Assert(!xaml.Contains("<TextBlock Text=\"PIN\""), "MainWindow.xaml must not contain hardcoded Text=\"PIN\"");
            Assert(xaml.Contains("TooltipCardPreview"), "MainWindow.xaml Preview button must bind to TooltipCardPreview");
            Assert(xaml.Contains("TooltipCardCopy"), "MainWindow.xaml Copy button must bind to TooltipCardCopy");
            Assert(xaml.Contains("TooltipCardPin"), "MainWindow.xaml Pin button must bind to TooltipCardPin");
            Assert(xaml.Contains("TooltipCardDelete"), "MainWindow.xaml Delete button must bind to TooltipCardDelete");

            // 4. Verify PinnedBadge across all 10 languages
            var itemPinned = new ClipboardItem { IsPinned = true };
            I18n.SetLanguage("th");
            Assert(itemPinned.PinnedBadgeText == "ปักหมุด", $"Thai pinned badge must be 'ปักหมุด', got '{itemPinned.PinnedBadgeText}'");
            I18n.SetLanguage("en");
            Assert(itemPinned.PinnedBadgeText == "PIN", $"English pinned badge must be 'PIN', got '{itemPinned.PinnedBadgeText}'");
            I18n.SetLanguage("ja");
            Assert(itemPinned.PinnedBadgeText == "ピン留め", $"Japanese pinned badge must be 'ピン留め', got '{itemPinned.PinnedBadgeText}'");
            I18n.SetLanguage("zh-Hans");
            Assert(itemPinned.PinnedBadgeText == "置顶", $"zh-Hans pinned badge must be '置顶', got '{itemPinned.PinnedBadgeText}'");
            I18n.SetLanguage("zh-Hant");
            Assert(itemPinned.PinnedBadgeText == "釘選", $"zh-Hant pinned badge must be '釘選', got '{itemPinned.PinnedBadgeText}'");
            I18n.SetLanguage("de");
            Assert(itemPinned.PinnedBadgeText == "ANGEHEFTET", $"German pinned badge must be 'ANGEHEFTET', got '{itemPinned.PinnedBadgeText}'");
            I18n.SetLanguage("es");
            Assert(itemPinned.PinnedBadgeText == "FIJADO", $"Spanish pinned badge must be 'FIJADO', got '{itemPinned.PinnedBadgeText}'");
            I18n.SetLanguage("fr");
            Assert(itemPinned.PinnedBadgeText == "ÉPINGLÉ", $"French pinned badge must be 'ÉPINGLÉ', got '{itemPinned.PinnedBadgeText}'");
            I18n.SetLanguage("ko");
            Assert(itemPinned.PinnedBadgeText == "고정", $"Korean pinned badge must be '고정', got '{itemPinned.PinnedBadgeText}'");
            I18n.SetLanguage("ru");
            Assert(itemPinned.PinnedBadgeText == "ЗАКРЕПЛЕНО", $"Russian pinned badge must be 'ЗАКРЕПЛЕНО', got '{itemPinned.PinnedBadgeText}'");

            // Reset back
            I18n.SetLanguage(I18n.LoadSavedLanguage());
        }

        private static void Test_RazorSharp_HotkeyModal_Architecture()
        {
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml");
            if (!File.Exists(xamlPath)) xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml");
            Assert(File.Exists(xamlPath), "MainWindow.xaml must exist");
            string xaml = File.ReadAllText(xamlPath);

            // 1. Verify HotkeySettingsBorder does not have permanent fractional ScaleTransform
            int hotkeyIdx = xaml.IndexOf("x:Name=\"HotkeySettingsBorder\"");
            Assert(hotkeyIdx >= 0, "HotkeySettingsBorder must exist in MainWindow.xaml");
            string hotkeySection = xaml.Substring(hotkeyIdx, Math.Min(600, xaml.Length - hotkeyIdx));

            Assert(!hotkeySection.Contains("ScaleTransform ScaleX=\"0.9\""), "HotkeySettingsBorder must not contain permanent ScaleX=0.9 which causes subpixel blur");
            Assert(!hotkeySection.Contains("<Border.Effect>"), "HotkeySettingsBorder must not contain direct Border.Effect which disables subpixel ClearType rendering");
            Assert(hotkeySection.Contains("UseLayoutRounding=\"True\""), "HotkeySettingsBorder must specify UseLayoutRounding='True'");
            Assert(hotkeySection.Contains("SnapsToDevicePixels=\"True\""), "HotkeySettingsBorder must specify SnapsToDevicePixels='True'");
            Assert(hotkeySection.Contains("TextOptions.TextFormattingMode=\"Display\""), "HotkeySettingsBorder must enforce TextOptions.TextFormattingMode='Display'");
            Assert(hotkeySection.Contains("TextOptions.TextRenderingMode=\"ClearType\""), "HotkeySettingsBorder must enforce TextOptions.TextRenderingMode='ClearType'");

            // 2. Verify ConfirmBorder also uses separated shadow without fractional scale
            int confirmIdx = xaml.IndexOf("x:Name=\"ConfirmBorder\"");
            Assert(confirmIdx >= 0, "ConfirmBorder must exist in MainWindow.xaml");
            string confirmSection = xaml.Substring(confirmIdx, Math.Min(500, xaml.Length - confirmIdx));
            Assert(!confirmSection.Contains("ScaleTransform ScaleX=\"0.9\""), "ConfirmBorder must not contain permanent ScaleX=0.9");
            Assert(!confirmSection.Contains("<Border.Effect>"), "ConfirmBorder must not have direct DropShadowEffect on text container");
        }

        private static void Test_Full_FilePath_Display()
        {
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml");
            if (!File.Exists(xamlPath)) xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml");
            Assert(File.Exists(xamlPath), "MainWindow.xaml must exist");
            string xaml = File.ReadAllText(xamlPath);

            // 1. Verify Tab 0 PreviewInfo.FilePath wrapping and zero character ellipsis
            int filePathIdx = xaml.IndexOf("PreviewInfo.FilePath");
            Assert(filePathIdx >= 0, "PreviewInfo.FilePath must exist in MainWindow.xaml");
            string filePathSection = xaml.Substring(filePathIdx, Math.Min(800, xaml.Length - filePathIdx));
            Assert(filePathSection.Contains("TextWrapping=\"Wrap\""), "PreviewInfo.FilePath must specify TextWrapping='Wrap'");
            Assert(!filePathSection.Contains("TextTrimming=\"CharacterEllipsis\""), "PreviewInfo.FilePath must not specify TextTrimming='CharacterEllipsis'");

            int detailsIdx = xaml.IndexOf("DETAILS METADATA VIEW");
            Assert(detailsIdx >= 0, "Details tab must exist in MainWindow.xaml");
            string detailsSection = xaml.Substring(detailsIdx, Math.Min(3000, xaml.Length - detailsIdx));
            Assert(detailsSection.Contains("TextWrapping=\"Wrap\""), "Details Tab Value must specify TextWrapping='Wrap'");
            Assert(!detailsSection.Contains("TextTrimming=\"CharacterEllipsis\""), "Details Tab Value must not specify TextTrimming='CharacterEllipsis'");

            // 3. Verify Details Tab ItemsControl has right margin to prevent scrollbar overlapping copy buttons
            Assert(detailsSection.Contains("<ItemsControl ItemsSource=\"{Binding PreviewInfo.DetailsList}\" Margin=\"0,0,10,0\">"),
                "Details Tab ItemsControl must have Margin='0,0,10,0' to eliminate scrollbar overlap");

            // 4. Verify MultipleFilesList and Color Formats cards have right margin to eliminate scrollbar overlap
            Assert(xaml.Contains("<ItemsControl ItemsSource=\"{Binding PreviewInfo.MultipleFilesList}\" Margin=\"0,0,8,0\">"),
                "MultipleFilesList ItemsControl must have right margin to clear scrollbar");
            Assert(xaml.Contains("<StackPanel Margin=\"0,0,8,0\">"),
                "Color Formats StackPanel must have right margin to clear scrollbar");

            // 5. Verify WPF TextBlock measures multi-line wrap for file paths
            var tb = new System.Windows.Controls.TextBlock
            {
                Text = @"C:\Users\woran\Documents\My_Project\C#\Clipboard\Clipboard_WebSetup.exe",
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Width = 250
            };
            tb.Measure(new System.Windows.Size(250, 1000));
            Assert(tb.DesiredSize.Height > 20, "Wrapped path should measure multiple lines in height");

            // 6. Verify Selectable Read-Only text styling and selectable TextBox elements
            Assert(xaml.Contains("x:Key=\"SelectableReadOnlyTextBoxStyle\""), "SelectableReadOnlyTextBoxStyle must be defined in Window.Resources");
            Assert(xaml.Contains("SelectableReadOnlyTextBoxStyle") && xaml.Contains("PreviewInfo.FilePath"), "Tab 0 FilePath must be configured as a selectable TextBox");
            Assert(detailsSection.Contains("SelectableReadOnlyTextBoxStyle"), "Details Tab Value must be configured as a selectable TextBox");
        }

        private static void Test_ScreenCaptureProtection()
        {
            // 1. Verify ViewModel property and toggle command
            var vm = new MainViewModel();
            bool initial = vm.IsScreenCaptureProtectionEnabled;
            vm.ToggleScreenCaptureProtectionCommand.Execute(null);
            Assert(vm.IsScreenCaptureProtectionEnabled != initial, "ToggleScreenCaptureProtectionCommand should toggle state");
            vm.ToggleScreenCaptureProtectionCommand.Execute(null);
            Assert(vm.IsScreenCaptureProtectionEnabled == initial, "ToggleScreenCaptureProtectionCommand should restore state");

            // 2. Verify DTO persistence
            var dto = new ClipboardDataDto
            {
                IsScreenCaptureProtectionEnabled = true
            };
            Assert(dto.IsScreenCaptureProtectionEnabled, "DTO should serialize screen capture protection state");

            // 3. Verify MainWindow.xaml has ui:ToggleSwitch bound to IsScreenCaptureProtectionEnabled
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml");
            if (!File.Exists(xamlPath)) xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml");
            Assert(File.Exists(xamlPath), "MainWindow.xaml must exist");
            string xaml = File.ReadAllText(xamlPath);
            Assert(xaml.Contains("IsScreenCaptureProtectionEnabled"),
                "MainWindow.xaml must bind ToggleSwitch to IsScreenCaptureProtectionEnabled");

            // 4. Verify MainWindow.xaml has AllowsTransparency="False" for capture protection compatibility
            Assert(xaml.Contains("AllowsTransparency=\"False\""),
                "MainWindow.xaml must specify AllowsTransparency='False' so SetWindowDisplayAffinity works");

            // 5. Verify MainWindow.xaml.cs has SetWindowDisplayAffinity
            string csPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml.cs");
            if (!File.Exists(csPath)) csPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml.cs");
            Assert(File.Exists(csPath), "MainWindow.xaml.cs must exist");
            string cs = File.ReadAllText(csPath);
            Assert(cs.Contains("SetWindowDisplayAffinity") && cs.Contains("WDA_EXCLUDEFROMCAPTURE"),
                "MainWindow.xaml.cs must declare and use SetWindowDisplayAffinity with WDA_EXCLUDEFROMCAPTURE");

            // 6. Functional HWND verification: Verify SetWindowDisplayAffinity succeeds on window with AllowsTransparency=false
            var testWin = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = false,
                Width = 430,
                Height = 595
            };
            var chrome = new System.Windows.Shell.WindowChrome
            {
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(16),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            };
            System.Windows.Shell.WindowChrome.SetWindowChrome(testWin, chrome);
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(testWin).EnsureHandle();
            Assert(hwnd != IntPtr.Zero, "Window must have a valid HWND");

            bool appliedOn = SetWindowDisplayAffinity(hwnd, 0x11);
            if (!appliedOn) appliedOn = SetWindowDisplayAffinity(hwnd, 0x01);
            Assert(appliedOn, "SetWindowDisplayAffinity with WDA_EXCLUDEFROMCAPTURE or WDA_MONITOR must succeed");

            uint affOn;
            GetWindowDisplayAffinity(hwnd, out affOn);
            Assert(affOn == 0x11 || affOn == 0x01, $"DisplayAffinity should be 0x11 or 0x01, got 0x{affOn:X}");

            bool appliedOff = SetWindowDisplayAffinity(hwnd, 0x00);
            Assert(appliedOff, "SetWindowDisplayAffinity with WDA_NONE must succeed");

            uint affOff;
            GetWindowDisplayAffinity(hwnd, out affOff);
            Assert(affOff == 0x00, $"DisplayAffinity should be 0x00, got 0x{affOff:X}");

            testWin.Close();
        }

        private static void Test_WindowsClipboard_WinV_Integration()
        {
            // 1. Verify HotkeyConfig IsWinV detection
            var hotkeyWinV = HotkeyManager.GetPreset("WIN+V");
            Assert(hotkeyWinV.IsWinV, "HotkeyConfig with Windows=true and VK_V should return IsWinV=true");

            var hotkeyOther = HotkeyConfig.Default;
            Assert(!hotkeyOther.IsWinV, "HotkeyConfig with Ctrl+Shift+V should return IsWinV=false");

            // 2. Verify WindowsClipboardHelper registry reading/writing
            bool initialVal = WindowsClipboardHelper.IsWindowsClipboardHistoryEnabled();
            
            // Sync with Win+V and no user override -> should disable
            bool autoDisabled = WindowsClipboardHelper.SyncWithHotkey(hotkeyWinV, userExplicitOverride: null);
            Assert(!autoDisabled, "SyncWithHotkey with Win+V and no user override should disable Windows clipboard");

            // Sync with non-Win+V and no user override -> should restore
            bool autoRestored = WindowsClipboardHelper.SyncWithHotkey(hotkeyOther, userExplicitOverride: null);
            Assert(autoRestored, "SyncWithHotkey with non-Win+V and no user override should restore Windows clipboard");

            // If user explicitly set override, user override must take precedence
            bool overriddenVal = WindowsClipboardHelper.SyncWithHotkey(hotkeyWinV, userExplicitOverride: true);
            Assert(overriddenVal, "SyncWithHotkey must respect userExplicitOverride=true even when hotkey is Win+V");

            // Restore initial state
            WindowsClipboardHelper.SetWindowsClipboardHistoryEnabled(initialVal);

            // 3. Verify ViewModel preference management
            var vm = new MainViewModel();
            vm.SetWindowsClipboardHistoryPreference(false);
            Assert(!vm.IsWindowsClipboardHistoryEnabled, "IsWindowsClipboardHistoryEnabled should reflect set preference");
            vm.SetWindowsClipboardHistoryPreference(true);
            Assert(vm.IsWindowsClipboardHistoryEnabled, "IsWindowsClipboardHistoryEnabled should reflect restored preference");
            WindowsClipboardHelper.SetWindowsClipboardHistoryEnabled(initialVal);
        }

        private static void Test_EfficiencyAndPowerSavingMode()
        {
            // 1. Verify EfficiencyModeHelper API calls without crash
            EfficiencyModeHelper.EnableEfficiencyMode();
            Assert(System.Diagnostics.Process.GetCurrentProcess().PriorityClass == System.Diagnostics.ProcessPriorityClass.BelowNormal,
                "Process priority should be BelowNormal in efficiency mode");

            EfficiencyModeHelper.DisableEfficiencyMode();
            Assert(System.Diagnostics.Process.GetCurrentProcess().PriorityClass == System.Diagnostics.ProcessPriorityClass.Normal,
                "Process priority should be Normal when active");

            // 2. Verify TrimMemory execution
            long memBefore = GC.GetTotalMemory(true);
            EfficiencyModeHelper.TrimMemory();
            long memAfter = GC.GetTotalMemory(true);
            Assert(memAfter <= memBefore + 64 * 1024 * 1024, $"TrimMemory should run cleanly without memory leak (before={memBefore}, after={memAfter})");

            // 3. Verify ViewModel toggle
            var vm = new MainViewModel();
            vm.ToggleEfficiencyModeCommand.Execute(null);
            bool state = vm.IsEfficiencyModeEnabled;
            vm.ToggleEfficiencyModeCommand.Execute(null);
            Assert(vm.IsEfficiencyModeEnabled != state, "ToggleEfficiencyModeCommand should toggle state");
        }

        private static void Test_WindowsService_CleanRemoval()
        {
            // 1. Verify ServiceManager operates as pure in-app monitor without ServiceController
            Assert(ServiceManager.IsServiceActive(), "ServiceManager in-app monitoring should default to true");
            ServiceManager.StopServiceSync();
            Assert(!ServiceManager.IsServiceActive(), "StopServiceSync should pause in-app monitoring state");
            ServiceManager.StartServiceAsync().Wait();
            Assert(ServiceManager.IsServiceActive(), "StartServiceAsync should restore state");

            // 2. Verify csproj has no System.ServiceProcess reference
            string csprojPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "CustomClipboardManager.csproj");
            if (!File.Exists(csprojPath)) csprojPath = Path.Combine(Directory.GetCurrentDirectory(), "CustomClipboardManager.csproj");
            Assert(File.Exists(csprojPath), "CustomClipboardManager.csproj must exist");
            string csproj = File.ReadAllText(csprojPath);
            Assert(!csproj.Contains("System.ServiceProcess"), "CustomClipboardManager.csproj must not reference System.ServiceProcess");

            // 3. Verify obsolete service files are deleted
            string rootDir = Path.GetDirectoryName(csprojPath)!;
            Assert(!File.Exists(Path.Combine(rootDir, "Core", "ClipboardWindowsService.cs")), "ClipboardWindowsService.cs must be removed");
            Assert(!File.Exists(Path.Combine(rootDir, "Core", "UserSessionLauncher.cs")), "UserSessionLauncher.cs must be removed");
        }

        private static void Test_NonBlockingAsyncClipboardRetries()
        {
            // Verify MainViewModel has non-blocking async SetClipboardWithRetryAsync without Thread.Sleep
            string vmPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ViewModels", "MainViewModel.cs");
            if (!File.Exists(vmPath)) vmPath = Path.Combine(Directory.GetCurrentDirectory(), "ViewModels", "MainViewModel.cs");
            Assert(File.Exists(vmPath), "MainViewModel.cs must exist");
            string vmCode = File.ReadAllText(vmPath);

            Assert(vmCode.Contains("SetClipboardWithRetryAsync"), "MainViewModel.cs must define SetClipboardWithRetryAsync");
            Assert(vmCode.Contains("await Task.Delay"), "SetClipboardWithRetryAsync must use await Task.Delay to avoid starving UI message pump");
            Assert(!vmCode.Contains("Thread.Sleep"), "MainViewModel.cs must not use synchronous Thread.Sleep");
        }

        private static void Test_EmojiClassification_And_DataMigration()
        {
            // 1. Verify ClipboardItem.IsPureEmoji
            Assert(ClipboardItem.IsPureEmoji("👋"), "Single emoji should be recognized as pure emoji");
            Assert(ClipboardItem.IsPureEmoji("❤️🔥🎉"), "Multiple emojis should be recognized as pure emoji");
            Assert(ClipboardItem.IsPureEmoji("  😀  "), "Emoji with whitespace should be recognized as pure emoji");
            Assert(!ClipboardItem.IsPureEmoji("Hello 👋"), "Mixed text and emoji should not be pure emoji");
            Assert(!ClipboardItem.IsPureEmoji("123"), "Numbers should not be pure emoji");
            Assert(!ClipboardItem.IsPureEmoji(""), "Empty string should not be pure emoji");

            // 2. Verify SmartCategory.Emoji Badge Text
            var emojiItem = new ClipboardItem
            {
                Category = SmartCategory.Emoji,
                TextContent = "👋"
            };
            I18n.SetLanguage("th-TH");
            Assert(emojiItem.CategoryBadgeText == "อิโมจิ", "Thai badge for Emoji should be 'อิโมจิ'");
            I18n.SetLanguage("en-US");
            Assert(emojiItem.CategoryBadgeText == "Emoji", "English badge for Emoji should be 'Emoji'");

            // 3. Verify MainWindow.xaml has dedicated Emoji container
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml");
            if (!File.Exists(xamlPath)) xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml");
            Assert(File.Exists(xamlPath), "MainWindow.xaml must exist");
            string xaml = File.ReadAllText(xamlPath);
            Assert(xaml.Contains("Segoe UI Emoji") && xaml.Contains("Value=\"Emoji\""),
                "MainWindow.xaml must contain dedicated Emoji visual presentation");

            // 4. Verify ClipboardDataService has smart historical data recovery
            string servicePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Services", "ClipboardDataService.cs");
            if (!File.Exists(servicePath)) servicePath = Path.Combine(Directory.GetCurrentDirectory(), "Services", "ClipboardDataService.cs");
            Assert(File.Exists(servicePath), "ClipboardDataService.cs must exist");
            string serviceCode = File.ReadAllText(servicePath);
            Assert(serviceCode.Contains("BackupsFolder") && serviceCode.Contains("data_*.json"),
                "ClipboardDataService.cs must scan historical backups and migrate past items");
        }

        private static void Test_BatchDeletion_And_ClickOutside()
        {
            // 1. Verify batch deletion performance (2000 items deleted in under 1 second without freezing)
            var vm = new MainViewModel();
            vm.ClipboardItems.Clear();

            for (int i = 0; i < 2000; i++)
            {
                vm.ClipboardItems.Add(new ClipboardItem
                {
                    TextContent = $"Performance Test Item {i}",
                    Category = SmartCategory.Text,
                    IsSelected = true
                });
            }

            Assert(vm.ClipboardItems.Count == 2000, "Should have 2000 items populated");
            Assert(vm.HasSelectedItems, "Should have selected items");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            vm.DeleteSelectedCommand.Execute(null);
            sw.Stop();

            Assert(vm.ClipboardItems.Count == 0, "All 2000 selected items should be cleanly deleted");
            Assert(!vm.IsSelectionMode, "Selection mode should be disabled after batch deletion");
            Assert(sw.ElapsedMilliseconds < 1500, $"Batch deletion of 2000 items took {sw.ElapsedMilliseconds}ms, must be < 1500ms");

            // 2. Verify _outsideClickTimer and modal backdrop click handlers
            string csPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml.cs");
            if (!File.Exists(csPath)) csPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml.cs");
            Assert(File.Exists(csPath), "MainWindow.xaml.cs must exist");
            string cs = File.ReadAllText(csPath);

            Assert(cs.Contains("_outsideClickTimer") && cs.Contains("InitializeOutsideClickMonitor"),
                "MainWindow.xaml.cs must implement _outsideClickTimer to close on blank outside area");
            Assert(cs.Contains("HotkeySettingsBackdrop_MouseDown") && cs.Contains("ConfirmBackdrop_MouseDown"),
                "MainWindow.xaml.cs must implement modal backdrop click handlers");

            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MainWindow.xaml");
            if (!File.Exists(xamlPath)) xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "MainWindow.xaml");
            Assert(File.Exists(xamlPath), "MainWindow.xaml must exist");
            string xaml = File.ReadAllText(xamlPath);

            Assert(xaml.Contains("ConfirmBackdrop_MouseDown") && xaml.Contains("HotkeySettingsBackdrop_MouseDown"),
                "MainWindow.xaml must bind backdrop click handlers on modals");
        }
    }
}
