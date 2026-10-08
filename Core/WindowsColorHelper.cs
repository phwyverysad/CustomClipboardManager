using System;
using System.Windows;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace CustomClipboardManager.Core
{
    public static class WindowsColorHelper
    {
        public static MediaColor GetWindowsAccentColor()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                if (key != null)
                {
                    var val = key.GetValue("AccentColor");
                    if (val != null)
                    {
                        uint abgr = Convert.ToUInt32(val);
                        byte r = (byte)(abgr & 0xFF);
                        byte g = (byte)((abgr >> 8) & 0xFF);
                        byte b = (byte)((abgr >> 16) & 0xFF);
                        return MediaColor.FromRgb(r, g, b);
                    }

                    var colVal = key.GetValue("ColorizationColor");
                    if (colVal != null)
                    {
                        uint argb = Convert.ToUInt32(colVal);
                        byte r = (byte)((argb >> 16) & 0xFF);
                        byte g = (byte)((argb >> 8) & 0xFF);
                        byte b = (byte)(argb & 0xFF);
                        if (r != 0 || g != 0 || b != 0)
                        {
                            return MediaColor.FromRgb(r, g, b);
                        }
                    }
                }
            }
            catch { }

            try
            {
                var glassColor = SystemParameters.WindowGlassColor;
                if (glassColor.A > 0 && (glassColor.R > 0 || glassColor.G > 0 || glassColor.B > 0))
                {
                    return MediaColor.FromRgb(glassColor.R, glassColor.G, glassColor.B);
                }
            }
            catch { }

            return MediaColor.FromRgb(0, 120, 215); // Default Windows Accent Blue fallback
        }

        public static bool GetIsWindowsLightTheme()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    var val = key.GetValue("AppsUseLightTheme");
                    if (val is int intVal)
                    {
                        return intVal == 1;
                    }
                }
            }
            catch { }
            return false;
        }

        public static void ApplyWindowsColors(ResourceDictionary dict, bool isDark)
        {
            MediaColor accent = GetWindowsAccentColor();
            MediaColor accentHover = MediaColor.FromArgb(255,
                (byte)Math.Min(255, accent.R + 24),
                (byte)Math.Min(255, accent.G + 24),
                (byte)Math.Min(255, accent.B + 24));

            MediaColor accentPressed = MediaColor.FromArgb(255,
                (byte)Math.Max(0, accent.R - 24),
                (byte)Math.Max(0, accent.G - 24),
                (byte)Math.Max(0, accent.B - 24));

            // Selected background tint with gentle opacity
            MediaColor selBg = isDark
                ? MediaColor.FromArgb(40, accent.R, accent.G, accent.B)
                : MediaColor.FromArgb(18, accent.R, accent.G, accent.B);

            MediaColor selBorder = isDark
                ? MediaColor.FromArgb(130, accent.R, accent.G, accent.B)
                : MediaColor.FromArgb(95, accent.R, accent.G, accent.B);

            dict["AccentColor"] = accent;
            dict["AccentBrush"] = new SolidColorBrush(accent);
            dict["AccentColorBrush"] = new SolidColorBrush(accent);
            dict["AccentHoverBrush"] = new SolidColorBrush(accentHover);
            dict["AccentPressedBrush"] = new SolidColorBrush(accentPressed);

            dict["SearchFocusedBorderBrush"] = new SolidColorBrush(accent);
            dict["FilterCheckedBackgroundBrush"] = new SolidColorBrush(accent);
            dict["FilterCheckedBorderBrush"] = new SolidColorBrush(accent);

            dict["ListViewItemHoverBorderBrush"] = new SolidColorBrush(accent);
            dict["ListViewItemSelectedBackgroundBrush"] = new SolidColorBrush(selBg);
            dict["ListViewItemSelectedBorderBrush"] = new SolidColorBrush(accent);

            dict["CardSelectedBackgroundBrush"] = new SolidColorBrush(selBg);
            dict["CardSelectedBorderBrush"] = new SolidColorBrush(selBorder);

            MediaColor chkHoverBg = MediaColor.FromArgb(24, accent.R, accent.G, accent.B);
            dict["CheckBoxHoverBackgroundBrush"] = new SolidColorBrush(chkHoverBg);
        }
    }
}
