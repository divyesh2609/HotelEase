using System;
using System.Globalization;
using System.Windows.Data;

namespace HotelEase.Converters
{
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is decimal d)
            {
                return $"${d:N2}";
            }
            if (value is double db)
            {
                return $"${db:N2}";
            }
            if (value is float f)
            {
                return $"${f:N2}";
            }
            if (decimal.TryParse(value?.ToString(), out var parsed))
            {
                return $"${parsed:N2}";
            }
            return "$0.00";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                var clean = s.Replace("$", "").Replace(",", "").Trim();
                if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            return 0m;
        }
    }
}
