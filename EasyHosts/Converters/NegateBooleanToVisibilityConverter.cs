using System;
using System.Windows;
using System.Windows.Data;

namespace EasyHosts.Converters
{
    public class NegateBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isVisible;
            bool.TryParse(System.Convert.ToString(value), out isVisible);

            return isVisible ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }
}
