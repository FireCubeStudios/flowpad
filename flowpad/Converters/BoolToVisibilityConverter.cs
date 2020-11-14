using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace flowpad.Converters
{
    class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter != null)
                return (Visibility)Convert(value, targetType, null, language) == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            if (value is null)
            {
                return Visibility.Collapsed;
            }

            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }

            if (value is int i)
            {
                return i > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (value is string str)
            {
                return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (value is IEnumerable e)
            {
                return e.OfType<object>().Any() ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
