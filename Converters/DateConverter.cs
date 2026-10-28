using System;
using System.Globalization;
using System.Windows.Data;

namespace HotelEase.Converters
{
    public class DateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                var format = parameter?.ToString() ?? "MMM dd, yyyy";
                return dt.ToString(format);
            }
            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (DateTime.TryParse(value?.ToString(), out var dt))
            {
                return dt;
            }
            return DateTime.Today;
        }
    }
}
