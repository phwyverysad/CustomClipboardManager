using System;
using System.Globalization;
using System.Windows.Data;

namespace CustomClipboardManager
{
    public class ThemeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDarkMode)
            {
                // Return Sun icon for dark mode (to switch to light), and Moon icon for light mode (to switch to dark)
                // Actually, Windows convention is the icon represents the CURRENT state or the TARGET state.
                // Let's use Moon for Dark Mode, Sun for Light Mode.
                return isDarkMode ? "\uE708" : "\uE706"; // E708 is Sun, E706 is Moon
            }
            return "\uE708";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
