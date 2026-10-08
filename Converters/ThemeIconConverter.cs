using System;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace CustomClipboardManager
{
    public class ThemeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDarkMode)
            {
                if (targetType == typeof(SymbolRegular))
                {
                    return isDarkMode ? SymbolRegular.WeatherSunny24 : SymbolRegular.WeatherMoon24;
                }
                return isDarkMode ? "\uE708" : "\uE706";
            }
            return targetType == typeof(SymbolRegular) ? SymbolRegular.WeatherSunny24 : "\uE708";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
