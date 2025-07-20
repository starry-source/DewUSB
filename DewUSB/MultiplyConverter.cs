using System;
using System.Globalization;
using System.Windows.Data;

namespace DewUSB
{
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double number && parameter is string multiplier)
            {
                if (double.TryParse(multiplier, out double factor))
                {
                    return number * factor;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}